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

    // A second AppImage (upgrade) loses the single-instance lock and exits
    // before MainViewModel can Register(). Retarget an existing login entry
    // to this image so the next session does not keep the old path.
    public static void RefreshCurrentAutostart()
    {
        if (!OperatingSystem.IsLinux())
            return;

        try
        {
            new LinuxStartupRegistration().RefreshRegisteredExecutable();
        }
        catch
        {
        }
    }

    internal void RefreshRegisteredExecutable()
    {
        if (!File.Exists(DesktopPath))
            return;

        var existing = File.ReadAllText(DesktopPath);
        var exePath = ResolveExecutablePath(
            Environment.GetEnvironmentVariable("APPIMAGE"),
            Environment.ProcessPath,
            File.Exists);
        if (!TryRefreshDesktop(existing, exePath, out var updated))
            return;

        File.WriteAllText(DesktopPath, updated);
    }

    internal static bool TryRefreshDesktop(string existingText, string exePath, out string updatedText)
    {
        updatedText = string.Empty;
        if (string.IsNullOrWhiteSpace(existingText) || string.IsNullOrWhiteSpace(exePath))
            return false;
        if (!IsAppImagePath(exePath))
            return false;
        if (!NeedsRefresh(existingText, exePath))
            return false;

        updatedText = BuildDesktopEntry(exePath, HasBackgroundArgument(existingText));
        return true;
    }

    internal static bool NeedsRefresh(string existingText, string exePath)
    {
        var registered = TryGetRegisteredExecutable(existingText);
        return !string.Equals(registered, exePath, StringComparison.Ordinal);
    }

    internal static bool HasBackgroundArgument(string desktopText) =>
        desktopText.Contains("--background", StringComparison.Ordinal);

    internal static bool IsAppImagePath(string path) =>
        path.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase);

    internal static string? TryGetRegisteredExecutable(string desktopText)
    {
        const string prefix = "TryExec=";
        using var reader = new StringReader(desktopText);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return line[prefix.Length..].Trim();
        }

        return null;
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
