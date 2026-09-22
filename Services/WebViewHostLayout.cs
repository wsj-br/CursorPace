namespace CursorPace.Services;

public static class WebViewHostLayout
{
    public const int LoginWidth = 900;
    public const int LoginHeight = 700;
    public const int MinWidth = 400;
    public const int MinHeight = 300;
    public const int CompactSilentWidth = 1;
    public const int CompactSilentHeight = 1;

    public static bool UsesCompactSilentHost(bool isLinux) => isLinux;

    public static bool DeferBrowserAttach(bool isLinux) => !isLinux;

    public static (int Width, int Height) SilentHostSize(bool isLinux) =>
        UsesCompactSilentHost(isLinux)
            ? (CompactSilentWidth, CompactSilentHeight)
            : (LoginWidth, LoginHeight);

    public static (int MinWidth, int MinHeight) SilentHostMinSize(bool isLinux) =>
        UsesCompactSilentHost(isLinux)
            ? (CompactSilentWidth, CompactSilentHeight)
            : (MinWidth, MinHeight);

    public static double SilentHostOpacity(bool isLinux) =>
        UsesCompactSilentHost(isLinux) ? 0 : 1;

    public static bool CentersCompactSilentHost(bool isLinux) => isLinux;

    public static bool SilentHostUsesDecorations(bool isLinux) => !isLinux;

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
