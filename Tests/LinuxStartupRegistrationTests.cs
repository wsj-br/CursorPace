using CursorPace.Services;

namespace CursorPace.Tests;

public class LinuxStartupRegistrationTests
{
    [Fact]
    public void ResolveExecutablePath_PrefersExistingAppImageOverFuseMount()
    {
        var path = LinuxStartupRegistration.ResolveExecutablePath(
            "/home/user/Applications/CursorPace.AppImage",
            "/tmp/.mount_CursorXXXX/usr/bin/CursorPace",
            fileExists: candidate => candidate.EndsWith(".AppImage", StringComparison.Ordinal));

        Assert.Equal("/home/user/Applications/CursorPace.AppImage", path);
    }

    [Fact]
    public void ResolveExecutablePath_WhenAppImageMissing_UsesProcessPath()
    {
        var path = LinuxStartupRegistration.ResolveExecutablePath(
            "/tmp/.gone/CursorPace.AppImage",
            "/usr/bin/CursorPace",
            fileExists: _ => false);

        Assert.Equal("/usr/bin/CursorPace", path);
    }

    [Fact]
    public void ResolveExecutablePath_WhenAppImageUnset_UsesProcessPath()
    {
        var path = LinuxStartupRegistration.ResolveExecutablePath(
            null,
            "/home/user/src/CursorPace/bin/CursorPace",
            fileExists: _ => true);

        Assert.Equal("/home/user/src/CursorPace/bin/CursorPace", path);
    }

    [Fact]
    public void ResolveExecutablePath_WhenNeitherPathExists_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LinuxStartupRegistration.ResolveExecutablePath(null, null, _ => false));
    }

    [Fact]
    public void BuildDesktopEntry_WhenStartInTray_DisablesAppImageLauncherAndPassesBackground()
    {
        var text = LinuxStartupRegistration.BuildDesktopEntry(
            "/home/user/Applications/CursorPace.AppImage",
            startInTray: true);

        Assert.Contains("Exec=env APPIMAGELAUNCHER_DISABLE=1 \"/home/user/Applications/CursorPace.AppImage\" --background", text);
        Assert.Contains("TryExec=/home/user/Applications/CursorPace.AppImage", text);
        Assert.Contains("X-GNOME-Autostart-enabled=true", text);
        Assert.Contains("StartupNotify=false", text);
    }

    [Fact]
    public void BuildDesktopEntry_WhenStartInTrayOff_OmitsBackgroundArgument()
    {
        var text = LinuxStartupRegistration.BuildDesktopEntry(
            "/opt/CursorPace/CursorPace",
            startInTray: false);

        Assert.Contains("Exec=env APPIMAGELAUNCHER_DISABLE=1 \"/opt/CursorPace/CursorPace\"", text);
        Assert.DoesNotContain("--background", text);
    }

    [Fact]
    public void TryGetRegisteredExecutable_ReadsTryExec()
    {
        var text = LinuxStartupRegistration.BuildDesktopEntry(
            "/home/user/Applications/CursorPace-0.3.0-linux-x64.AppImage",
            startInTray: true);

        Assert.Equal(
            "/home/user/Applications/CursorPace-0.3.0-linux-x64.AppImage",
            LinuxStartupRegistration.TryGetRegisteredExecutable(text));
    }

    [Fact]
    public void TryRefreshDesktop_WhenAppImagePathChanged_RewritesAndKeepsBackground()
    {
        var existing = LinuxStartupRegistration.BuildDesktopEntry(
            "/home/user/Applications/CursorPace-0.3.0-linux-x64.AppImage",
            startInTray: true);
        var next = "/home/user/Applications/CursorPace-0.3.1-linux-x64.AppImage";

        Assert.True(LinuxStartupRegistration.TryRefreshDesktop(existing, next, out var updated));
        Assert.Equal(next, LinuxStartupRegistration.TryGetRegisteredExecutable(updated));
        Assert.Contains($"Exec=env APPIMAGELAUNCHER_DISABLE=1 \"{next}\" --background", updated);
    }

    [Fact]
    public void TryRefreshDesktop_WhenAppImagePathChanged_KeepsForegroundLaunch()
    {
        var existing = LinuxStartupRegistration.BuildDesktopEntry(
            "/old/CursorPace-0.3.0-linux-x64.AppImage",
            startInTray: false);

        Assert.True(LinuxStartupRegistration.TryRefreshDesktop(
            existing,
            "/new/CursorPace-0.3.1-linux-x64.AppImage",
            out var updated));
        Assert.False(LinuxStartupRegistration.HasBackgroundArgument(updated));
    }

    [Fact]
    public void TryRefreshDesktop_WhenPathUnchanged_ReturnsFalse()
    {
        var path = "/home/user/Applications/CursorPace.AppImage";
        var existing = LinuxStartupRegistration.BuildDesktopEntry(path, startInTray: true);

        Assert.False(LinuxStartupRegistration.TryRefreshDesktop(existing, path, out _));
    }

    [Fact]
    public void TryRefreshDesktop_WhenCurrentExeIsNotAppImage_LeavesAutostart()
    {
        var existing = LinuxStartupRegistration.BuildDesktopEntry(
            "/home/user/Applications/CursorPace.AppImage",
            startInTray: true);

        Assert.False(LinuxStartupRegistration.TryRefreshDesktop(
            existing,
            "/usr/lib/dotnet/dotnet",
            out _));
    }

    [Fact]
    public void TryRefreshDesktop_WhenAutostartMissing_ReturnsFalse()
    {
        Assert.False(LinuxStartupRegistration.TryRefreshDesktop(
            "",
            "/home/user/Applications/CursorPace.AppImage",
            out _));
    }
}
