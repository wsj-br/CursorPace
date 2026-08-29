namespace CursorUsageProgress.Services;

public static class WebViewProfilePaths
{
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorUsageProgress");

    public static string ProfileDirectory { get; } = Path.Combine(
        AppDataDirectory,
        OperatingSystem.IsWindows() ? "WebView2" : "WebView");

    public static string CacheDirectory { get; } = Path.Combine(ProfileDirectory, "Cache");
}
