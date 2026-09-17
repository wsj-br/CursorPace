using CursorPace.Services;

namespace CursorPace.Tests;

public class LinuxDesktopIntegrationTests
{
    [Fact]
    public void BuildDesktopEntry_UsesAbsoluteIconAndStartupWmClass()
    {
        var text = LinuxDesktopIntegration.BuildDesktopEntry(
            "/home/user/Apps/CursorPace.AppImage",
            "/home/user/.local/share/icons/hicolor/256x256/apps/cursor-pace.png");

        Assert.Contains("StartupWMClass=CursorPace", text);
        Assert.Contains("Icon=/home/user/.local/share/icons/hicolor/256x256/apps/cursor-pace.png", text);
        Assert.Contains("Exec=\"/home/user/Apps/CursorPace.AppImage\"", text);
        Assert.Contains("TryExec=/home/user/Apps/CursorPace.AppImage", text);
        Assert.DoesNotContain("NoDisplay", text);
    }

    [Fact]
    public void Install_WritesDesktopFileMatchingWmClassAndCopiesIcon()
    {
        var root = Path.Combine(Path.GetTempPath(), "CursorPaceDesktop-" + Guid.NewGuid().ToString("N"));
        var applications = Path.Combine(root, "applications");
        var iconDestination = Path.Combine(root, "icons", "cursor-pace.png");
        var iconSource = Path.Combine(root, "source.png");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(applications);
        File.WriteAllText(Path.Combine(applications, LinuxDesktopIntegration.LegacyDesktopFileName), "legacy");
        File.WriteAllBytes(iconSource, [1, 2, 3, 4]);

        try
        {
            LinuxDesktopIntegration.Install(
                applications,
                iconDestination,
                "/opt/CursorPace/CursorPace",
                iconSource);

            var desktopPath = Path.Combine(applications, LinuxDesktopIntegration.DesktopFileName);
            var text = File.ReadAllText(desktopPath);
            Assert.Equal("CursorPace.desktop", LinuxDesktopIntegration.DesktopFileName);
            Assert.Contains("StartupWMClass=CursorPace", text);
            Assert.Contains($"Icon={iconDestination}", text);
            Assert.Contains("Exec=\"/opt/CursorPace/CursorPace\"", text);
            Assert.True(File.Exists(iconDestination));
            Assert.Equal(File.ReadAllBytes(iconSource), File.ReadAllBytes(iconDestination));
            Assert.False(File.Exists(Path.Combine(applications, LinuxDesktopIntegration.LegacyDesktopFileName)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Install_WhenIconSourceMissing_FallsBackToThemedIconName()
    {
        var root = Path.Combine(Path.GetTempPath(), "CursorPaceDesktop-" + Guid.NewGuid().ToString("N"));
        var applications = Path.Combine(root, "applications");
        var iconDestination = Path.Combine(root, "icons", "cursor-pace.png");

        try
        {
            LinuxDesktopIntegration.Install(
                applications,
                iconDestination,
                "/usr/bin/CursorPace",
                iconSourcePath: Path.Combine(root, "missing.png"));

            var text = File.ReadAllText(Path.Combine(applications, LinuxDesktopIntegration.DesktopFileName));
            Assert.Contains("Icon=cursor-pace", text);
            Assert.False(File.Exists(iconDestination));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
