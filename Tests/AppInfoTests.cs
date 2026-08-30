using System.Globalization;
using System.Reflection;
using CursorPace.Services;

namespace CursorPace.Tests;

public class AppInfoTests
{
    [Fact]
    public void Read_AppAssembly_UsesProjectVersionCopyrightAndBuildDate()
    {
        var assembly = typeof(AppInfo).Assembly;
        var info = AppInfo.Read(assembly);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Assert.Equal(AppInfo.NormalizeVersion(informational, assembly.GetName().Version), info.Version);
        Assert.Equal(
            assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
            info.Copyright);
        Assert.True(info.BuildDateUtc.HasValue);
        Assert.True(info.BuildDateUtc.Value <= DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.True(info.BuildDateUtc.Value.Year >= 2026);
        Assert.Equal("MIT License", AppInfo.LicenseName);
        Assert.Equal("https://github.com/wsj-br/CursorPace", AppInfo.RepositoryUrl);
        Assert.Equal(new Uri("https://github.com/wsj-br/CursorPace"), AppInfo.RepositoryUri);
    }

    [Theory]
    [InlineData("0.2.2", "0.2.2")]
    [InlineData("0.2.2+abc123", "0.2.2")]
    [InlineData("1.0.0-beta+deadbeef", "1.0.0-beta")]
    public void NormalizeVersion_StripsSourceRevisionSuffix(string informational, string expected)
    {
        Assert.Equal(expected, AppInfo.NormalizeVersion(informational, new Version(9, 9, 9, 9)));
    }

    [Fact]
    public void NormalizeVersion_WhenInformationalMissing_UsesThreePartAssemblyVersion()
    {
        Assert.Equal("1.2.3", AppInfo.NormalizeVersion(null, new Version(1, 2, 3, 0)));
    }

    [Fact]
    public void FormatBuildDate_UsesDayMonthYear()
    {
        var info = new AppInfo("0.2.2", "Copyright © 2026 Waldemar Scudeller Jr.", new DateOnly(2026, 8, 30));

        Assert.Equal("30-Aug-2026", info.FormatBuildDate(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatBuildDate_WhenMissing_UsesEmDash()
    {
        var info = new AppInfo("0.2.2", "Copyright © 2026 Waldemar Scudeller Jr.", null);

        Assert.Equal("—", info.FormatBuildDate(CultureInfo.InvariantCulture));
    }
}
