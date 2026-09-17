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
}
