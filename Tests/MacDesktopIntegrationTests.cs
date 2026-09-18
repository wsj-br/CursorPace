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
}
