namespace LyricodCMHelper;

/// <summary>
/// Sets up NetEase Cloud Music sync for Lyricod: installs BetterNCM and the InfLink-rs plugin into
/// NetEase Cloud Music, or removes them. Lyricod downloads this helper only when asked, runs it as
/// administrator and reads what happened from the result file.
/// </summary>
static class Program
{
    const string Usage =
        "Lyricod CM Helper sets up NetEase Cloud Music sync for Lyricod. Lyricod runs it for you: "
        + "open Lyricod's settings, then General > NetEase Cloud Music sync.";

    static async Task<int> Main(string[] args)
    {
        if (Options.Parse(args) is not { } options)
        {
            NativeMethods.ShowMessage("Lyricod CM Helper", Usage);
            return 2;
        }
        string? error = null;
        try
        {
            if (!Environment.IsPrivilegedProcess) throw new HelperException("The helper needs administrator permission.");
            if (options.Action == HelperAction.Install) await Setup.InstallAsync(options);
            else Setup.Remove(options);
        }
        catch (HelperException e) { error = e.Message; }
        catch (Exception e) { error = $"{e.GetType().Name}: {e.Message}"; }
        Result.Write(options.ResultPath, error);
        return error == null ? 0 : 1;
    }
}

/// <summary>A step failed in a way worth telling the user as is.</summary>
sealed class HelperException(string message) : Exception(message);
