using CursorPace.Services;

namespace CursorPace.Tests;

public class LaunchModeTests
{
    [Fact]
    public void HideMainWindow_WhenStartInNotificationTray_ReturnsTrue()
    {
        Assert.True(LaunchMode.HideMainWindow(true, ["CursorPace"]));
    }

    [Fact]
    public void HideMainWindow_WhenStartInNotificationTrayOff_ReturnsFalse()
    {
        Assert.False(LaunchMode.HideMainWindow(false, ["CursorPace"]));
    }

    [Fact]
    public void HideMainWindow_WhenBackgroundArgument_ReturnsTrue()
    {
        Assert.True(LaunchMode.HideMainWindow(false, ["CursorPace", LaunchMode.BackgroundArgument]));
    }

    [Fact]
    public void HideMainWindow_WhenBackgroundArgumentAndTrayOff_StillHides()
    {
        Assert.True(LaunchMode.HideMainWindow(false, [LaunchMode.BackgroundArgument]));
    }

    [Fact]
    public void HideMainWindow_IgnoresUnrelatedArguments()
    {
        Assert.False(LaunchMode.HideMainWindow(false, ["CursorPace", "--test", "-d"]));
    }

    [Fact]
    public void HideMainWindow_WhenShowArgument_ReturnsFalse()
    {
        Assert.False(LaunchMode.HideMainWindow(false, ["CursorPace", LaunchMode.ShowArgument]));
    }

    [Fact]
    public void HideMainWindow_WhenShowArgument_OverridesStartInNotificationTray()
    {
        Assert.False(LaunchMode.HideMainWindow(true, ["CursorPace", LaunchMode.ShowArgument]));
    }

    [Fact]
    public void HideMainWindow_WhenShowArgument_OverridesBackgroundArgument()
    {
        Assert.False(LaunchMode.HideMainWindow(
            true,
            ["CursorPace", LaunchMode.BackgroundArgument, LaunchMode.ShowArgument]));
    }
}
