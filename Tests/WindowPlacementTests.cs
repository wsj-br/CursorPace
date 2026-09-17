using CursorPace.Services;

namespace CursorPace.Tests;

public class WindowPlacementTests
{
    [Fact]
    public void ClampToWorkArea_KeepsPointInside()
    {
        var (x, y, width, height) = WindowPlacement.ClampToWorkArea(100, 50, 760, 749, 0, 0, 1920, 1080);

        Assert.Equal(100, x);
        Assert.Equal(50, y);
        Assert.Equal(760, width);
        Assert.Equal(749, height);
    }

    [Fact]
    public void ClampToWorkArea_PullsOffscreenIntoWorkArea()
    {
        var (x, y, width, height) = WindowPlacement.ClampToWorkArea(4000, 3000, 760, 749, 0, 0, 1920, 1080);

        Assert.Equal(1920 - 760, x);
        Assert.Equal(1080 - 749, y);
        Assert.Equal(760, width);
        Assert.Equal(749, height);
    }

    [Fact]
    public void ClampToWorkArea_WhenWindowLargerThanWorkArea_ShrinksAndPinsToOrigin()
    {
        var (x, y, width, height) = WindowPlacement.ClampToWorkArea(10, 10, 2000, 2000, 100, 40, 800, 600);

        Assert.Equal(100, x);
        Assert.Equal(40, y);
        Assert.Equal(800, width);
        Assert.Equal(600, height);
    }

    [Fact]
    public void ClampToWorkArea_WhenSizeAlreadyFits_LeavesSizeUnchanged()
    {
        var (x, y, width, height) = WindowPlacement.ClampToWorkArea(200, 80, 760, 787, 0, 0, 1920, 1080);

        Assert.Equal(200, x);
        Assert.Equal(80, y);
        Assert.Equal(760, width);
        Assert.Equal(787, height);
    }
}
