using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PecaOneRelay;

internal sealed class AppState
{
    private readonly object _gate = new();
    private readonly string _path;
    private string? _tokenHash;
    private string? _pairCode;
    private DateTimeOffset _pairExpires;

    public AppState(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PecaOneRelay");
        Directory.CreateDirectory(DataDirectory);
        _path = Path.Combine(DataDirectory, "settings.json");
        if (File.Exists(_path))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<SavedSettings>(File.ReadAllText(_path));
                _tokenHash = saved?.TokenHash;
                PeerCastUrl = saved?.PeerCastUrl ?? PeerCastUrl;
                ListenPort = saved?.ListenPort is >= 1024 and <= 65535 ? saved.ListenPort : ListenPort;
            }
            catch (JsonException) { }
        }
    }

    public string DataDirectory { get; }
    public string PeerCastUrl { get; private set; } = "http://127.0.0.1:7144";
    public int ListenPort { get; private set; } = 17444;
    public bool HasPairedDevice { get { lock (_gate) return _tokenHash is not null; } }

    public void UpdatePeerCast(string url, int port)
    {
        var uri = new Uri(url);
        var localHost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address);
        if (uri.Scheme != Uri.UriSchemeHttp || !localHost || uri.AbsolutePath != "/" ||
            uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
            throw new ArgumentException("PeerCastStation はローカルアドレスだけ指定できます。");
        if (port is < 1024 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        lock (_gate)
        {
            PeerCastUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            ListenPort = port;
            Save();
        }
    }

    public (string Code, DateTimeOffset Expires) NewPairCode()
    {
        lock (_gate)
        {
            _pairCode = RandomToken(24);
            _pairExpires = DateTimeOffset.UtcNow.AddMinutes(5);
            return (_pairCode, _pairExpires);
        }
    }

    public string? Pair(string code)
    {
        lock (_gate)
        {
            if (_pairCode is null || DateTimeOffset.UtcNow >= _pairExpires || !FixedEquals(_pairCode, code))
                return null;
            _pairCode = null;
            var token = RandomToken(32);
            _tokenHash = Hash(token);
            Save();
            return token;
        }
    }

    public void Revoke()
    {
        lock (_gate)
        {
            _tokenHash = null;
            _pairCode = null;
            Save();
        }
    }

    public bool Authenticate(string? authorization)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal)) return false;
        var token = authorization[7..];
        lock (_gate) return _tokenHash is not null && FixedEquals(_tokenHash, Hash(token));
    }

    private void Save()
    {
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new SavedSettings(_tokenHash, PeerCastUrl, ListenPort)));
        File.Move(temp, _path, true);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool FixedEquals(string left, string right) => CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
    private static string RandomToken(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record SavedSettings(string? TokenHash, string PeerCastUrl, int ListenPort);
}

internal static class TailscaleAddress
{
    public static IPAddress? Find()
    {
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus != OperationalStatus.Up ||
                !(network.Name.Contains("Tailscale", StringComparison.OrdinalIgnoreCase) ||
                  network.Description.Contains("Tailscale", StringComparison.OrdinalIgnoreCase))) continue;
            foreach (var address in network.GetIPProperties().UnicastAddresses)
            {
                var ip = address.Address;
                if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                var octets = ip.GetAddressBytes();
                if (octets[0] == 100 && octets[1] is >= 64 and <= 127) return ip;
            }
        }
        return null;
    }
}
