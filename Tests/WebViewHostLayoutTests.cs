using CursorPace.Services;

namespace CursorPace.Tests;

public class WebViewHostLayoutTests
{
    [Fact]
    public void OffscreenLayout_KeepsLoginSizeFinite()
    {
        Assert.True(WebViewHostLayout.LoginWidth >= WebViewHostLayout.MinWidth);
        Assert.True(WebViewHostLayout.LoginHeight >= WebViewHostLayout.MinHeight);
        Assert.True(WebViewHostLayout.HasFinitePositiveSize(
            WebViewHostLayout.LoginWidth, WebViewHostLayout.LoginHeight));
    }

    [Fact]
    public void OffscreenPosition_WithoutWorkArea_IsFiniteAndOffOrigin()
    {
        var (x, y) = WebViewHostLayout.OffscreenPosition();

        Assert.True(x < 0);
        Assert.True(y < 0);
    }

    [Fact]
    public void OffscreenPosition_WithWorkArea_SitsAboveTheWorkArea()
    {
        var (x, y) = WebViewHostLayout.OffscreenPosition(100, 40, 1080);

        Assert.Equal(100, x);
        Assert.True(y < 40);
        Assert.True(y <= 40 - 1080);
    }

    [Fact]
    public void HasFinitePositiveSize_RejectsNaNZeroAndNegative()
    {
        Assert.False(WebViewHostLayout.HasFinitePositiveSize(double.NaN, 700));
        Assert.False(WebViewHostLayout.HasFinitePositiveSize(900, double.NaN));
        Assert.False(WebViewHostLayout.HasFinitePositiveSize(0, 700));
        Assert.False(WebViewHostLayout.HasFinitePositiveSize(900, -1));
        Assert.True(WebViewHostLayout.HasFinitePositiveSize(900, 700));
    }

    [Fact]
    public void BrowserSlotSize_WhenBannerHidden_UsesHostSize()
    {
        var (width, height) = WebViewHostLayout.BrowserSlotSize(900, 700, 48, bannerVisible: false);

        Assert.Equal(900, width);
        Assert.Equal(700, height);
    }

    [Fact]
    public void BrowserSlotSize_WhenBannerVisible_SubtractsBannerHeight()
    {
        var (width, height) = WebViewHostLayout.BrowserSlotSize(900, 700, 48, bannerVisible: true);

        Assert.Equal(900, width);
        Assert.Equal(652, height);
    }

    [Fact]
    public void BrowserSlotSize_WhenHostSizeIsNaN_FallsBackToLoginSize()
    {
        var (width, height) = WebViewHostLayout.BrowserSlotSize(
            double.NaN, double.NaN, double.NaN, bannerVisible: true);

        Assert.Equal(WebViewHostLayout.LoginWidth, width);
        Assert.Equal(WebViewHostLayout.LoginHeight, height);
        Assert.True(WebViewHostLayout.HasFinitePositiveSize(width, height));
    }

    [Fact]
    public void BrowserSlotSize_WhenRemainingHeightIsInvalid_KeepsPositiveSlot()
    {
        var (width, height) = WebViewHostLayout.BrowserSlotSize(900, 20, 48, bannerVisible: true);

        Assert.Equal(900, width);
        Assert.Equal(20, height);
        Assert.True(WebViewHostLayout.HasFinitePositiveSize(width, height));
    }

    [Fact]
    public void SilentHost_OnLinux_IsCompactAndTransparent()
    {
        Assert.True(WebViewHostLayout.UsesCompactSilentHost(isLinux: true));
        Assert.False(WebViewHostLayout.DeferBrowserAttach(isLinux: true));
        Assert.Equal(
            (WebViewHostLayout.CompactSilentWidth, WebViewHostLayout.CompactSilentHeight),
            WebViewHostLayout.SilentHostSize(isLinux: true));
        Assert.Equal(0, WebViewHostLayout.SilentHostOpacity(isLinux: true));
        Assert.Equal(
            (WebViewHostLayout.CompactSilentWidth, WebViewHostLayout.CompactSilentHeight),
            WebViewHostLayout.SilentHostMinSize(isLinux: true));
        Assert.True(WebViewHostLayout.CentersCompactSilentHost(isLinux: true));
        Assert.False(WebViewHostLayout.SilentHostUsesDecorations(isLinux: true));
    }

    [Fact]
    public void SilentHost_OnMacOrWindows_KeepsLoginSizeAndOpaque()
    {
        Assert.False(WebViewHostLayout.UsesCompactSilentHost(isLinux: false));
        Assert.True(WebViewHostLayout.DeferBrowserAttach(isLinux: false));
        Assert.Equal(
            (WebViewHostLayout.LoginWidth, WebViewHostLayout.LoginHeight),
            WebViewHostLayout.SilentHostSize(isLinux: false));
        Assert.Equal(1, WebViewHostLayout.SilentHostOpacity(isLinux: false));
        Assert.Equal(
            (WebViewHostLayout.MinWidth, WebViewHostLayout.MinHeight),
            WebViewHostLayout.SilentHostMinSize(isLinux: false));
        Assert.False(WebViewHostLayout.CentersCompactSilentHost(isLinux: false));
        Assert.True(WebViewHostLayout.SilentHostUsesDecorations(isLinux: false));
    }
}
