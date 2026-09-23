using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PecaOneRelay.Tests;

public class RelayTests
{
    [Fact]
    public void PairCodeIsSingleUseAndRevokesOldToken()
    {
        var directory = NewDirectory();
        try
        {
            var settings = new AppState(directory);
            var firstCode = settings.NewPairCode().Code;
            var firstToken = settings.Pair(firstCode);
            Assert.NotNull(firstToken);
            Assert.Null(settings.Pair(firstCode));
            Assert.True(settings.Authenticate("Bearer " + firstToken));
            var secondToken = settings.Pair(settings.NewPairCode().Code);
            Assert.NotNull(secondToken);
            Assert.False(settings.Authenticate("Bearer " + firstToken));
            Assert.True(settings.Authenticate("Bearer " + secondToken));
            settings.Revoke();
            Assert.False(settings.Authenticate("Bearer " + secondToken));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("0123456789ABCDEF0123456789ABCDEF", "example.net:7144", true)]
    [InlineData("00000000000000000000000000000000", "example.net:7144", false)]
    [InlineData("invalid", "example.net:7144", false)]
    [InlineData("0123456789ABCDEF0123456789ABCDEF", "example.net:7144/path", false)]
    public void ValidatesChannelRequest(string id, string tracker, bool expected) =>
        Assert.Equal(expected, PeerCastClient.ValidChannel(id) && PeerCastClient.ValidTracker(tracker));

    [Fact]
    public async Task PeerCastApiRequestsIncludeContentLength()
    {
        var directory = NewDirectory();
        await using var fake = await FakePeerCastStation.CreateAsync();
        try
        {
            var settings = new AppState(directory);
            settings.UpdatePeerCast(fake.Url, FreePort());
            using var peerCast = new PeerCastClient(settings);
            Assert.True((await peerCast.HealthAsync(null)).Online);
            var stream = await peerCast.StreamUriAsync(
                "0123456789ABCDEF0123456789ABCDEF", "example.net:7144", CancellationToken.None);
            Assert.Equal("/stream/0123456789ABCDEF0123456789ABCDEF", stream.AbsolutePath);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task ApiRequiresPairingAndInvalidatesOldCredential()
    {
        var directory = NewDirectory();
        try
        {
            var settings = new AppState(directory);
            var port = FreePort();
            settings.UpdatePeerCast("http://127.0.0.1:65534", port);
            using var peerCast = new PeerCastClient(settings);
            await using var sessions = new SessionManager(peerCast, directory);
            await using var server = new RelayServer(settings, sessions, () => IPAddress.Loopback);
            await server.EnsureBoundAsync();
            Assert.Null(server.Error);
            using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl!) };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/status")).StatusCode);
            var payload = JsonDocument.Parse(server.NewPairPayload()!).RootElement;
            var code = payload.GetProperty("pairCode").GetString();
            var paired = await client.PostAsJsonAsync("/api/v1/pair", new { pairCode = code });
            Assert.Equal(HttpStatusCode.OK, paired.StatusCode);
            var token = (await paired.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PostAsJsonAsync("/api/v1/pair", new { pairCode = code })).StatusCode);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/status")).StatusCode);
            var secondCode = JsonDocument.Parse(server.NewPairPayload()!).RootElement.GetProperty("pairCode").GetString();
            var second = await client.PostAsJsonAsync("/api/v1/pair", new { pairCode = secondCode });
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/status")).StatusCode);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task SpIndexRequiresPairingAndReturnsWindowsFetchedBytes()
    {
        var directory = NewDirectory();
        try
        {
            var settings = new AppState(directory);
            settings.UpdatePeerCast("http://127.0.0.1:65534", FreePort());
            using var peerCast = new PeerCastClient(settings);
            await using var sessions = new SessionManager(peerCast, directory);
            var fetches = 0;
            var fail = false;
            await using var server = new RelayServer(settings, sessions, () => IPAddress.Loopback,
                _ =>
                {
                    fetches++;
                    if (fail) throw new HttpRequestException("SP unavailable");
                    return Task.FromResult(System.Text.Encoding.UTF8.GetBytes("SP channel"));
                });
            await server.EnsureBoundAsync();
            using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl!) };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/sp/index.txt")).StatusCode);
            Assert.Equal(0, fetches);
            var code = JsonDocument.Parse(server.NewPairPayload()!).RootElement.GetProperty("pairCode").GetString();
            var paired = await client.PostAsJsonAsync("/api/v1/pair", new { pairCode = code });
            var token = (await paired.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var index = await client.GetAsync("/api/v1/sp/index.txt");
            Assert.Equal(HttpStatusCode.OK, index.StatusCode);
            Assert.Equal("SP channel", await index.Content.ReadAsStringAsync());
            Assert.Equal("no-store", index.Headers.CacheControl?.ToString());
            Assert.Equal(1, fetches);
            fail = true;
            Assert.Equal(HttpStatusCode.BadGateway, (await client.GetAsync("/api/v1/sp/index.txt")).StatusCode);
            Assert.Equal(2, fetches);
            settings.Revoke();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/sp/index.txt")).StatusCode);
            Assert.Equal(2, fetches);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task SessionAllowsOnlyOneChannelAndReportsRelayHealth()
    {
        var directory = NewDirectory();
        await using var fake = await FakePeerCastStation.CreateAsync();
        try
        {
            var settings = new AppState(directory);
            settings.UpdatePeerCast(fake.Url, FreePort());
            using var peerCast = new PeerCastClient(settings);
            await using var sessions = new SessionManager(peerCast, directory);
            var first = await sessions.StartAsync(new SessionRequest("0123456789ABCDEF0123456789ABCDEF", "example.net:7144"));
            var same = await sessions.StartAsync(new SessionRequest("0123456789ABCDEF0123456789ABCDEF", "example.net:7144"));
            var other = await sessions.StartAsync(new SessionRequest("FEDCBA9876543210FEDCBA9876543210", "example.net:7144"));
            Assert.False(first.Conflict);
            Assert.Equal(first.SessionId, same.SessionId);
            Assert.True(other.Conflict);
            var view = await sessions.ViewAsync("http://127.0.0.1:17444");
            Assert.True(view.PeerCastOnline);
            Assert.True(view.RelayReachable);
            Assert.Equal(1, view.DownstreamRelays);
            Assert.True(await sessions.StopAsync());
            Assert.False(await sessions.StopAsync());
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task EndedSourceReportsFailureAndCanStartAnotherSession()
    {
        var directory = NewDirectory();
        try
        {
            var input = Path.Combine(directory, "short.flv");
            await RunAsync(Transcoder.ToolPath("ffmpeg.exe"), ["-hide_banner", "-loglevel", "error", "-f", "lavfi",
                "-i", "testsrc2=size=320x180:rate=30", "-f", "lavfi", "-i", "sine=frequency=440",
                "-t", "4", "-c:v", "libx264", "-c:a", "aac", "-f", "flv", input]);
            await using var fake = await FakePeerCastStation.CreateAsync(input);
            var settings = new AppState(directory);
            settings.UpdatePeerCast(fake.Url, FreePort());
            using var peerCast = new PeerCastClient(settings);
            await using var sessions = new SessionManager(peerCast, directory);
            var first = await sessions.StartAsync(new SessionRequest("0123456789ABCDEF0123456789ABCDEF", "example.net:7144"));
            SessionView view;
            var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
            do
            {
                await Task.Delay(200);
                view = await sessions.ViewAsync("http://127.0.0.1:17444");
            } while (view.State != "failed" && DateTimeOffset.UtcNow < deadline);
            Assert.Equal("failed", view.State);
            Assert.NotNull(view.Error);
            var cleanupDeadline = DateTimeOffset.UtcNow.AddSeconds(5);
            while (Directory.GetDirectories(Path.Combine(directory, "hls")).Length > 0 && DateTimeOffset.UtcNow < cleanupDeadline)
                await Task.Delay(100);
            Assert.Empty(Directory.GetDirectories(Path.Combine(directory, "hls")));
            var second = await sessions.StartAsync(new SessionRequest("FEDCBA9876543210FEDCBA9876543210", "example.net:7144"));
            Assert.False(second.Conflict);
            Assert.NotEqual(first.SessionId, second.SessionId);
            await sessions.StopAsync();
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task FfmpegProducesThreeHlsVariants()
    {
        var directory = NewDirectory();
        try
        {
            var ffmpeg = Transcoder.ToolPath("ffmpeg.exe");
            Assert.True(File.Exists(ffmpeg), "Run prepare-ffmpeg.ps1 before the tests.");
            var input = Path.Combine(directory, "input.flv");
            await RunAsync(ffmpeg, ["-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc2=size=320x180:rate=30",
                "-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000", "-t", "8", "-c:v", "libx264",
                "-pix_fmt", "yuv420p", "-c:a", "aac", "-f", "flv", input]);
            var source = new Uri(input);
            var probe = await Transcoder.ProbeAsync(source, CancellationToken.None);
            Assert.True(probe.HasVideo);
            Assert.True(probe.HasAudio);
            var output = Path.Combine(directory, "hls");
            await using var transcoder = new Transcoder();
            transcoder.Start(source, output, probe.HasAudio);
            await transcoder.ExitTask!.WaitAsync(TimeSpan.FromSeconds(90));
            Assert.Equal(0, transcoder.ExitCode);
            var master = File.ReadAllText(Path.Combine(output, "master.m3u8"));
            var variantPaths = master.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith('#')).ToArray();
            Assert.Equal(["high/index.m3u8", "medium/index.m3u8", "low/index.m3u8"], variantPaths);
            foreach (var profile in new[] { "high", "medium", "low" })
            {
                Assert.Contains(profile, master);
                var playlist = File.ReadAllText(Path.Combine(output, profile, "index.m3u8"));
                Assert.Contains("#EXTM3U", playlist);
                var segments = Directory.GetFiles(Path.Combine(output, profile), "*.ts");
                Assert.NotEmpty(segments);
                var dimensions = await ProbeDimensionsAsync(segments[0]);
                Assert.Equal((320, 180), dimensions); // A low-resolution source is never enlarged.
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    private static async Task RunAsync(string executable, string[] args)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
        Assert.True(process.ExitCode == 0, await errors);
    }

    private static async Task<(int Width, int Height)> ProbeDimensionsAsync(string segment)
    {
        var start = new ProcessStartInfo(Transcoder.ToolPath("ffprobe.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var arg in new[] { "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "json", segment })
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.Equal(0, process.ExitCode);
        using var document = JsonDocument.Parse(output);
        var stream = document.RootElement.GetProperty("streams")[0];
        return (stream.GetProperty("width").GetInt32(), stream.GetProperty("height").GetInt32());
    }

    private static string NewDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class FakePeerCastStation : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public string Url { get; }

        private FakePeerCastStation(WebApplication app, string url)
        {
            _app = app;
            Url = url;
        }

        public static async Task<FakePeerCastStation> CreateAsync(string? flvPath = null)
        {
            var port = FreePort();
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));
            var app = builder.Build();
            app.MapPost("/api/1", async (HttpContext context) =>
            {
                if (context.Request.ContentLength is null)
                    return Results.StatusCode(StatusCodes.Status411LengthRequired);
                using var request = await JsonDocument.ParseAsync(context.Request.Body);
                var method = request.RootElement.GetProperty("method").GetString();
                object result = method switch
                {
                    "getVersionInfo" => new { apiVersion = "1.0.0" },
                    "getAuthToken" => "",
                    "getStatus" => new { isFirewalled = false, globalRelayEndPoint = new object[] { "127.0.0.1", 7144 } },
                    "getListeners" => new[] { new { port = 7144, globalAccepts = 7 } },
                    "getChannels" => new[] { new { channelId = "0123456789ABCDEF0123456789ABCDEF", status = new { totalRelays = 1 } } },
                    _ => new { }
                };
                return Results.Json(new { jsonrpc = "2.0", id = 1, result });
            });
            app.MapGet("/stream/{id}", async (HttpContext context) =>
            {
                context.Response.ContentType = "video/x-flv";
                if (flvPath is null) await Task.Delay(TimeSpan.FromSeconds(30), context.RequestAborted);
                else await context.Response.SendFileAsync(flvPath);
            });
            await app.StartAsync();
            return new FakePeerCastStation(app, $"http://127.0.0.1:{port}");
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
