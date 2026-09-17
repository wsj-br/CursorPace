using CursorPace.Services;

namespace CursorPace.Tests;

public class LinuxWebKitCookiePersistenceTests
{
    [Fact]
    public void DatabasePath_IsSqliteFileUnderProfileDirectory()
    {
        var path = LinuxWebKitCookiePersistence.DatabasePath;

        Assert.Equal(
            Path.Combine(WebViewProfilePaths.ProfileDirectory, "cookies.sqlite"),
            path);
        Assert.Equal("cookies.sqlite", Path.GetFileName(path));
    }
}
