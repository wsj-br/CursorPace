using CursorPace.Services;

namespace CursorPace.Tests;

public class MacStartupRegistrationTests
{
    [Fact]
    public void ResolveAppBundlePath_ReturnsBundleWhenProcessIsInnerExecutable()
    {
        var spaced = Path.Combine("Applications", "Cursor Pace.app");
        var current = Path.Combine("Applications", "CursorPace.app");

        Assert.Equal(
            spaced,
            MacStartupRegistration.ResolveAppBundlePath(Path.Combine(spaced, "Contents", "MacOS", "CursorPace")));
        Assert.Equal(
            current,
            MacStartupRegistration.ResolveAppBundlePath(Path.Combine(current, "Contents", "MacOS", "CursorPace")));
    }

    [Fact]
    public void ResolveAppBundlePath_ReturnsNullForUnpackagedExecutable()
    {
        var exe = Path.Combine("Users", "dev", "CursorPace", "bin", "Debug", "net10.0", "CursorPace");
        Assert.Null(MacStartupRegistration.ResolveAppBundlePath(exe));
    }

    [Fact]
    public void ResolveAppBundlePath_ReturnsNullWhenContentsMacOSShapeIsMissing()
    {
        var exe = Path.Combine("Applications", "Cursor Pace.app", "CursorPace");
        Assert.Null(MacStartupRegistration.ResolveAppBundlePath(exe));
    }

    [Fact]
    public void ResolveAppBundlePath_ReturnsNullForBlankProcessPath()
    {
        Assert.Null(MacStartupRegistration.ResolveAppBundlePath(null));
        Assert.Null(MacStartupRegistration.ResolveAppBundlePath(""));
        Assert.Null(MacStartupRegistration.ResolveAppBundlePath("   "));
    }

    [Fact]
    public void BuildLaunchProgramArguments_WhenBundledAndStartInTray_UsesOpenWithoutExecingInnerBinary()
    {
        var bundle = "/Applications/Cursor Pace.app";
        var exe = "/Applications/Cursor Pace.app/Contents/MacOS/CursorPace";

        var args = MacStartupRegistration.BuildLaunchProgramArguments(bundle, exe, startInTray: true);

        Assert.Equal(
            new[]
            {
                MacStartupRegistration.OpenExecutable,
                "-g",
                "-a",
                bundle,
                "--args",
                "--background"
            },
            args);
        Assert.DoesNotContain(exe, args);
    }

    [Fact]
    public void BuildLaunchProgramArguments_WhenBundledAndWindowShouldShow_OpensAppWithoutBackground()
    {
        var bundle = "/Applications/Cursor Pace.app";
        var args = MacStartupRegistration.BuildLaunchProgramArguments(
            bundle,
            bundle + "/Contents/MacOS/CursorPace",
            startInTray: false);

        Assert.Equal(
            new[] { MacStartupRegistration.OpenExecutable, "-a", bundle },
            args);
        Assert.DoesNotContain("--background", args);
        Assert.DoesNotContain("-g", args);
    }

    [Fact]
    public void BuildLaunchProgramArguments_WhenUnpackaged_UsesExecutableAndBackground()
    {
        var exe = "/Users/dev/CursorPace/bin/Debug/net10.0/CursorPace";

        Assert.Equal(
            new[] { exe, "--background" },
            MacStartupRegistration.BuildLaunchProgramArguments(null, exe, startInTray: true));
        Assert.Equal(
            new[] { exe },
            MacStartupRegistration.BuildLaunchProgramArguments("  ", exe, startInTray: false));
    }

    [Fact]
    public void BuildLaunchAgentPlist_LaunchesAquaSessionThroughOpen()
    {
        var plist = MacStartupRegistration.BuildLaunchAgentPlist(
            MacStartupRegistration.Label,
            MacStartupRegistration.BuildLaunchProgramArguments(
                "/Applications/Cursor Pace.app",
                "/Applications/Cursor Pace.app/Contents/MacOS/CursorPace",
                startInTray: true));

        Assert.Contains("<string>com.cursorpace.app</string>", plist);
        Assert.Contains("<key>LimitLoadToSessionType</key>", plist);
        Assert.Contains("<string>Aqua</string>", plist);
        Assert.Contains("<string>/usr/bin/open</string>", plist);
        Assert.Contains("<string>-g</string>", plist);
        Assert.Contains("<string>-a</string>", plist);
        Assert.Contains("<string>/Applications/Cursor Pace.app</string>", plist);
        Assert.Contains("<string>--background</string>", plist);
        Assert.Contains("<key>AssociatedBundleIdentifiers</key>", plist);
        Assert.Contains("<string>com.cursorpace.app</string>", plist);
        Assert.DoesNotContain("Contents/MacOS/CursorPace", plist);
    }

    [Fact]
    public void IsNativeLoginItemRegistered_RecognizesEnabledAndPendingApproval()
    {
        Assert.True(MacStartupRegistration.IsNativeLoginItemRegistered(1));
        Assert.True(MacStartupRegistration.IsNativeLoginItemRegistered(2));
        Assert.False(MacStartupRegistration.IsNativeLoginItemRegistered(0));
        Assert.False(MacStartupRegistration.IsNativeLoginItemRegistered(3));
    }

    [Fact]
    public void BuildLaunchAgentPlist_EscapesSpecialCharactersInPaths()
    {
        var plist = MacStartupRegistration.BuildLaunchAgentPlist(
            "com.cursorpace.app",
            ["/usr/bin/open", "-a", "/tmp/Cursor & Pace.app"]);

        Assert.Contains("<string>/tmp/Cursor &amp; Pace.app</string>", plist);
    }
}
