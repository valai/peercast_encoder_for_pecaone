using System.Diagnostics;
using System.Text.Json;

namespace PecaOneRelay;

internal sealed class Transcoder : IAsyncDisposable
{
    private Process? _process;
    public int? ExitCode => _process is { HasExited: true } process ? process.ExitCode : null;
    public Task? ExitTask { get; private set; }

    public static string ToolPath(string file)
    {
        var root = Environment.GetEnvironmentVariable("PECAONE_FFMPEG_DIR");
        return Path.Combine(root ?? Path.Combine(AppContext.BaseDirectory, "ffmpeg"), file);
    }

    public static async Task<(bool HasVideo, bool HasAudio)> ProbeAsync(Uri source, CancellationToken cancellationToken)
    {
        var executable = ToolPath("ffprobe.exe");
        if (!File.Exists(executable)) throw new FileNotFoundException("同梱 FFmpeg が見つかりません", executable);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var arg in new[] { "-v", "error", "-rw_timeout", "15000000", "-show_entries", "stream=codec_type", "-of", "json", Input(source) })
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("ffprobe を起動できません");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errors = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            await errors;
            if (process.ExitCode != 0) throw new InvalidOperationException("PeerCastStation の FLV を解析できません");
            using var document = JsonDocument.Parse(await output);
            if (!document.RootElement.TryGetProperty("streams", out var streams)) return (false, false);
            var types = streams.EnumerateArray().Select(stream => stream.GetProperty("codec_type").GetString()).ToArray();
            return (types.Contains("video"), types.Contains("audio"));
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw new TimeoutException("FLV の映像情報を25秒以内に取得できませんでした");
        }
    }

    public void Start(Uri source, string outputDirectory, bool hasAudio)
    {
        var executable = ToolPath("ffmpeg.exe");
        if (!File.Exists(executable)) throw new FileNotFoundException("同梱 FFmpeg が見つかりません", executable);
        foreach (var profile in new[] { "high", "medium", "low" })
            Directory.CreateDirectory(Path.Combine(outputDirectory, profile));
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
        };
        foreach (var arg in BuildArguments(source, outputDirectory, hasAudio)) start.ArgumentList.Add(arg);
        _process = Process.Start(start) ?? throw new InvalidOperationException("FFmpeg を起動できません");
        _process.ErrorDataReceived += (_, _) => { }; // Drain stderr without logging the source authentication token.
        _process.BeginErrorReadLine();
        ExitTask = _process.WaitForExitAsync();
    }

    internal static IReadOnlyList<string> BuildArguments(Uri source, string outputDirectory, bool hasAudio)
    {
        var filters = "[0:v:0]split=3[vh][vm][vl];" +
            "[vh]scale=w='min(iw,1280)':h='min(ih,720)':force_original_aspect_ratio=decrease:force_divisible_by=2,format=yuv420p[h];" +
            "[vm]scale=w='min(iw,854)':h='min(ih,480)':force_original_aspect_ratio=decrease:force_divisible_by=2,format=yuv420p[m];" +
            "[vl]scale=w='min(iw,640)':h='min(ih,360)':force_original_aspect_ratio=decrease:force_divisible_by=2,format=yuv420p[l]";
        var args = new List<string>
        {
            "-nostdin", "-hide_banner", "-loglevel", "error", "-rw_timeout", "15000000",
            "-i", Input(source), "-filter_complex", filters
        };
        foreach (var video in new[] { "[h]", "[m]", "[l]" })
        {
            args.AddRange(["-map", video]);
            if (hasAudio) args.AddRange(["-map", "0:a:0"]);
        }
        args.AddRange(["-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p",
            "-b:v:0", "2500k", "-maxrate:v:0", "2500k", "-bufsize:v:0", "5000k",
            "-b:v:1", "1200k", "-maxrate:v:1", "1200k", "-bufsize:v:1", "2400k",
            "-b:v:2", "500k", "-maxrate:v:2", "500k", "-bufsize:v:2", "1000k",
            "-force_key_frames:v:0", "expr:gte(t,n_forced*3)",
            "-force_key_frames:v:1", "expr:gte(t,n_forced*3)",
            "-force_key_frames:v:2", "expr:gte(t,n_forced*3)"]);
        if (hasAudio) args.AddRange(["-c:a", "aac", "-b:a:0", "128k", "-b:a:1", "96k", "-b:a:2", "64k"]);
        args.AddRange(["-f", "hls", "-hls_time", "3", "-hls_list_size", "6",
            "-hls_segment_type", "mpegts", "-hls_flags", "delete_segments+temp_file+independent_segments",
            "-master_pl_name", "master.m3u8", "-master_pl_publish_rate", "1",
            "-var_stream_map", hasAudio
                ? "v:0,a:0,name:high v:1,a:1,name:medium v:2,a:2,name:low"
                : "v:0,name:high v:1,name:medium v:2,name:low",
            "-hls_segment_filename", FfmpegPath(outputDirectory, "%v", "segment_%06d.ts"),
            FfmpegPath(outputDirectory, "%v", "index.m3u8")]);
        return args;
    }

    // FFmpeg writes the variant playlist path into master.m3u8 verbatim.
    // Windows path separators there are invalid HLS URI separators.
    private static string FfmpegPath(params string[] parts) => Path.Combine(parts).Replace('\\', '/');

    public async ValueTask DisposeAsync()
    {
        var process = Interlocked.Exchange(ref _process, null);
        if (process is null) return;
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        try { await process.WaitForExitAsync(); } catch (InvalidOperationException) { }
        process.Dispose();
    }

    private static string Input(Uri source) => source.IsFile ? source.LocalPath : source.ToString();
}
