using CursorPace.Services;

namespace CursorPace;

// Entry point with no Avalonia usings. Loading Program pulls Avalonia, which
// can initialize fontconfig before Main() sets FONTCONFIG_FILE.
internal static class Startup
{
    [STAThread]
    public static void Main(string[] args)
    {
        LinuxFontSetup.Apply();
        Program.Run(args);
    }
}
