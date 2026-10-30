using System.Diagnostics;
using Microsoft.Win32;

namespace LyricodCMHelper;

/// <summary>
/// BetterNCM needs Microsoft's Visual C++ runtime (x64). If it's missing, it comes from Microsoft
/// and only runs when Windows confirms Microsoft signed it.
/// </summary>
static class VcRuntime
{
    static readonly Uri Download = new("https://aka.ms/vs/17/release/vc_redist.x64.exe");

    public static bool IsInstalled()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
        return key?.GetValue("Installed") is int installed && installed == 1;
    }

    public static async Task EnsureAsync(HttpClient http, string folder)
    {
        if (IsInstalled()) return;
        string path = await Downloads.FetchAsync(http, Download, Path.Combine(folder, "vc_redist.x64.exe"), "the Visual C++ runtime", sha256: null);
        if (!Signature.IsMicrosoft(path)) throw new HelperException("The Visual C++ runtime download isn't signed by Microsoft, so it wasn't run.");
        using var setup = Process.Start(new ProcessStartInfo(path, "/install /quiet /norestart") { UseShellExecute = false })
            ?? throw new HelperException("Couldn't start the Visual C++ runtime setup.");
        await setup.WaitForExitAsync();
        // 3010: installed, restart later; 1638: a newer version is already there.
        if (setup.ExitCode is not (0 or 3010 or 1638))
            throw new HelperException($"The Visual C++ runtime didn't install (code {setup.ExitCode}).");
    }
}
