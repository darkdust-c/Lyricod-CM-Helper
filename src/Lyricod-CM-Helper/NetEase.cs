using System.Diagnostics;

namespace LyricodCMHelper;

static class NetEase
{
    public const string ExeName = "cloudmusic.exe";

    /// <summary>NetEase Cloud Music is in <paramref name="folder"/>, and it's the 64-bit 3.x that BetterNCM 1.3.4 fits.</summary>
    public static void CheckFolder(string folder)
    {
        string exe = Path.Combine(folder, ExeName);
        if (!File.Exists(exe)) throw new HelperException($"NetEase Cloud Music isn't in {folder}.");
        if (PeFile.Machine(exe) != PeFile.Amd64) throw new HelperException("Only the 64-bit NetEase Cloud Music (3.x) is supported.");
    }

    /// <summary>Closes NetEase Cloud Music so its files can be changed; Lyricod opens it again afterwards.</summary>
    public static void Close()
    {
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExeName)))
        {
            using (process)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(10_000);
                }
                catch (Exception) { /* already gone */ }
            }
        }
        var left = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExeName));
        bool running = left.Length > 0;
        foreach (var process in left) process.Dispose();
        if (running) throw new HelperException("Couldn't close NetEase Cloud Music.");
    }
}

/// <summary>Reads which processor an exe is built for.</summary>
static class PeFile
{
    public const ushort Amd64 = 0x8664, I386 = 0x014C, Arm64 = 0xAA64;

    /// <summary>The machine type from the exe's header, or 0 when it isn't one.</summary>
    public static ushort Machine(string path)
    {
        using var file = File.OpenRead(path);
        return Machine(file);
    }

    public static ushort Machine(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (stream.Length < 0x40 || reader.ReadUInt16() != 0x5A4D) return 0; // "MZ"
        stream.Position = 0x3C;
        uint header = reader.ReadUInt32();
        if (header > stream.Length - 6) return 0;
        stream.Position = header;
        if (reader.ReadUInt32() != 0x00004550) return 0; // "PE\0\0"
        return reader.ReadUInt16();
    }
}
