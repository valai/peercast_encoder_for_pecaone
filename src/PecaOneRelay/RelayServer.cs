using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PecaOneRelay;

internal sealed class RelayServer(
    AppState settings, SessionManager sessions, Func<IPAddress?>? addressProvider = null,
    Func<CancellationToken, Task<byte[]>>? spFetcher = null) : IAsyncDisposable
{
    private readonly SemaphoreSlim _binding = new(1, 1);
    private readonly Func<IPAddress?> _addressProvider = addressProvider ?? TailscaleAddress.Find;
    private WebApplication? _app;
    private IPAddress? _address;
    private int _boundPort;
    public string? BaseUrl => _address is null ? null : $"http://{_address}:{settings.ListenPort}";
    public string? Error { get; private set; }

    public string? NewPairPayload()
    {
        if (BaseUrl is null) return null;
        var (code, expires) = settings.NewPairCode();
        return JsonSerializer.Serialize(new { v = 1, baseUrl = BaseUrl, pairCode = code, expiresAt = expires });
    }

    public async Task EnsureBoundAsync()
    {
        if (!await _binding.WaitAsync(0)) return;
        try
        {
            var desired = _addressProvider();
            if (Equals(desired, _address) && _app is not null && _boundPort == settings.ListenPort) return;
            if (_app is not null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
                _app = null;
                _address = null;
            }
            if (desired is null)
            {
                Error = "Tailscale が接続されていません";
                return;
            }
            try
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.Logging.ClearProviders(); // HLS URLs are credentials and must not be logged.
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(desired, settings.ListenPort);
                    options.Limits.MaxRequestBodySize = 4096;
                });
                var app = builder.Build();
                MapEndpoints(app);
                await app.StartAsync();
                _app = app;
                _address = desired;
                _boundPort = settings.ListenPort;
                Error = null;
            }
            catch (Exception ex)
            {
                Error = "VPN 待受を開始できません: " + ex.Message;
            }
        }
        finally { _binding.Release(); }
    }

    private void MapEndpoints(WebApplication app)
    {
        static bool Authorized(HttpContext context, AppState state) =>
            state.Authenticate(context.Request.Headers.Authorization.ToString());

        app.MapPost("/api/v1/pair", async (HttpContext context) =>
        {
            PairRequest? request;
            try { request = await context.Request.ReadFromJsonAsync<PairRequest>(); }
            catch (JsonException) { return Results.BadRequest(new { error = "invalid_json" }); }
            var token = request?.PairCode is null ? null : settings.Pair(request.PairCode);
            if (token is null) return Results.Json(new { error = "invalid_or_expired_pair_code" }, statusCode: 401);
            await sessions.StopAsync();
            return Results.Json(new { token, tokenType = "Bearer" });
        });

        app.MapGet("/api/v1/status", async (HttpContext context) =>
        {
            if (!Authorized(context, settings)) return Results.Unauthorized();
            return Results.Json(await sessions.ViewAsync(BaseUrl, context.RequestAborted));
        });
        app.MapGet("/api/v1/sessions/current", async (HttpContext context) =>
        {
            if (!Authorized(context, settings)) return Results.Unauthorized();
            return Results.Json(await sessions.ViewAsync(BaseUrl, context.RequestAborted));
        });
        app.MapPost("/api/v1/sessions", async (HttpContext context) =>
        {
            if (!Authorized(context, settings)) return Results.Unauthorized();
            SessionRequest? request;
            try { request = await context.Request.ReadFromJsonAsync<SessionRequest>(); }
            catch (JsonException) { return Results.BadRequest(new { error = "invalid_json" }); }
            if (request is null || !PeerCastClient.ValidChannel(request.ChannelId) ||
                !PeerCastClient.ValidTracker(request.Tracker))
                return Results.UnprocessableEntity(new { error = "invalid_channel_or_tracker" });
            var started = await sessions.StartAsync(request);
            if (started.Conflict) return Results.Conflict(new { error = "session_busy", sessionId = started.SessionId });
            return Results.Json(new { sessionId = started.SessionId, state = "starting" }, statusCode: 202);
        });
        app.MapDelete("/api/v1/sessions/current", async (HttpContext context) =>
        {
            if (!Authorized(context, settings)) return Results.Unauthorized();
            await sessions.StopAsync();
            return Results.NoContent();
        });

        // SP は要求元の外部 IP と PeerCastStation の公開待受ポートで判定する。
        // スマホの IP ではなく Windows から直接取得した結果だけを返す。
        app.MapGet("/api/v1/sp/index.txt", async (HttpContext context) =>
        {
            if (!Authorized(context, settings)) return Results.Unauthorized();
            try
            {
                var bytes = await (spFetcher ?? SpDirectoryClient.FetchAsync)(context.RequestAborted);
                context.Response.Headers.CacheControl = "no-store";
                return Results.Bytes(bytes, "text/plain; charset=utf-8");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                return Results.Json(new { error = "sp_fetch_failed" }, statusCode: 502);
            }
        });

        app.MapGet("/hls/{sessionId}/{ticket}/{**file}", (HttpContext context, string sessionId, string ticket, string file) =>
        {
            var path = sessions.MediaFile(sessionId, ticket, file);
            if (path is null) return Results.NotFound();
            context.Response.Headers.CacheControl = "no-store";
            return Results.File(path, file.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.apple.mpegurl" : "video/mp2t");
        });
    }

    private sealed record PairRequest(string PairCode);

    public async ValueTask DisposeAsync()
    {
        await _binding.WaitAsync();
        try
        {
            if (_app is not null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
                _app = null;
            }
            _address = null;
        }
        finally { _binding.Release(); _binding.Dispose(); }
    }
}
