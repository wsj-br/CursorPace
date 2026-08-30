using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class JsonPlanStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _settingsPath;
    private readonly JsonPlanStore _store;

    public JsonPlanStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "cup-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _settingsPath = Path.Combine(_directory, "settings.json");
        _store = new JsonPlanStore(_directory);
    }

    [Fact]
    public void Load_CorruptFile_WritesBackupAndBlankSettings()
    {
        File.WriteAllText(_settingsPath, "{ not json");

        var loaded = _store.Load();

        Assert.Null(loaded.ActiveCycle);
        Assert.True(File.Exists(Path.Combine(_directory, "settings.corrupt.json")));
        Assert.True(File.Exists(_settingsPath));
        Assert.DoesNotContain("not json", File.ReadAllText(_settingsPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_LockedFile_DoesNotOverwriteExistingSettings()
    {
        _store.Save(new AppSettings { SyncIntervalHours = 6, AutoSyncEnabled = false });
        var original = File.ReadAllText(_settingsPath);

        using (new FileStream(_settingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var loaded = _store.Load();
            Assert.Null(loaded.ActiveCycle);
        }

        Assert.Equal(original, File.ReadAllText(_settingsPath));
        Assert.False(File.Exists(Path.Combine(_directory, "settings.corrupt.json")));
    }

    [Fact]
    public void Load_IgnoresLeftoverRenewalDay()
    {
        File.WriteAllText(_settingsPath, """
            {
              "version": 2,
              "activeCycle": {
                "renewalDay": 15,
                "cycleStart": "2026-08-02T08:00:00",
                "nextRenewal": "2026-09-02T08:00:00"
              }
            }
            """);

        var loaded = _store.Load();

        Assert.NotNull(loaded.ActiveCycle);
        Assert.Equal(2, loaded.ActiveCycle.RenewalDay);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsThemeMode()
    {
        _store.Save(new AppSettings { ThemeMode = UiThemeMode.Dark });

        var loaded = _store.Load();

        Assert.Equal(UiThemeMode.Dark, loaded.ThemeMode);
        Assert.Contains("\"themeMode\": \"Dark\"", File.ReadAllText(_settingsPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MissingThemeMode_DefaultsToSystem()
    {
        File.WriteAllText(_settingsPath, """
            {
              "version": 2,
              "autoSyncEnabled": true
            }
            """);

        var loaded = _store.Load();

        Assert.Equal(UiThemeMode.System, loaded.ThemeMode);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch
        {
        }
    }
}
