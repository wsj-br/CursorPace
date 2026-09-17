using CursorPace.Services;

namespace CursorPace.Tests;

public class WindowResizeTests
{
    [Fact]
    public void Apply_East_GrowsWidth()
    {
        var (_, _, width, height) = WindowResize.Apply(
            10, 20, 760, 787, 40, 0, 1, 760, 787,
            west: false, east: true, north: false, south: false);

        Assert.Equal(800, width);
        Assert.Equal(787, height);
    }

    [Fact]
    public void Apply_West_MovesOriginAndShrinksWidth()
    {
        var (x, y, width, _) = WindowResize.Apply(
            100, 40, 900, 787, 50, 0, 1, 760, 787,
            west: true, east: false, north: false, south: false);

        Assert.Equal(150, x);
        Assert.Equal(40, y);
        Assert.Equal(850, width);
    }

    [Fact]
    public void Apply_West_WhenBelowMin_KeepsRightEdge()
    {
        var (x, _, width, _) = WindowResize.Apply(
            100, 40, 800, 787, 200, 0, 1, 760, 787,
            west: true, east: false, north: false, south: false);

        Assert.Equal(760, width);
        Assert.Equal(140, x);
    }

    [Fact]
    public void Apply_North_MovesOriginAndShrinksHeight()
    {
        var (x, y, _, height) = WindowResize.Apply(
            10, 100, 760, 900, 0, 40, 1, 760, 787,
            west: false, east: false, north: true, south: false);

        Assert.Equal(10, x);
        Assert.Equal(140, y);
        Assert.Equal(860, height);
    }

    [Fact]
    public void Apply_SouthEast_GrowsBoth()
    {
        var (x, y, width, height) = WindowResize.Apply(
            10, 20, 760, 787, 20, 30, 1, 760, 787,
            west: false, east: true, north: false, south: true);

        Assert.Equal(10, x);
        Assert.Equal(20, y);
        Assert.Equal(780, width);
        Assert.Equal(817, height);
    }

    [Fact]
    public void Apply_ScaledDelta_ConvertsPixelsToDip()
    {
        var (_, _, width, _) = WindowResize.Apply(
            0, 0, 760, 787, 20, 0, 2, 760, 787,
            west: false, east: true, north: false, south: false);

        Assert.Equal(770, width);
    }
}
