using CursorPace.Services;

namespace CursorPace.Tests;

public class MacDesktopIntegrationTests
{
    [Fact]
    public void ResolveBundledIconPath_ReturnsPngUnderAssetsWhenPresent()
    {
        var root = Path.Combine(Path.GetTempPath(), "CursorPaceMacIcon-" + Guid.NewGuid().ToString("N"));
        var png = Path.Combine(root, "Assets", MacDesktopIntegration.IconFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        File.WriteAllBytes(png, [1, 2, 3, 4]);

        try
        {
            Assert.Equal(png, MacDesktopIntegration.ResolveBundledIconPath(root));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveBundledIconPath_ReturnsNullWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "CursorPaceMacIcon-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            Assert.Null(MacDesktopIntegration.ResolveBundledIconPath(root));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveBundledIconPath_ReturnsNullForBlankBaseDirectory()
    {
        Assert.Null(MacDesktopIntegration.ResolveBundledIconPath(""));
        Assert.Null(MacDesktopIntegration.ResolveBundledIconPath("   "));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void ShouldShowInDock_HidesWhenWindowIsHiddenOrMinimized(
        bool windowVisible,
        bool windowMinimized,
        bool expected)
    {
        Assert.Equal(expected, MacDesktopIntegration.ShouldShowInDock(windowVisible, windowMinimized));
    }

    [Fact]
    public void ActivationPolicyForWindow_UsesRegularWhenDockShouldShow()
    {
        Assert.Equal(
            MacDesktopIntegration.ActivationPolicyRegular,
            MacDesktopIntegration.ActivationPolicyForWindow(true, false));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ActivationPolicyForWindow_UsesAccessoryWhenDockShouldHide(
        bool windowVisible,
        bool windowMinimized)
    {
        Assert.Equal(
            MacDesktopIntegration.ActivationPolicyAccessory,
            MacDesktopIntegration.ActivationPolicyForWindow(windowVisible, windowMinimized));
    }
}
