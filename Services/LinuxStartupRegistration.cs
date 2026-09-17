namespace CursorPace.Services;

public sealed class LinuxStartupRegistration : IStartupRegistration
{
    private static string DesktopPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autostart",
        "cursor-pace.desktop");

    public bool IsRegistered => File.Exists(DesktopPath);

    public void Register(bool startInTray)
    {
        var exePath = ResolveExecutablePath(
            Environment.GetEnvironmentVariable("APPIMAGE"),
            Environment.ProcessPath,
            File.Exists);
        var directory = Path.GetDirectoryName(DesktopPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(DesktopPath, BuildDesktopEntry(exePath, startInTray));
    }

    public void Unregister()
    {
        if (File.Exists(DesktopPath))
            File.Delete(DesktopPath);
    }

    public static string BuildDesktopEntry(string exePath, bool startInTray)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exePath);
        var args = startInTray ? " --background" : string.Empty;
        // AppImageLauncher's binfmt helper can re-exec an AppImage through its
        // own .desktop file, which would start a second process at login.
        var exec = $"env APPIMAGELAUNCHER_DISABLE=1 \"{exePath}\"{args}";
        return $"""
            [Desktop Entry]
            Type=Application
            Name=Cursor Pace
            Exec={exec}
            TryExec={exePath}
            X-GNOME-Autostart-enabled=true
            Hidden=false
            StartupNotify=false

            """;
    }

    // AppImage sets ProcessPath to the FUSE mount under /tmp/.mount_*, which
    // disappears after reboot. The APPIMAGE env var is the stable .AppImage file.
    public static string ResolveExecutablePath(
        string? appImagePath,
        string? processPath,
        Func<string, bool> fileExists)
    {
        if (!string.IsNullOrWhiteSpace(appImagePath) && fileExists(appImagePath))
            return appImagePath;

        if (!string.IsNullOrWhiteSpace(processPath))
            return processPath;

        throw new InvalidOperationException("Cannot determine executable path");
    }
}
