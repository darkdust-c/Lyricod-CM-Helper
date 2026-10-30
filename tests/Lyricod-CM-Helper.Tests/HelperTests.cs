using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace LyricodCMHelper.Tests;

public class OptionsTests
{
    static readonly string[] Install = ["install", "--netease", @"C:\Program Files\NetEase\CloudMusic", "--data", @"C:\betterncm", "--result", @"C:\Temp\result.json"];

    [Fact]
    public void ReadsAnInstall()
    {
        var options = Options.Parse(Install);
        if (!OperatingSystem.IsWindows()) return; // the paths are Windows paths
        Assert.NotNull(options);
        Assert.Equal(HelperAction.Install, options.Action);
        Assert.Equal(@"C:\Program Files\NetEase\CloudMusic", options.NetEase);
        Assert.Equal(@"C:\betterncm", options.Data);
        Assert.Equal(@"C:\Temp\result.json", options.ResultPath);
    }

    [Fact]
    public void ReadsARemove()
    {
        string root = Path.GetTempPath();
        var options = Options.Parse(["remove", "--netease", root, "--data", root, "--result", Path.Combine(root, "r.json")]);
        Assert.Equal(HelperAction.Remove, options?.Action);
    }

    [Theory]
    [InlineData]
    [InlineData("uninstall")]
    [InlineData("install", "--netease")]
    [InlineData("install", "--netease", "relative", "--data", "relative", "--result", "relative")]
    [InlineData("install", "--other", "x")]
    public void RejectsAnythingElse(params string[] args) => Assert.Null(Options.Parse(args));

    [Fact]
    public void NeedsEveryFolder()
    {
        string root = Path.GetTempPath();
        Assert.Null(Options.Parse(["install", "--netease", root, "--result", Path.Combine(root, "r.json")]));
    }
}

public class ReleaseTests
{
    const string Repository = "apoint123/inflink-rs";
    const string Hash = "7d0b7b863b072a99b88eb614c788099315e690920889aaca22685d3663661468";

    static string Json(string name = "InfLink-rs.plugin", string url = $"https://github.com/{Repository}/releases/download/3.4.0/InfLink-rs.plugin",
        string? digest = "sha256:" + Hash) =>
        $$"""
        {"tag_name":"3.4.0","assets":[
          {"name":"other.zip","browser_download_url":"https://github.com/{{Repository}}/releases/download/3.4.0/other.zip"},
          {"name":"{{name}}","browser_download_url":"{{url}}"{{(digest == null ? "" : $",\"digest\":\"{digest}\"")}}}
        ]}
        """;

    [Fact]
    public void FindsThePluginAndItsChecksum()
    {
        var asset = Release.FindAsset(Json(), "InfLink-rs.plugin", Repository);
        Assert.NotNull(asset);
        Assert.Equal($"https://github.com/{Repository}/releases/download/3.4.0/InfLink-rs.plugin", asset.Url.ToString());
        Assert.Equal(Hash, asset.Sha256);
    }

    [Fact]
    public void AMissingChecksumIsNull() =>
        Assert.Null(Release.FindAsset(Json(digest: null), "InfLink-rs.plugin", Repository)?.Sha256);

    [Theory]
    [InlineData("md5:0123")]
    [InlineData("sha256:xyz")]
    public void AnOddChecksumIsNull(string digest) =>
        Assert.Null(Release.FindAsset(Json(digest: digest), "InfLink-rs.plugin", Repository)?.Sha256);

    [Theory]
    [InlineData("http://github.com/apoint123/inflink-rs/releases/download/3.4.0/InfLink-rs.plugin")]
    [InlineData("https://example.com/apoint123/inflink-rs/releases/download/3.4.0/InfLink-rs.plugin")]
    [InlineData("https://github.com/someone/else/releases/download/3.4.0/InfLink-rs.plugin")]
    [InlineData("https://github.com:8443/apoint123/inflink-rs/releases/download/3.4.0/InfLink-rs.plugin")]
    public void OnlyFromTheRepositorysOwnReleases(string url) =>
        Assert.Null(Release.FindAsset(Json(url: url), "InfLink-rs.plugin", Repository));

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"assets\":{}}")]
    public void OddRepliesFindNothing(string json) => Assert.Null(Release.FindAsset(json, "InfLink-rs.plugin", Repository));

    [Fact]
    public void AnotherNameFindsNothing() => Assert.Null(Release.FindAsset(Json(name: "InfLink.plugin"), "InfLink-rs.plugin", Repository));
}

public class FileTests
{
    static string Temp(string name) => Path.Combine(Directory.CreateTempSubdirectory("cm-helper-tests-").FullName, name);

    static string Zip(string? manifest)
    {
        string path = Temp("plugin.plugin");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            zip.CreateEntry("index.js");
            if (manifest != null)
                using (var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open())) writer.Write(manifest);
        }
        return path;
    }

    [Fact]
    public void APluginNamedRightPasses() => Plugin.Check(Zip("{\"name\":\"InfLink-rs\",\"version\":\"3.4.0\"}"), "InfLink-rs");

    [Theory]
    [InlineData("{\"name\":\"InfLink\"}")]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? manifest) =>
        Assert.Throws<HelperException>(() => Plugin.Check(Zip(manifest), "InfLink-rs"));

    static void MakePlugin(string folder, string file, string? manifest)
    {
        using var zip = ZipFile.Open(Path.Combine(folder, file), ZipArchiveMode.Create);
        if (manifest != null)
            using (var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open())) writer.Write(manifest);
    }

    [Fact]
    public void FindsEveryCopyOfAPluginWhateverItsFileIsCalled()
    {
        string folder = Directory.CreateTempSubdirectory("cm-helper-plugins-").FullName;
        MakePlugin(folder, "InfLinkrs-3.4.0.plugin", "{\"name\":\"InfLink-rs\",\"version\":\"3.4.0\"}"); // from BetterNCM's store
        MakePlugin(folder, "InfLink-rs.plugin", "{\"name\":\"InfLink-rs\"}");
        MakePlugin(folder, "InfLink-1.1.1.plugin", "{\"name\":\"InfLink\"}"); // the older plugin, not this one
        MakePlugin(folder, "PluginMarket.plugin", "{\"name\":\"PluginMarket\"}");
        File.WriteAllText(Path.Combine(folder, "broken.plugin"), "not a zip");
        var copies = Plugin.Copies(folder, "InfLink-rs").Select(Path.GetFileName).Order().ToList();
        Assert.Equal(["InfLink-rs.plugin", "InfLinkrs-3.4.0.plugin"], copies);
        Assert.Empty(Plugin.Copies(Path.Combine(folder, "missing"), "InfLink-rs"));
    }

    [Fact]
    public void ANonZipIsRefused()
    {
        string path = Temp("plugin.plugin");
        File.WriteAllText(path, "not a zip");
        Assert.Throws<HelperException>(() => Plugin.Check(path, "InfLink-rs"));
    }

    [Fact]
    public void ChecksumsCompareIgnoringCase()
    {
        string path = Temp("file.bin");
        File.WriteAllText(path, "Lyricod");
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Lyricod")));
        Assert.True(Downloads.Matches(path, hash.ToLowerInvariant()));
        Assert.False(Downloads.Matches(path, new string('0', 64)));
    }

    static MemoryStream Exe(ushort machine, int header = 0x80)
    {
        var bytes = new byte[header + 6];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BitConverter.GetBytes(header).CopyTo(bytes, 0x3C);
        "PE\0\0"u8.ToArray().CopyTo(bytes, header);
        BitConverter.GetBytes(machine).CopyTo(bytes, header + 4);
        return new MemoryStream(bytes);
    }

    [Theory]
    [InlineData(PeFile.Amd64)]
    [InlineData(PeFile.I386)]
    [InlineData(PeFile.Arm64)]
    public void ReadsTheExesMachine(ushort machine) => Assert.Equal(machine, PeFile.Machine(Exe(machine)));

    [Fact]
    public void ANonExeHasNoMachine()
    {
        Assert.Equal(0, PeFile.Machine(new MemoryStream(Encoding.ASCII.GetBytes("just some text, nothing like an exe at all here"))));
        // A header said to be past the end of the file.
        var truncated = Exe(PeFile.Amd64).ToArray();
        BitConverter.GetBytes(0x7FFF_0000).CopyTo(truncated, 0x3C);
        Assert.Equal(0, PeFile.Machine(new MemoryStream(truncated)));
    }
}
