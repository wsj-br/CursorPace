namespace CursorPace.Services;

public static class WindowResize
{
    public static (int X, int Y, double Width, double Height) Apply(
        int originX,
        int originY,
        double originWidth,
        double originHeight,
        int deltaXPixels,
        int deltaYPixels,
        double scaling,
        double minWidth,
        double minHeight,
        bool west,
        bool east,
        bool north,
        bool south)
    {
        if (scaling <= 0)
            scaling = 1;

        var width = originWidth;
        var height = originHeight;
        var x = originX;
        var y = originY;

        if (east)
            width = Math.Max(minWidth, originWidth + deltaXPixels / scaling);

        if (west)
        {
            width = Math.Max(minWidth, originWidth - deltaXPixels / scaling);
            x = originX + (int)Math.Round((originWidth - width) * scaling);
        }

        if (south)
            height = Math.Max(minHeight, originHeight + deltaYPixels / scaling);

        if (north)
        {
            height = Math.Max(minHeight, originHeight - deltaYPixels / scaling);
            y = originY + (int)Math.Round((originHeight - height) * scaling);
        }

        return (x, y, width, height);
    }
}
