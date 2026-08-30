using System.Globalization;
using CursorUsageProgress.Models;
using CursorUsageProgress.Services;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Tests;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_WithoutCycle_IsNotInitialized()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.False(vm.IsInitialized);
        Assert.Empty(vm.Days);
    }

    [Fact]
    public void Constructor_WithCycle_IsInitialized()
    {
        var vm = CreateInitializedViewModel(signedIn: true);

        Assert.True(vm.IsInitialized);
        Assert.NotEmpty(vm.Days);
    }

    [Fact]
    public void Constructor_WhenSignedIn_PersistsCursorAccountConnected()
    {
        var store = new FakePlanStore();
        CreateViewModel(new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok }, store);

        Assert.True(store.Settings.CursorAccountConnected);
    }

    [Fact]
    public void Constructor_WhenSignedOut_DoesNotMarkCursorAccountConnected()
    {
        var store = new FakePlanStore { Settings = new AppSettings { CursorAccountConnected = true } };
        CreateViewModel(new FakeSync { IsSignedIn = false, Status = SyncStatus.SignedOut }, store);

        Assert.False(store.Settings.CursorAccountConnected);
    }

    [Fact]
    public void AuthRequired_WhenConnected_ShowsSignInActions()
    {
        var sync = new FakeSync
        {
            IsSignedIn = true,
            Status = SyncStatus.AuthRequired,
            StatusText = "Sign in to Cursor to sync usage."
        };
        var store = new FakePlanStore();
        store.Settings.ActiveCycle = new CycleCalculator().GenerateCycleFromBounds(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 1));
        var vm = CreateViewModel(sync, store);

        Assert.True(vm.IsCursorConnected);
        Assert.True(vm.ShowSyncAlertSignInActions);
        Assert.True(vm.ShowSignInButton);
        Assert.True(((AsyncRelayCommand)vm.SignInCommand).CanExecute(null));
    }

    [Fact]
    public void SignIn_PersistsCursorAccountConnected()
    {
        var store = new FakePlanStore();
        var sync = new FakeSync { IsSignedIn = false };
        CreateViewModel(sync, store);

        sync.SetSignedIn(true);

        Assert.True(store.Settings.CursorAccountConnected);
    }

    [Fact]
    public void SignOut_ClearsPersistedCursorAccountConnected()
    {
        var store = new FakePlanStore { Settings = new AppSettings { CursorAccountConnected = true } };
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        CreateViewModel(sync, store);
        Assert.True(store.Settings.CursorAccountConnected);

        sync.SetSignedIn(false);

        Assert.False(store.Settings.CursorAccountConnected);
    }

    [Fact]
    public void TryBuildUsageSamplesCsv_WhenSamplesExist_ReturnsCsv()
    {
        var timestamp = DateTimeOffset.Parse("2026-08-03T12:00:00Z");
        var samples = new List<UsageSample>
        {
            new()
            {
                TimestampUtc = timestamp,
                CursorModelsPercent = 10m,
                OtherModelsPercent = 20m
            }
        };
        var vm = CreateViewModel(new FakeSync { Samples = samples });

        Assert.True(vm.TryBuildUsageSamplesCsv(out var csv));
        Assert.Equal(UsageSamplesCsvBuilder.Build(samples), csv);
    }

    [Fact]
    public void Constructor_WhenLastSuccessUtcSet_PersistsLastUsageSyncUtc()
    {
        var last = DateTimeOffset.Parse("2026-08-18T10:40:00Z");
        var store = new FakePlanStore();
        CreateViewModel(new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok, LastSuccessUtc = last }, store);

        Assert.Equal(last, store.Settings.LastUsageSyncUtc);
    }

    [Fact]
    public void SyncSuccess_PersistsLastUsageSyncUtc()
    {
        var store = new FakePlanStore { Settings = new AppSettings { CursorAccountConnected = true } };
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        CreateViewModel(sync, store);

        var last = DateTimeOffset.Parse("2026-08-18T11:00:00Z");
        sync.SetLastSuccessUtc(last);

        Assert.Equal(last, store.Settings.LastUsageSyncUtc);
    }

    [Fact]
    public void SaveWindowPosition_PersistsCoordinates()
    {
        var store = new FakePlanStore();
        var vm = CreateViewModel(new FakeSync(), store);

        vm.SaveWindowPosition(120, 80);

        Assert.Equal(120, store.Settings.WindowX);
        Assert.Equal(80, store.Settings.WindowY);
    }

    [Fact]
    public void SaveWindowPosition_WhenUnchanged_DoesNotSaveAgain()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings { WindowX = 10, WindowY = 20 }
        };
        var vm = CreateViewModel(new FakeSync(), store);
        var savesAfterLoad = store.SaveCount;

        vm.SaveWindowPosition(10, 20);

        Assert.Equal(savesAfterLoad, store.SaveCount);
    }

    [Fact]
    public void TryGetSavedWindowPosition_WhenUnset_ReturnsFalse()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.False(vm.TryGetSavedWindowPosition(out var x, out var y));
        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void TryGetSavedWindowPosition_WhenLoaded_ReturnsStoredCoordinates()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings { WindowX = 40, WindowY = 60 }
        };
        var vm = CreateViewModel(new FakeSync(), store);

        Assert.True(vm.TryGetSavedWindowPosition(out var x, out var y));
        Assert.Equal(40, x);
        Assert.Equal(60, y);
    }

    [Fact]
    public void Constructor_WhenRunAtStartup_SyncsRegistrationFromSettings()
    {
        var startup = new FakeStartup();
        var store = new FakePlanStore
        {
            Settings = new AppSettings { RunAtStartup = true, StartInNotificationTray = false }
        };

        CreateViewModel(new FakeSync(), store, startup);

        Assert.True(startup.IsRegistered);
        Assert.False(startup.LastStartInTray);
    }

    [Fact]
    public void Constructor_WhenStartInNotificationTrayUnset_DefaultsToTrue()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.True(vm.StartInNotificationTray);
    }

    [Fact]
    public void RunAtStartup_RegistersWithStartInNotificationTray()
    {
        var startup = new FakeStartup();
        var vm = CreateViewModel(new FakeSync(), new FakePlanStore(), startup);

        vm.RunAtStartup = true;

        Assert.True(startup.IsRegistered);
        Assert.True(startup.LastStartInTray);
    }

    [Fact]
    public void StartInNotificationTray_WhenRunAtStartup_ReRegistersCommand()
    {
        var startup = new FakeStartup();
        var store = new FakePlanStore
        {
            Settings = new AppSettings { RunAtStartup = true, StartInNotificationTray = true }
        };
        var vm = CreateViewModel(new FakeSync(), store, startup);

        vm.StartInNotificationTray = false;

        Assert.True(startup.IsRegistered);
        Assert.False(startup.LastStartInTray);
        Assert.False(store.Settings.StartInNotificationTray);
    }

    [Fact]
    public void StartInNotificationTray_WhenNotRunAtStartup_DoesNotRegister()
    {
        var startup = new FakeStartup();
        var vm = CreateViewModel(new FakeSync(), new FakePlanStore(), startup);

        vm.StartInNotificationTray = false;

        Assert.False(startup.IsRegistered);
        Assert.Null(startup.LastStartInTray);
    }

    [Fact]
    public void ThemeMode_PersistsToStore()
    {
        var store = new FakePlanStore();
        var vm = CreateViewModel(new FakeSync(), store);

        Assert.Equal(UiThemeMode.System, vm.ThemeMode);

        vm.ThemeMode = UiThemeMode.Light;

        Assert.Equal(UiThemeMode.Light, store.Settings.ThemeMode);
    }

    [Fact]
    public void TryBuildUsageSamplesCsv_WhenEmpty_ReturnsFalse()
    {
        var vm = CreateViewModel(signedIn: true);

        Assert.False(vm.TryBuildUsageSamplesCsv(out var csv));
        Assert.Equal(string.Empty, csv);
    }

    [Fact]
    public void RefreshCycle_HidesEstimatedPercentOnAndBeforeLastUpdateDay()
    {
        var calculator = new CycleCalculator();
        var cycle = calculator.GenerateCycleFromBounds(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var lastLocal = new DateTime(2026, 8, 18, 20, 0, 0);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart, 0m, 0m),
            SampleAt(lastLocal, 75m, 70m)
        };
        var store = new FakePlanStore
        {
            Settings = new AppSettings { ActiveCycle = cycle }
        };
        var vm = CreateViewModel(
            new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok, Samples = samples },
            store);

        var earlier = vm.Days.Single(d => d.Date == new DateTime(2026, 8, 17));
        var lastUpdate = vm.Days.Single(d => d.Date == lastLocal.Date);
        var next = vm.Days.Single(d => d.Date == new DateTime(2026, 8, 19));

        Assert.False(earlier.HasCursorProjection);
        Assert.False(lastUpdate.HasCursorProjection);
        Assert.False(lastUpdate.HasOtherProjection);
        Assert.True(next.HasCursorProjection);
        Assert.True(next.HasOtherProjection);
    }

    [Fact]
    public void InfoCards_IncludeTimeOfDay()
    {
        var calculator = new CycleCalculator();
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = calculator.GenerateCycleFromBounds(start, end);
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddDays(30), 97m, 97m)
        };
        var store = new FakePlanStore
        {
            Settings = new AppSettings { ActiveCycle = cycle }
        };
        var vm = CreateViewModel(
            new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok, Samples = samples },
            store);

        Assert.Equal(start.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture), vm.CycleStartText);
        Assert.Equal(end.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture), vm.NextRenewalText);

        var runOut = calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, samples);
        Assert.NotNull(runOut);
        Assert.Equal(runOut.Value.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture), vm.CursorModelsRunOutText);
    }

    [Fact]
    public void TimedCycle_RenewalDateHasPercentsAndMonthHeading()
    {
        var calculator = new CycleCalculator();
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = calculator.GenerateCycleFromBounds(start, end);
        var store = new FakePlanStore
        {
            Settings = new AppSettings { ActiveCycle = cycle }
        };
        var vm = CreateViewModel(
            new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok },
            store);

        Assert.Equal(32, vm.Days.Count);
        var last = Assert.Single(vm.Days, d => d.Date == new DateTime(2026, 9, 2));
        Assert.True(last.ShownExpectedCursor < 100);

        var cell = vm.Calendar.GetCellForDate(new DateTime(2026, 9, 2));
        Assert.NotNull(cell);
        Assert.True(cell.HasData);
        Assert.True(cell.IsRenewalDay);
        Assert.False(string.IsNullOrEmpty(cell.ExpectedCursorText));
        Assert.Equal(
            CalendarMonthViewModel.FormatMonthHeading(start.Date),
            vm.Calendar.MonthHeading);
    }

    [Fact]
    public void ShowSettings_SwitchesMainWindowToSettingsView()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.True(vm.IsMainView);
        Assert.False(vm.IsSettingsView);

        vm.ShowSettingsCommand.Execute(null);

        Assert.True(vm.IsSettingsView);
        Assert.False(vm.IsMainView);

        vm.HideSettingsCommand.Execute(null);

        Assert.True(vm.IsMainView);
        Assert.False(vm.IsSettingsView);
    }

    [Fact]
    public void SuggestedExportFileNames_UseClockTimestamp()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.Equal("cursor-usage-progress-2026-08-18-12_00_00", vm.SuggestedCycleFileName);
        Assert.Equal("usage-samples-2026-08-18-12_00_00", vm.SuggestedUsageSamplesFileName);
        Assert.Equal("cursor-usage-progress-backup-2026-08-18-12_00_00", vm.SuggestedBackupFileName);
    }

    [Fact]
    public void RestoreBackup_ReloadsCycleAndSamples()
    {
        var calculator = new CycleCalculator();
        var cycle = calculator.GenerateCycleFromBounds(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var last = DateTimeOffset.Parse("2026-08-18T10:40:00Z");
        var samples = new List<UsageSample>
        {
            new()
            {
                TimestampUtc = last,
                CursorModelsPercent = 40m,
                OtherModelsPercent = 30m
            }
        };

        var sourceStore = new FakePlanStore { Settings = new AppSettings { ActiveCycle = cycle } };
        var sourceSamples = new FakeSampleStore
        {
            Document = new UsageSampleDocument { CycleStartUtc = last, Samples = samples }
        };
        var archive = new MemoryStream();
        new DataBackupService(sourceStore, sourceSamples)
            .WriteBackup(archive, last);

        var destSync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        var destStore = new FakePlanStore();
        var vm = CreateViewModel(destSync, destStore);
        Assert.False(vm.IsInitialized);

        archive.Position = 0;
        Assert.True(vm.TryRestoreBackup(archive, out var error));
        Assert.Null(error);
        Assert.True(vm.IsInitialized);
        Assert.Equal(cycle.CycleStart, destStore.Settings.ActiveCycle!.CycleStart);
        Assert.Equal(cycle.NextRenewal, destStore.Settings.ActiveCycle.NextRenewal);
        Assert.Equal(40m, destSync.Samples[0].CursorModelsPercent);
        Assert.Equal(last, destSync.LastSuccessUtc);
    }

    [Fact]
    public void TryWriteBackup_WritesRestorableArchive()
    {
        var calculator = new CycleCalculator();
        var cycle = calculator.GenerateCycleFromBounds(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var store = new FakePlanStore { Settings = new AppSettings { ActiveCycle = cycle, SyncIntervalHours = 4 } };
        var vm = CreateInitializedViewModel(signedIn: true, store);

        using var archive = new MemoryStream();
        Assert.True(vm.TryWriteBackup(archive, out var error));
        Assert.Null(error);

        archive.Position = 0;
        var read = DataBackupArchive.Read(archive);
        Assert.True(read.Success);
        Assert.True(JsonPlanStore.TryDeserialize(read.SettingsJson, out var settings));
        Assert.Equal(4, settings.SyncIntervalHours);
        Assert.Equal(cycle.CycleStart, settings.ActiveCycle!.CycleStart);
    }

    private static MainViewModel CreateViewModel(bool signedIn) =>
        CreateViewModel(new FakeSync { IsSignedIn = signedIn, Status = signedIn ? SyncStatus.Ok : SyncStatus.SignedOut });

    private static MainViewModel CreateViewModel(FakeSync sync) =>
        CreateViewModel(sync, new FakePlanStore());

    private static MainViewModel CreateViewModel(FakeSync sync, FakePlanStore store) =>
        CreateViewModel(sync, store, new FakeStartup());

    private static MainViewModel CreateViewModel(FakeSync sync, FakePlanStore store, FakeStartup startup) =>
        new(
            new FakeClock(),
            new CycleCalculator(),
            store,
            startup,
            sync,
            new DataBackupService(store, sync.SampleStore));

    private static MainViewModel CreateInitializedViewModel(bool signedIn, FakePlanStore? store = null)
    {
        var calculator = new CycleCalculator();
        var cycle = calculator.GenerateCycleFromBounds(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        store ??= new FakePlanStore();
        store.Settings.ActiveCycle ??= cycle;
        return CreateViewModel(new FakeSync
        {
            IsSignedIn = signedIn,
            Status = signedIn ? SyncStatus.Ok : SyncStatus.SignedOut
        }, store);
    }

    private static UsageSample SampleAt(DateTime local, decimal cursor, decimal other)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new UsageSample
        {
            TimestampUtc = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset),
            CursorModelsPercent = cursor,
            OtherModelsPercent = other
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTime Now { get; set; } = new(2026, 8, 18, 12, 0, 0);
        public DateTime Today => Now.Date;
    }

    private sealed class FakePlanStore : IPlanStore
    {
        public AppSettings Settings { get; set; } = new();
        public int SaveCount { get; private set; }
        public AppSettings Load() => Settings;
        public void Save(AppSettings settings)
        {
            Settings = settings;
            SaveCount++;
        }
    }

    private sealed class FakeStartup : IStartupRegistration
    {
        public bool IsRegistered { get; private set; }
        public bool? LastStartInTray { get; private set; }
        public void Register(bool startInTray)
        {
            IsRegistered = true;
            LastStartInTray = startInTray;
        }
        public void Unregister()
        {
            IsRegistered = false;
            LastStartInTray = null;
        }
    }

    private sealed class FakeSampleStore : IUsageSampleStore
    {
        public UsageSampleDocument Document { get; set; } = new();
        public UsageSampleDocument Load() => Document;
        public void Save(UsageSampleDocument document) => Document = document;
    }

    private sealed class FakeSync : IUsageSyncService
    {
        public FakeSampleStore SampleStore { get; } = new();
        public SyncStatus Status { get; set; } = SyncStatus.SignedOut;
        public bool IsSignedIn { get; set; }
        public string StatusText { get; set; } = "Not signed in";
        public DateTimeOffset? LastSuccessUtc { get; set; }
        public IReadOnlyList<UsageSample> Samples
        {
            get => SampleStore.Document.Samples;
            set => SampleStore.Document = new UsageSampleDocument
            {
                Version = SampleStore.Document.Version,
                CycleStartUtc = SampleStore.Document.CycleStartUtc,
                Samples = value.ToList()
            };
        }
        public event EventHandler? StateChanged;
#pragma warning disable CS0067
        public event EventHandler<UsageSnapshot>? SnapshotReceived;
#pragma warning restore CS0067
        public Task StartAsync(bool autoSyncEnabled, int intervalHours) => Task.CompletedTask;
        public Task RefreshNowAsync(bool allowInteractiveLogin) => Task.CompletedTask;
        public Task SignInAsync() => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public void SetIntervalHours(int hours) { }
        public void SetAutoSyncEnabled(bool enabled) { }
        public void ReloadPersistedUsage(DateTimeOffset? lastSuccessUtc)
        {
            LastSuccessUtc = lastSuccessUtc
                ?? (Samples.Count == 0 ? null : Samples[^1].TimestampUtc);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Dispose() { }

        public void SetSignedIn(bool signedIn)
        {
            IsSignedIn = signedIn;
            Status = signedIn ? SyncStatus.Ok : SyncStatus.SignedOut;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetLastSuccessUtc(DateTimeOffset last)
        {
            LastSuccessUtc = last;
            Status = SyncStatus.Ok;
            IsSignedIn = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
