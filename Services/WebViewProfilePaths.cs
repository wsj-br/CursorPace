namespace CursorPace.Services;

public static class WebViewProfilePaths
{
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorPace");

    // An AppImage bundles its own WebKitGTK build. If it wrote to the same
    // profile folder as a system-WebKitGTK run (a dev build, or a
    // non-AppImage Linux install), the two WebKit versions can fail to read
    // each other's cookie database and the session looks lost even though
    // nothing deleted it. Give AppImage runs a separate folder so the two
    // never share one cookie store.
    private static bool IsRunningFromAppImage =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE"));

    public static string ProfileDirectory { get; } = Path.Combine(
        AppDataDirectory,
        OperatingSystem.IsWindows()
            ? "WebView2"
            : IsRunningFromAppImage
                ? "WebView-AppImage"
                : "WebView");

    public static string CacheDirectory { get; } = Path.Combine(ProfileDirectory, "Cache");
}
