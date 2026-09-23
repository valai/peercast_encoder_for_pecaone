using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PecaOneRelay;

internal sealed record PeerCastHealth(bool Online, bool RelayReachable, int DownstreamRelays, string Message);

internal sealed class PeerCastClient : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly AppState _settings;

    public PeerCastClient(AppState settings) => _settings = settings;

    public static bool ValidChannel(string? id) => id is not null &&
        Regex.IsMatch(id, "^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant) &&
        !id.Equals(new string('0', 32), StringComparison.Ordinal);

    public static bool ValidTracker(string? tracker)
    {
        if (string.IsNullOrWhiteSpace(tracker) || tracker.Length > 255 ||
            tracker.Any(char.IsWhiteSpace) || !Uri.TryCreate("http://" + tracker, UriKind.Absolute, out var uri)) return false;
        return uri.Host.Length > 0 && uri.Port is >= 1 and <= 65535 &&
            uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.UserInfo.Length == 0 &&
            tracker.Contains(':');
    }

    public async Task<PeerCastHealth> HealthAsync(string? channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var status = await CallAsync("getStatus", null, cancellationToken);
            var root = status.RootElement;
            var relayPort = 0;
            var reachable = root.TryGetProperty("isFirewalled", out var firewall) && firewall.ValueKind == JsonValueKind.False &&
                root.TryGetProperty("globalRelayEndPoint", out var endpoint) && endpoint.ValueKind == JsonValueKind.Array &&
                endpoint.GetArrayLength() >= 2 && endpoint[1].TryGetInt32(out relayPort) && relayPort > 0;
            if (reachable)
            {
                using var listeners = await CallAsync("getListeners", null, cancellationToken);
                reachable = listeners.RootElement.ValueKind == JsonValueKind.Array &&
                    listeners.RootElement.EnumerateArray().Any(listener =>
                        listener.TryGetProperty("port", out var port) && port.GetInt32() == relayPort &&
                        listener.TryGetProperty("globalAccepts", out var accepts) && (accepts.GetInt32() & 2) != 0);
            }
            var count = 0;
            if (channelId is not null)
            {
                using var channels = await CallAsync("getChannels", null, cancellationToken);
                foreach (var channel in channels.RootElement.EnumerateArray())
                {
                    if (!channel.TryGetProperty("channelId", out var id) ||
                        !id.GetString()!.Equals(channelId, StringComparison.OrdinalIgnoreCase)) continue;
                    if (channel.TryGetProperty("status", out var detail) && detail.TryGetProperty("totalRelays", out var relays))
                        count = relays.GetInt32();
                    break;
                }
            }
            return new PeerCastHealth(true, reachable, count,
                reachable ? $"リレー待受可能・下流 {count}" : "PeerCastStation の外部リレーポートを確認できません");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            return new PeerCastHealth(false, false, 0, "PeerCastStation に接続できません: " + ex.Message);
        }
    }

    public async Task<Uri> StreamUriAsync(string channelId, string tracker, CancellationToken cancellationToken)
    {
        if (!ValidChannel(channelId) || !ValidTracker(tracker)) throw new ArgumentException("FLV番組のIDまたはトラッカーが不正です");
        using var _ = await CallAsync("getVersionInfo", null, cancellationToken);
        string? auth = null;
        try
        {
            using var token = await CallAsync("getAuthToken", null, cancellationToken);
            if (token.RootElement.ValueKind == JsonValueKind.String) auth = token.RootElement.GetString();
        }
        catch (InvalidOperationException) { }
        var query = "tip=" + Uri.EscapeDataString(tracker);
        if (!string.IsNullOrEmpty(auth)) query += "&auth=" + Uri.EscapeDataString(auth);
        return new Uri($"{_settings.PeerCastUrl}/stream/{channelId.ToUpperInvariant()}?{query}");
    }

    private async Task<JsonDocument> CallAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?> { ["jsonrpc"] = "2.0", ["id"] = 1, ["method"] = method };
        if (parameters is not null) body["params"] = parameters;
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.PeerCastUrl + "/api/1")
        {
            Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (document.RootElement.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException(error.ToString());
        return JsonDocument.Parse(document.RootElement.GetProperty("result").GetRawText());
    }

    public void Dispose() => _http.Dispose();
}
