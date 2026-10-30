namespace LyricodCMHelper;

/// <summary>
/// What installing and removing do. Everything is downloaded and checked before NetEase Cloud Music
/// is touched, so a failed download leaves it as it was.
/// </summary>
static class Setup
{
    /// <summary>BetterNCM loads from NetEase's folder under this name.</summary>
    public const string LoaderName = "msimg32.dll";

    public static async Task InstallAsync(Options options)
    {
        NetEase.CheckFolder(options.NetEase);
        var work = Directory.CreateTempSubdirectory("Lyricod-CM-Helper-");
        try
        {
            using var http = Downloads.CreateClient();
            string betterNcm = await Downloads.BetterNcmAsync(http, work.FullName);
            string plugin = await Downloads.InfLinkAsync(http, work.FullName);
            await VcRuntime.EnsureAsync(http, work.FullName);
            NetEase.Close();
            Copy(betterNcm, Path.Combine(options.NetEase, LoaderName));
            string plugins = Path.Combine(options.Data, "plugins");
            Directory.CreateDirectory(plugins);
            Copy(plugin, Path.Combine(plugins, Downloads.PluginName));
        }
        finally
        {
            try { work.Delete(true); } catch (Exception) { /* left in the temp folder */ }
        }
    }

    /// <summary>Takes out what <see cref="InstallAsync"/> put in. BetterNCM's other plugins and data stay.</summary>
    public static void Remove(Options options)
    {
        NetEase.CheckFolder(options.NetEase);
        NetEase.Close();
        Delete(Path.Combine(options.NetEase, LoaderName));
        Delete(Path.Combine(options.Data, "plugins", Downloads.PluginName));
    }

    /// <summary>A file NetEase just let go of can stay locked for a moment: try a few times.</summary>
    static void Copy(string from, string to) => Retry(() => File.Copy(from, to, overwrite: true), $"Couldn't write {to}.");

    static void Delete(string path) => Retry(() => File.Delete(path), $"Couldn't delete {path}.");

    static void Retry(Action action, string failure)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (attempt == 5) throw new HelperException($"{failure} {e.Message}");
                Thread.Sleep(400 * attempt);
            }
        }
    }
}
