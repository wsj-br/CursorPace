using CursorPace.Services;

namespace CursorPace.Tests;

public class LinuxFontSetupTests
{
    [Fact]
    public void BuildConfig_RejectsWoffAndIncludesSystemConfig()
    {
        var text = LinuxFontSetup.BuildConfig("/etc/fonts/fonts.conf");

        Assert.Contains("<include ignore_missing=\"yes\">/etc/fonts/fonts.conf</include>", text);
        Assert.Contains("<glob>/usr/share/fonts/woff/**</glob>", text);
        Assert.Contains("<glob>**/*.woff</glob>", text);
        Assert.Contains("<glob>**/*.woff2</glob>", text);
    }

    [Fact]
    public void BuildConfig_EscapesIncludePath()
    {
        var text = LinuxFontSetup.BuildConfig("/tmp/a&b.conf");
        Assert.Contains("<include ignore_missing=\"yes\">/tmp/a&amp;b.conf</include>", text);
    }

    [Fact]
    public void ResolveIncludePath_UsesSystemConfigWhenUnsetOrUnsafe()
    {
        Assert.Equal("/etc/fonts/fonts.conf", LinuxFontSetup.ResolveIncludePath(null));
        Assert.Equal("/etc/fonts/fonts.conf", LinuxFontSetup.ResolveIncludePath(""));
        Assert.Equal("/etc/fonts/fonts.conf", LinuxFontSetup.ResolveIncludePath("</fontconfig>"));
        Assert.Equal("/custom/fonts.conf", LinuxFontSetup.ResolveIncludePath("/custom/fonts.conf"));
    }

    [Fact]
    public void ApplyTo_WritesConfigAndSetsFontconfigFile()
    {
        var previous = Environment.GetEnvironmentVariable("FONTCONFIG_FILE");
        var dir = Path.Combine(Path.GetTempPath(), "cp-font-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            LinuxFontSetup.ApplyTo(dir, null);

            var path = Path.Combine(dir, LinuxFontSetup.ConfigFileName);
            Assert.True(File.Exists(path));
            Assert.Equal(path, Environment.GetEnvironmentVariable("FONTCONFIG_FILE"));
            Assert.Contains("**/*.woff", File.ReadAllText(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("FONTCONFIG_FILE", previous);
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void ApplyTo_WhenAlreadyPointingAtOurFile_DoesNotRewriteInclude()
    {
        var previous = Environment.GetEnvironmentVariable("FONTCONFIG_FILE");
        var dir = Path.Combine(Path.GetTempPath(), "cp-font-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, LinuxFontSetup.ConfigFileName);
        File.WriteAllText(path, "keep");
        try
        {
            LinuxFontSetup.ApplyTo(dir, path);
            Assert.Equal("keep", File.ReadAllText(path));
            Assert.Equal(path, Environment.GetEnvironmentVariable("FONTCONFIG_FILE"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("FONTCONFIG_FILE", previous);
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void ManagerOptions_UsesEmbeddedInter()
    {
        Assert.Equal(AppFonts.InterFamilyName, AppFonts.ManagerOptions.DefaultFamilyName);
        Assert.Equal("fonts:Inter#Inter", AppFonts.InterFamilyName);
    }

    [Fact]
    public void SetNativeEnvironment_UpdatesProcessEnvironment()
    {
        const string name = "CURSORPACE_FONT_TEST";
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            LinuxFontSetup.SetNativeEnvironment(name, "ok");
            Assert.Equal("ok", Environment.GetEnvironmentVariable(name));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }
}
