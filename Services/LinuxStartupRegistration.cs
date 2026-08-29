namespace CursorUsageProgress.Services;

public sealed class LinuxStartupRegistration : IStartupRegistration
{
    private static string DesktopPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autostart",
        "cursor-usage-progress.desktop");

    public bool IsRegistered => File.Exists(DesktopPath);

    public void Register(bool startInTray)
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine executable path");
        var exec = startInTray
            ? $"\"{exePath}\" --background"
            : $"\"{exePath}\"";

        var desktop = $"""
            [Desktop Entry]
            Type=Application
            Name=Cursor Usage Progress
            Exec={exec}
            X-GNOME-Autostart-enabled=true
            Hidden=false
            """;

        var directory = Path.GetDirectoryName(DesktopPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(DesktopPath, desktop);
    }

    public void Unregister()
    {
        if (File.Exists(DesktopPath))
            File.Delete(DesktopPath);
    }
}
