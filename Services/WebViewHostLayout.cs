namespace CursorPace.Services;

public static class WebViewHostLayout
{
    public const int LoginWidth = 900;
    public const int LoginHeight = 700;
    public const int MinWidth = 400;
    public const int MinHeight = 300;

    public static (int X, int Y) OffscreenPosition() => (-32000, -32000);

    public static (int X, int Y) OffscreenPosition(int workX, int workY, int workHeight)
    {
        if (workHeight <= 0)
            return OffscreenPosition();

        return (workX, workY - Math.Max(2000, workHeight + 100));
    }

    public static bool HasFinitePositiveSize(double width, double height) =>
        IsFinitePositive(width) && IsFinitePositive(height);

    public static bool IsFinitePositive(double value) =>
        double.IsFinite(value) && value > 0;

    public static (double Width, double Height) BrowserSlotSize(
        double hostWidth,
        double hostHeight,
        double bannerHeight,
        bool bannerVisible)
    {
        var width = IsFinitePositive(hostWidth) ? hostWidth : LoginWidth;
        var height = IsFinitePositive(hostHeight) ? hostHeight : LoginHeight;
        var banner = bannerVisible && IsFinitePositive(bannerHeight) ? bannerHeight : 0;
        var slotHeight = height - banner;
        if (!IsFinitePositive(slotHeight))
            slotHeight = height;

        return (Math.Max(1, width), Math.Max(1, slotHeight));
    }
}
