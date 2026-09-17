using System.Diagnostics;

namespace CursorPace.Services;

public static class LinuxDesktopIntegration
{
    public const string StartupWmClass = "CursorPace";
    public const string DesktopFileName = "CursorPace.desktop";
    public const string LegacyDesktopFileName = "cursor-pace.desktop";
    public const string IconName = "cursor-pace";

    public static void EnsureUserEntry()
    {
        if (!OperatingSystem.IsLinux())
            return;

        try
        {
            var exePath = LinuxStartupRegistration.ResolveExecutablePath(
                Environment.GetEnvironmentVariable("APPIMAGE"),
                Environment.ProcessPath,
                File.Exists);
            var localShare = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var hicolor = Path.Combine(localShare, "icons", "hicolor");
            Install(
                applicationsDirectory: Path.Combine(localShare, "applications"),
                iconDestinationPath: Path.Combine(hicolor, "256x256", "apps", IconName + ".png"),
                execPath: exePath,
                iconSourcePath: ResolveBundledIconPath());
            TryUpdateIconCache(hicolor);
        }
        catch
        {
        }
    }

    public static void Install(
        string applicationsDirectory,
        string iconDestinationPath,
        string execPath,
        string? iconSourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationsDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconDestinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(execPath);

        var installedIcon = false;
        if (!string.IsNullOrWhiteSpace(iconSourcePath) && File.Exists(iconSourcePath))
        {
            var iconDirectory = Path.GetDirectoryName(iconDestinationPath);
            if (!string.IsNullOrEmpty(iconDirectory))
                Directory.CreateDirectory(iconDirectory);

            File.Copy(iconSourcePath, iconDestinationPath, overwrite: true);
            installedIcon = true;
        }

        Directory.CreateDirectory(applicationsDirectory);
        File.WriteAllText(
            Path.Combine(applicationsDirectory, DesktopFileName),
            BuildDesktopEntry(execPath, installedIcon ? iconDestinationPath : IconName));

        var legacy = Path.Combine(applicationsDirectory, LegacyDesktopFileName);
        if (File.Exists(legacy))
            File.Delete(legacy);
    }

    public static string BuildDesktopEntry(string execPath, string iconPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(execPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);
        return $"""
            [Desktop Entry]
            Type=Application
            Name=Cursor Pace
            Comment=Track Cursor quota across a billing cycle
            Exec="{execPath}"
            TryExec={execPath}
            Icon={iconPath}
            StartupWMClass={StartupWmClass}
            Categories=Utility;
            Terminal=false

            """;
    }

    internal static string? ResolveBundledIconPath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "Assets", "cursor_pace.png");
        return File.Exists(candidate) ? candidate : null;
    }

    internal static void TryUpdateIconCache(string hicolorDirectory)
    {
        if (string.IsNullOrWhiteSpace(hicolorDirectory) || !Directory.Exists(hicolorDirectory))
            return;

        foreach (var tool in new[] { "gtk-update-icon-cache", "gtk-update-icon-cache-3.0" })
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = tool,
                    ArgumentList = { "-f", "-t", hicolorDirectory },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit(2000);
                return;
            }
            catch
            {
            }
        }
    }
}
