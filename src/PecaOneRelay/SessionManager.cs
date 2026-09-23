using System.Security.Cryptography;

namespace PecaOneRelay;

internal sealed record SessionRequest(string ChannelId, string Tracker);
internal sealed record SessionStart(bool Conflict, string SessionId);
internal sealed record SessionView(
    string? SessionId, string? ChannelId, string State, string? Error,
    bool PeerCastOnline, bool RelayReachable, int DownstreamRelays, string RelayMessage,
    IReadOnlyDictionary<string, string>? Playlists);

internal sealed class SessionManager : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly PeerCastClient _peerCast;
    private readonly string _root;
    private Session? _current;

    private sealed class Session
    {
        public required string Id;
        public required string Ticket;
        public required string ChannelId;
        public required string Tracker;
        public required string Directory;
        public required CancellationTokenSource Cancellation;
        public Transcoder? Transcoder;
        public string State = "starting";
        public string? Error;
        public DateTimeOffset LastMediaRequest = DateTimeOffset.UtcNow;
    }

    public SessionManager(PeerCastClient peerCast, string dataDirectory)
    {
        _peerCast = peerCast;
        _root = Path.Combine(dataDirectory, "hls");
        Directory.CreateDirectory(_root);
        foreach (var stale in Directory.GetDirectories(_root))
            try { Directory.Delete(stale, true); } catch (IOException) { }
    }

    public string? ActiveChannel { get { lock (_gate) return _current?.ChannelId; } }

    public async Task<SessionStart> StartAsync(SessionRequest request)
    {
        Session? previous = null;
        Session created;
        lock (_gate)
        {
            if (_current is { State: not "failed" } active)
            {
                if (active.ChannelId.Equals(request.ChannelId, StringComparison.OrdinalIgnoreCase) &&
                    active.Tracker.Equals(request.Tracker, StringComparison.OrdinalIgnoreCase))
                    return new SessionStart(false, active.Id);
                return new SessionStart(true, active.Id);
            }
            previous = _current;
            created = new Session
            {
                Id = Guid.NewGuid().ToString("N"), Ticket = Token(),
                ChannelId = request.ChannelId.ToUpperInvariant(), Tracker = request.Tracker,
                Directory = Path.Combine(_root, Guid.NewGuid().ToString("N")),
                Cancellation = new CancellationTokenSource()
            };
            _current = created;
        }
        if (previous is not null) await DisposeSessionAsync(previous);
        _ = Task.Run(() => StartCoreAsync(created));
        return new SessionStart(false, created.Id);
    }

    private async Task StartCoreAsync(Session session)
    {
        try
        {
            var token = session.Cancellation.Token;
            var source = await _peerCast.StreamUriAsync(session.ChannelId, session.Tracker, token);
            var (video, audio) = await Transcoder.ProbeAsync(source, token);
            if (!video) throw new InvalidOperationException("映像のある FLV 番組だけ変換できます");
            token.ThrowIfCancellationRequested();
            var transcoder = new Transcoder();
            lock (_gate)
            {
                if (!ReferenceEquals(_current, session) || token.IsCancellationRequested) return;
                session.Transcoder = transcoder;
                Directory.CreateDirectory(session.Directory);
                transcoder.Start(source, session.Directory, audio);
            }
            _ = WatchExitAsync(session, transcoder);
            var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
            while (DateTimeOffset.UtcNow < deadline)
            {
                token.ThrowIfCancellationRequested();
                if (session.State == "failed") throw new InvalidOperationException(session.Error ?? "FFmpeg が終了しました");
                if (transcoder.ExitCode is not null) throw new InvalidOperationException("FFmpeg が終了しました");
                if (File.Exists(Path.Combine(session.Directory, "master.m3u8")) &&
                    new[] { "high", "medium", "low" }.All(profile =>
                        File.Exists(Path.Combine(session.Directory, profile, "index.m3u8"))))
                {
                    lock (_gate) if (ReferenceEquals(_current, session)) session.State = "ready";
                    return;
                }
                await Task.Delay(300, token);
            }
            throw new TimeoutException("HLS の準備が45秒以内に完了しませんでした");
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            lock (_gate)
            {
                if (ReferenceEquals(_current, session))
                {
                    session.State = "failed";
                    session.Error = ex.Message;
                }
            }
            if (session.Transcoder is not null) await session.Transcoder.DisposeAsync();
            try { if (Directory.Exists(session.Directory)) Directory.Delete(session.Directory, true); } catch (IOException) { }
        }
    }

    private async Task WatchExitAsync(Session session, Transcoder transcoder)
    {
        if (transcoder.ExitTask is null) return;
        try { await transcoder.ExitTask; } catch (InvalidOperationException) { return; }
        var unexpected = false;
        lock (_gate)
        {
            if (ReferenceEquals(_current, session) && !session.Cancellation.IsCancellationRequested)
            {
                session.State = "failed";
                session.Error = $"FFmpeg が終了しました (code {transcoder.ExitCode})";
                unexpected = true;
            }
        }
        if (unexpected)
        {
            await transcoder.DisposeAsync();
            try { if (Directory.Exists(session.Directory)) Directory.Delete(session.Directory, true); } catch (IOException) { }
        }
    }

    public async Task<SessionView> ViewAsync(string? baseUrl, CancellationToken cancellationToken = default)
    {
        Session? session;
        lock (_gate) session = _current;
        var health = await _peerCast.HealthAsync(session?.ChannelId, cancellationToken);
        if (session is null)
            return new SessionView(null, null, "idle", null, health.Online, health.RelayReachable,
                health.DownstreamRelays, health.Message, null);
        IReadOnlyDictionary<string, string>? playlists = null;
        if (session.State == "ready" && baseUrl is not null)
        {
            var prefix = $"{baseUrl}/hls/{session.Id}/{session.Ticket}/";
            playlists = new Dictionary<string, string>
            {
                ["auto"] = prefix + "master.m3u8", ["high"] = prefix + "high/index.m3u8",
                ["medium"] = prefix + "medium/index.m3u8", ["low"] = prefix + "low/index.m3u8"
            };
        }
        return new SessionView(session.Id, session.ChannelId, session.State, session.Error,
            health.Online, health.RelayReachable, health.DownstreamRelays, health.Message, playlists);
    }

    public string? MediaFile(string sessionId, string ticket, string relativePath)
    {
        lock (_gate)
        {
            var session = _current;
            if (session is null || session.State != "ready" || session.Id != sessionId ||
                !FixedEquals(session.Ticket, ticket)) return null;
            if (!(relativePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
                  relativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))) return null;
            var path = Path.GetFullPath(Path.Combine(session.Directory, relativePath));
            if (!path.StartsWith(Path.GetFullPath(session.Directory) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
            session.LastMediaRequest = DateTimeOffset.UtcNow;
            return path;
        }
    }

    public async Task<bool> StopAsync()
    {
        Session? old;
        lock (_gate)
        {
            old = _current;
            _current = null;
        }
        if (old is null) return false;
        await DisposeSessionAsync(old);
        return true;
    }

    public async Task ExpireIdleAsync()
    {
        Session? expired = null;
        lock (_gate)
        {
            if (_current is { State: "ready" } session &&
                DateTimeOffset.UtcNow - session.LastMediaRequest > TimeSpan.FromMinutes(2))
            {
                expired = session;
                _current = null;
            }
        }
        if (expired is not null) await DisposeSessionAsync(expired);
    }

    private static async Task DisposeSessionAsync(Session session)
    {
        session.Cancellation.Cancel();
        if (session.Transcoder is not null) await session.Transcoder.DisposeAsync();
        session.Cancellation.Dispose();
        try { if (Directory.Exists(session.Directory)) Directory.Delete(session.Directory, true); } catch (IOException) { }
    }

    private static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool FixedEquals(string left, string right) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(left), System.Text.Encoding.UTF8.GetBytes(right));

    public async ValueTask DisposeAsync() => await StopAsync();
}
