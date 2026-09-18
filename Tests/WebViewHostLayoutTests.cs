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
}
