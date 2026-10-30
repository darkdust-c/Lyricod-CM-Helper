using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace LyricodCMHelper;

/// <summary>Whether Windows trusts a file's signature, and whose it is.</summary>
static class Signature
{
    /// <summary>Signed, Windows trusts the signature, and the signer is Microsoft Corporation.</summary>
    public static bool IsMicrosoft(string path)
    {
        if (!NativeMethods.IsTrusted(path)) return false;
        try
        {
#pragma warning disable SYSLIB0057 // the signer of an Authenticode file has no replacement API
            using var certificate = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            return certificate.Subject.Contains("O=Microsoft Corporation", StringComparison.Ordinal);
        }
        catch (Exception) { return false; }
    }
}

static partial class NativeMethods
{
    public static void ShowMessage(string title, string text) => MessageBoxW(0, text, title, 0x40); // MB_ICONINFORMATION

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBoxW(nint owner, string text, string caption, uint type);

    /// <summary>WinVerifyTrust's check of an Authenticode signature (no revocation check: the file just came from Microsoft over HTTPS).</summary>
    public static unsafe bool IsTrusted(string path)
    {
        fixed (char* file = path)
        {
            var fileInfo = new TrustFileInfo { Size = (uint)sizeof(TrustFileInfo), FilePath = (nint)file };
            var data = new TrustData
            {
                Size = (uint)sizeof(TrustData),
                UiChoice = 2, // WTD_UI_NONE
                RevocationChecks = 0, // WTD_REVOKE_NONE
                UnionChoice = 1, // WTD_CHOICE_FILE
                File = (nint)(&fileInfo),
                StateAction = 1, // WTD_STATEACTION_VERIFY
            };
            var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE"); // WINTRUST_ACTION_GENERIC_VERIFY_V2
            int result = WinVerifyTrust(-1, &action, &data); // INVALID_HANDLE_VALUE: no UI
            data.StateAction = 2; // WTD_STATEACTION_CLOSE
            WinVerifyTrust(-1, &action, &data);
            return result == 0;
        }
    }

    [LibraryImport("wintrust.dll")]
    private static unsafe partial int WinVerifyTrust(nint window, Guid* action, TrustData* data);

    [StructLayout(LayoutKind.Sequential)]
    struct TrustFileInfo
    {
        public uint Size;
        public nint FilePath;
        public nint File;
        public nint KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TrustData
    {
        public uint Size;
        public nint PolicyCallbackData;
        public nint SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public nint File;
        public uint StateAction;
        public nint StateData;
        public nint UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public nint SignatureSettings;
    }
}

/// <summary>What happened, for Lyricod: <c>{"ok":true}</c>, or <c>{"ok":false,"error":"…"}</c>.</summary>
static class Result
{
    public static void Write(string path, string? error)
    {
        try
        {
            using var stream = File.Create(path);
            using var json = new Utf8JsonWriter(stream);
            json.WriteStartObject();
            json.WriteBoolean("ok", error == null);
            if (error != null) json.WriteString("error", error);
            json.WriteEndObject();
        }
        catch (Exception) { /* Lyricod then goes by the exit code */ }
    }
}
