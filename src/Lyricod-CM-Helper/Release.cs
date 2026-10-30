using System.IO.Compression;
using System.Text.Json;

namespace LyricodCMHelper;

/// <param name="Sha256">The file's SHA-256 as GitHub lists it (hex), or null when it lists none.</param>
sealed record ReleaseAsset(Uri Url, string? Sha256);

/// <summary>Reads a release from GitHub's API.</summary>
static class Release
{
    /// <summary>
    /// The file called <paramref name="name"/> in a release, if it's downloaded from that repository's
    /// own releases on github.com.
    /// </summary>
    public static ReleaseAsset? FindAsset(string json, string name, string repository)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return null;
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.ValueKind != JsonValueKind.Object || Text(asset, "name") != name) continue;
                if (!Uri.TryCreate(Text(asset, "browser_download_url"), UriKind.Absolute, out var url)
                    || url.Scheme != Uri.UriSchemeHttps || !url.IsDefaultPort || url.UserInfo.Length != 0
                    || !url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                    || !url.AbsolutePath.StartsWith($"/{repository}/releases/download/", StringComparison.OrdinalIgnoreCase)) return null;
                string? digest = Text(asset, "digest");
                string? sha256 = digest is { Length: 71 } && digest.StartsWith("sha256:", StringComparison.Ordinal)
                    && digest.AsSpan(7).ContainsOnlyHex() ? digest[7..] : null;
                return new ReleaseAsset(url, sha256);
            }
            return null;
        }
        catch (JsonException) { return null; }
    }

    static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

static class HexExtensions
{
    public static bool ContainsOnlyHex(this ReadOnlySpan<char> text)
    {
        foreach (char c in text) if (!char.IsAsciiHexDigit(c)) return false;
        return true;
    }
}

/// <summary>A BetterNCM plugin is a zip with a manifest.json naming it.</summary>
static class Plugin
{
    public static void Check(string path, string expectedName)
    {
        string? name = null;
        try
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("manifest.json");
            if (entry != null)
            {
                using var stream = entry.Open();
                using var manifest = JsonDocument.Parse(stream);
                if (manifest.RootElement.ValueKind == JsonValueKind.Object
                    && manifest.RootElement.TryGetProperty("name", out var value) && value.ValueKind == JsonValueKind.String)
                    name = value.GetString();
            }
        }
        catch (Exception e) when (e is InvalidDataException or JsonException or IOException) { }
        if (name != expectedName) throw new HelperException($"The download isn't the {expectedName} plugin.");
    }
}
