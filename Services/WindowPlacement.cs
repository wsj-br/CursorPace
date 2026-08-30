namespace CursorPace.Services;

public static class WindowPlacement
{
    public static (int X, int Y) ClampToWorkArea(
        int x,
        int y,
        int width,
        int height,
        int workX,
        int workY,
        int workWidth,
        int workHeight)
    {
        var maxX = workX + Math.Max(0, workWidth - width);
        var maxY = workY + Math.Max(0, workHeight - height);
        return (Math.Clamp(x, workX, maxX), Math.Clamp(y, workY, maxY));
    }
}
