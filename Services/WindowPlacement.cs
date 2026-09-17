namespace CursorPace.Services;

public static class WindowPlacement
{
    public static (int X, int Y, int Width, int Height) ClampToWorkArea(
        int x,
        int y,
        int width,
        int height,
        int workX,
        int workY,
        int workWidth,
        int workHeight)
    {
        var clampedWidth = workWidth > 0 ? Math.Min(width, workWidth) : Math.Max(0, width);
        var clampedHeight = workHeight > 0 ? Math.Min(height, workHeight) : Math.Max(0, height);
        if (clampedWidth < 0)
            clampedWidth = 0;
        if (clampedHeight < 0)
            clampedHeight = 0;

        var maxX = workX + Math.Max(0, workWidth - clampedWidth);
        var maxY = workY + Math.Max(0, workHeight - clampedHeight);
        return (
            Math.Clamp(x, workX, maxX),
            Math.Clamp(y, workY, maxY),
            clampedWidth,
            clampedHeight);
    }
}
