namespace LyricodCMHelper;

enum HelperAction { Install, Remove }

/// <summary>
/// <c>install</c> or <c>remove</c>, then:
/// <c>--netease</c> NetEase Cloud Music's folder (where cloudmusic.exe is),
/// <c>--data</c> BetterNCM's data folder (BETTERNCM_PROFILE, or C:\betterncm), and
/// <c>--result</c> the file to write what happened to. Lyricod passes the folders it sees as the
/// signed-in user, which an administrator account may not share.
/// </summary>
sealed record Options(HelperAction Action, string NetEase, string Data, string ResultPath)
{
    public static Options? Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return null;
        HelperAction action;
        switch (args[0])
        {
            case "install": action = HelperAction.Install; break;
            case "remove": action = HelperAction.Remove; break;
            default: return null;
        }
        string? netease = null, data = null, result = null;
        for (int i = 1; i < args.Count; i += 2)
        {
            if (i + 1 >= args.Count) return null;
            string value = args[i + 1];
            switch (args[i])
            {
                case "--netease": netease = value; break;
                case "--data": data = value; break;
                case "--result": result = value; break;
                default: return null;
            }
        }
        if (!Full(netease) || !Full(data) || !Full(result)) return null;
        return new Options(action, netease!, data!, result!);
    }

    static bool Full(string? path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path);
}
