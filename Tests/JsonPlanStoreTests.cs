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
    public void SaveAndLoad_RoundTripsRemoteSyncSettings()
    {
        _store.Save(new AppSettings
        {
            RemoteSyncEnabled = true,
            RemoteSyncUrl = "http://server:8000",
            RemoteSyncApiKey = "secret-key",
            RemoteSyncMachineName = "dev-box",
            LastRemoteSyncUtc = new DateTimeOffset(2026, 9, 18, 10, 0, 0, TimeSpan.Zero)
        });

        var loaded = _store.Load();

        Assert.True(loaded.RemoteSyncEnabled);
        Assert.Equal("http://server:8000", loaded.RemoteSyncUrl);
        Assert.Equal("secret-key", loaded.RemoteSyncApiKey);
        Assert.Equal("dev-box", loaded.RemoteSyncMachineName);
        Assert.Equal(new DateTimeOffset(2026, 9, 18, 10, 0, 0, TimeSpan.Zero), loaded.LastRemoteSyncUtc);
    }

    [Fact]
    public void Load_IgnoresRemovedShowChartView()
    {
        File.WriteAllText(_settingsPath, """
            {
              "version": 2,
              "showChartView": true,
              "syncIntervalHours": 4
            }
            """);

        var loaded = _store.Load();
        _store.Save(loaded);
        var json = File.ReadAllText(_settingsPath);

        Assert.Equal(4, loaded.SyncIntervalHours);
        Assert.DoesNotContain("showChartView", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_MissingRemoteSyncFields_DefaultsToDisabled()
    {
        File.WriteAllText(_settingsPath, """{ "version": 2 }""");

        var loaded = _store.Load();

        Assert.False(loaded.RemoteSyncEnabled);
        Assert.Null(loaded.RemoteSyncUrl);
        Assert.Null(loaded.RemoteSyncApiKey);
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

    [Fact]
    public void SaveAndLoad_RoundTripsRawSampleMaxDays()
    {
        _store.Save(new AppSettings { RawSampleMaxDays = 7 });

        var loaded = _store.Load();

        Assert.Equal(7, loaded.RawSampleMaxDays);
        Assert.Contains("\"rawSampleMaxDays\": 7", File.ReadAllText(_settingsPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MissingOrUnknownRawSampleMaxDays_DefaultsToFour()
    {
        File.WriteAllText(_settingsPath, """{ "version": 2 }""");
        Assert.Equal(4, _store.Load().RawSampleMaxDays);

        File.WriteAllText(_settingsPath, """{ "version": 2, "rawSampleMaxDays": 3 }""");
        Assert.Equal(4, _store.Load().RawSampleMaxDays);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsWindowPlacement()
    {
        _store.Save(new AppSettings
        {
            WindowX = 40,
            WindowY = 60,
            WindowWidth = 1024,
            WindowHeight = 900,
            WindowMaximized = true
        });

        var loaded = _store.Load();

        Assert.Equal(40, loaded.WindowX);
        Assert.Equal(60, loaded.WindowY);
        Assert.Equal(1024, loaded.WindowWidth);
        Assert.Equal(900, loaded.WindowHeight);
        Assert.True(loaded.WindowMaximized);
        var json = File.ReadAllText(_settingsPath);
        Assert.Contains("\"windowWidth\": 1024", json, StringComparison.Ordinal);
        Assert.Contains("\"windowMaximized\": true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MissingWindowSize_LeavesSizeUnset()
    {
        File.WriteAllText(_settingsPath, """
            {
              "version": 2,
              "windowX": 10,
              "windowY": 20
            }
            """);

        var loaded = _store.Load();

        Assert.Equal(10, loaded.WindowX);
        Assert.Equal(20, loaded.WindowY);
        Assert.Null(loaded.WindowWidth);
        Assert.Null(loaded.WindowHeight);
        Assert.False(loaded.WindowMaximized);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsCycleHistory()
    {
        var previous = new QuotaCycle
        {
            RenewalDay = 2,
            CycleStart = new DateTime(2026, 7, 2, 8, 0, 0),
            NextRenewal = new DateTime(2026, 8, 2, 8, 0, 0)
        };
        var active = new QuotaCycle
        {
            RenewalDay = 2,
            CycleStart = new DateTime(2026, 8, 2, 8, 0, 0),
            NextRenewal = new DateTime(2026, 9, 2, 8, 0, 0)
        };

        _store.Save(new AppSettings
        {
            ActiveCycle = active,
            CycleHistory = [previous]
        });

        var loaded = _store.Load();
        var json = File.ReadAllText(_settingsPath);

        Assert.Equal(active.CycleStart, loaded.ActiveCycle!.CycleStart);
        Assert.Equal(active.NextRenewal, loaded.ActiveCycle.NextRenewal);
        var archived = Assert.Single(loaded.CycleHistory);
        Assert.Equal(previous.CycleStart, archived.CycleStart);
        Assert.Equal(previous.NextRenewal, archived.NextRenewal);
        Assert.Equal(2, archived.RenewalDay);
        Assert.Contains("\"cycleHistory\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MissingCycleHistory_DefaultsToEmpty()
    {
        File.WriteAllText(_settingsPath, """
            {
              "version": 2,
              "activeCycle": {
                "renewalDay": 2,
                "cycleStart": "2026-08-02T08:00:00",
                "nextRenewal": "2026-09-02T08:00:00"
              }
            }
            """);

        var loaded = _store.Load();

        Assert.NotNull(loaded.ActiveCycle);
        Assert.Empty(loaded.CycleHistory);
    }

    [Fact]
    public void Load_SkipsInvalidCycleHistoryEntries()
    {
        File.WriteAllText(_settingsPath, """
            {
              "version": 2,
              "cycleHistory": [
                {
                  "renewalDay": 15,
                  "cycleStart": "2026-07-02T08:00:00",
                  "nextRenewal": "2026-08-02T08:00:00"
                },
                {
                  "renewalDay": 1,
                  "cycleStart": "2026-06-01T08:00:00",
                  "nextRenewal": "2026-06-01T08:00:00"
                }
              ]
            }
            """);

        var loaded = _store.Load();

        var archived = Assert.Single(loaded.CycleHistory);
        Assert.Equal(new DateTime(2026, 7, 2, 8, 0, 0), archived.CycleStart);
        Assert.Equal(2, archived.RenewalDay);
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
