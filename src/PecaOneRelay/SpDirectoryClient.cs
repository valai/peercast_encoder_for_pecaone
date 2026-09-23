using System.Net;

namespace PecaOneRelay;

internal static class SpDirectoryClient
{
    private const int MaxBytes = 4 * 1024 * 1024;
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseProxy = false, // SP に Windows 本来の外部 IP を判定させる。
    }) { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task<byte[]> FetchAsync(CancellationToken cancellationToken)
    {
        var uri = new Uri("http://bayonet.ddo.jp/sp/index.txt");
        for (var redirects = 0; redirects <= 2; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or
                HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect)
            {
                var location = response.Headers.Location;
                if (location is null) throw new HttpRequestException("SP redirect has no location");
                var next = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if ((next.Scheme != Uri.UriSchemeHttp && next.Scheme != Uri.UriSchemeHttps) ||
                    !next.Host.Equals("bayonet.ddo.jp", StringComparison.OrdinalIgnoreCase) ||
                    next.AbsolutePath != "/sp/index.txt" || next.UserInfo.Length != 0 ||
                    next.Query.Length != 0 || next.Fragment.Length != 0 ||
                    next.Port != (next.Scheme == Uri.UriSchemeHttps ? 443 : 80))
                    throw new HttpRequestException("SP redirected outside its index.txt");
                uri = next;
                continue;
            }
            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"SP returned HTTP {(int)response.StatusCode}");
            if (response.Content.Headers.ContentLength > MaxBytes)
                throw new InvalidDataException("SP index.txt is too large");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, cancellationToken);
                if (count == 0) break;
                if (output.Length + count > MaxBytes)
                    throw new InvalidDataException("SP index.txt is too large");
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
        throw new HttpRequestException("SP redirected too many times");
    }
}
