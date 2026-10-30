using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace LyricodCMHelper;

/// <summary>
/// Fetches what gets installed, each checked by SHA-256 before it's used. BetterNCM is no longer
/// updated, so its last release is checked against a fixed hash; InfLink-rs is its latest release,
/// checked against the hash GitHub lists for that file, which shows it's exactly what InfLink-rs's
/// author uploaded.
/// </summary>
static class Downloads
{
    public const string PluginName = "InfLink-rs.plugin";
    public const string InfLinkRepository = "apoint123/inflink-rs";

    // BetterNCM 1.3.4, its last release (the project continues as chromatic), for 64-bit NetEase Cloud Music 3.x.
    static readonly Uri BetterNcmUrl = new("https://github.com/std-microblock/chromatic/releases/download/1.3.4/BetterNCMII.dll");
    const string BetterNcmSha256 = "A7C77AF418D7940E63FAA58EA036FBA1F4BAAD497947109EA52ED78C8E86608F";
    static readonly Uri InfLinkRelease = new($"https://api.github.com/repos/{InfLinkRepository}/releases/latest");

    /// <summary>Nothing it downloads is anywhere near this big.</summary>
    const long MaxBytes = 100L * 1024 * 1024;

    public static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Lyricod-CM-Helper/1.0 (+https://github.com/darkdust-c/Lyricod-CM-Helper)");
        return http;
    }

    public static Task<string> BetterNcmAsync(HttpClient http, string folder) =>
        FetchAsync(http, BetterNcmUrl, Path.Combine(folder, "BetterNCMII.dll"), "BetterNCM", BetterNcmSha256);

    public static async Task<string> InfLinkAsync(HttpClient http, string folder)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, InfLinkRelease);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        string json;
        try
        {
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new HelperException($"Couldn't look up InfLink-rs's latest release. {e.Message}");
        }
        var asset = Release.FindAsset(json, PluginName, InfLinkRepository)
            ?? throw new HelperException($"InfLink-rs's latest release has no {PluginName}.");
        if (asset.Sha256 == null) throw new HelperException("GitHub lists no checksum for InfLink-rs's latest release, so it can't be checked.");
        string path = await FetchAsync(http, asset.Url, Path.Combine(folder, PluginName), "InfLink-rs", asset.Sha256);
        Plugin.Check(path, "InfLink-rs");
        return path;
    }

    /// <summary>Downloads <paramref name="url"/> to <paramref name="path"/>; with <paramref name="sha256"/>, the file must match it.</summary>
    public static async Task<string> FetchAsync(HttpClient http, Uri url, string path, string what, string? sha256)
    {
        if (url.Scheme != Uri.UriSchemeHttps) throw new HelperException($"{what}'s download isn't secure (HTTPS).");
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
                throw new HelperException($"{what}'s download was redirected somewhere insecure.");
            if (response.Content.Headers.ContentLength > MaxBytes) throw new HelperException($"{what}'s download is far bigger than expected.");
            await using var body = await response.Content.ReadAsStreamAsync();
            await using var file = File.Create(path);
            var buffer = new byte[81920];
            long total = 0;
            for (int read; (read = await body.ReadAsync(buffer)) > 0;)
            {
                total += read;
                if (total > MaxBytes) throw new HelperException($"{what}'s download is far bigger than expected.");
                await file.WriteAsync(buffer.AsMemory(0, read));
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException)
        {
            throw new HelperException($"Couldn't download {what}. {e.Message}");
        }
        if (sha256 != null && !Matches(path, sha256))
            throw new HelperException($"{what}'s download didn't match its checksum, so it wasn't installed.");
        return path;
    }

    public static bool Matches(string path, string sha256)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(file)).Equals(sha256, StringComparison.OrdinalIgnoreCase);
    }
}
