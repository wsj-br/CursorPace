using System.Globalization;
using CursorPace.Models;
using CursorPace.Services;
using CursorPace.ViewModels;

namespace CursorPace.Tests;

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
    public void SaveWindowPlacement_PersistsCoordinatesSizeAndMaximized()
    {
        var store = new FakePlanStore();
        var vm = CreateViewModel(new FakeSync(), store);

        vm.SaveWindowPlacement(120, 80, 900, 820, true);

        Assert.Equal(120, store.Settings.WindowX);
        Assert.Equal(80, store.Settings.WindowY);
        Assert.Equal(900, store.Settings.WindowWidth);
        Assert.Equal(820, store.Settings.WindowHeight);
        Assert.True(store.Settings.WindowMaximized);
    }

    [Fact]
    public void SaveWindowPlacement_WhenUnchanged_DoesNotSaveAgain()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings
            {
                WindowX = 10,
                WindowY = 20,
                WindowWidth = 760,
                WindowHeight = 787,
                WindowMaximized = false
            }
        };
        var vm = CreateViewModel(new FakeSync(), store);
        var savesAfterLoad = store.SaveCount;

        vm.SaveWindowPlacement(10, 20, 760, 787, false);

        Assert.Equal(savesAfterLoad, store.SaveCount);
    }

    [Fact]
    public void TryGetSavedWindowPlacement_WhenUnset_ReturnsFalse()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.False(vm.TryGetSavedWindowPlacement(out var x, out var y, out var width, out var height, out var maximized));
        Assert.Null(x);
        Assert.Null(y);
        Assert.Null(width);
        Assert.Null(height);
        Assert.False(maximized);
    }

    [Fact]
    public void TryGetSavedWindowPlacement_WhenOnlyPositionLoaded_ReturnsCoordinates()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings { WindowX = 40, WindowY = 60 }
        };
        var vm = CreateViewModel(new FakeSync(), store);

        Assert.True(vm.TryGetSavedWindowPlacement(out var x, out var y, out var width, out var height, out var maximized));
        Assert.Equal(40, x);
        Assert.Equal(60, y);
        Assert.Null(width);
        Assert.Null(height);
        Assert.False(maximized);
    }

    [Fact]
    public void TryGetSavedWindowPlacement_WhenLoaded_ReturnsStoredPlacement()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings
            {
                WindowX = 40,
                WindowY = 60,
                WindowWidth = 1024,
                WindowHeight = 900,
                WindowMaximized = true
            }
        };
        var vm = CreateViewModel(new FakeSync(), store);

        Assert.True(vm.TryGetSavedWindowPlacement(out var x, out var y, out var width, out var height, out var maximized));
        Assert.Equal(40, x);
        Assert.Equal(60, y);
        Assert.Equal(1024, width);
        Assert.Equal(900, height);
        Assert.True(maximized);
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
    public void AboutProperties_MatchAppInfo()
    {
        var vm = CreateViewModel(signedIn: false);

        Assert.Equal(AppInfo.Current.Version, vm.AboutVersion);
        Assert.Equal(AppInfo.Current.FormatBuildDate(CultureInfo.CurrentCulture), vm.AboutBuildDate);
        Assert.Equal(AppInfo.Current.Copyright, vm.AboutCopyright);
        Assert.Equal(AppInfo.LicenseName, vm.AboutLicense);
        Assert.Equal(AppInfo.RepositoryUrl, vm.AboutRepositoryUrl);
        Assert.Equal(AppInfo.RepositoryUri, vm.AboutRepositoryUri);
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

        Assert.Equal("cursor-pace-2026-08-18-12_00_00", vm.SuggestedCycleFileName);
        Assert.Equal("usage-samples-2026-08-18-12_00_00", vm.SuggestedUsageSamplesFileName);
        Assert.Equal("cursor-pace-backup-2026-08-18-12_00_00", vm.SuggestedBackupFileName);
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

    [Fact]
    public void Snapshot_NewCycleStart_ArchivesPreviousAndStaysOnLive()
    {
        var calculator = new CycleCalculator();
        var previous = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 8, 1));
        var store = new FakePlanStore { Settings = new AppSettings { ActiveCycle = previous } };
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        var vm = CreateViewModel(sync, store);

        Assert.False(vm.CanGoToPreviousCycle);
        Assert.False(vm.CanGoToNextCycle);

        sync.RaiseSnapshotReceived(SnapshotFor(
            new DateTime(2026, 8, 1, 8, 0, 0),
            new DateTime(2026, 9, 1, 8, 0, 0),
            1m,
            2m));

        Assert.Equal(new DateTime(2026, 8, 1, 8, 0, 0), store.Settings.ActiveCycle!.CycleStart);
        var archived = Assert.Single(store.Settings.CycleHistory);
        Assert.Equal(previous.CycleStart, archived.CycleStart);
        Assert.Equal(
            CalendarMonthViewModel.FormatMonthHeading(new DateTime(2026, 8, 1)),
            vm.Calendar.MonthHeading);
        Assert.True(vm.CanGoToPreviousCycle);
        Assert.False(vm.CanGoToNextCycle);
        Assert.False(vm.GoToNextCycleCommand.CanExecute(null));
        Assert.True(vm.GoToPreviousCycleCommand.CanExecute(null));
    }

    [Fact]
    public void GoToPreviousCycle_ShowsArchivedCycleSamplesAndChart()
    {
        var calculator = new CycleCalculator();
        var previousStart = new DateTime(2026, 7, 1, 8, 0, 0);
        var liveStart = new DateTime(2026, 8, 1, 8, 0, 0);
        var previous = calculator.GenerateCycleFromBounds(previousStart, liveStart);
        var live = calculator.GenerateCycleFromBounds(liveStart, new DateTime(2026, 9, 1, 8, 0, 0));
        var samples = new List<UsageSample>
        {
            SampleAt(previousStart.AddDays(1), 40m, 41m),
            SampleAt(liveStart.AddDays(1), 5m, 6m)
        };
        var store = new FakePlanStore
        {
            Settings = new AppSettings
            {
                ActiveCycle = live,
                CycleHistory = [previous]
            }
        };
        var vm = CreateViewModel(
            new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok, Samples = samples },
            store);

        Assert.Equal(CalendarMonthViewModel.FormatMonthHeading(liveStart.Date), vm.Calendar.MonthHeading);
        Assert.Equal(liveStart, vm.Chart.Document!.CycleStart);

        vm.GoToPreviousCycleCommand.Execute(null);

        Assert.Equal(CalendarMonthViewModel.FormatMonthHeading(previousStart.Date), vm.Calendar.MonthHeading);
        Assert.Equal(previousStart.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture), vm.CycleStartText);
        Assert.DoesNotContain(vm.Days, d => d.Date == liveStart.Date.AddDays(1));
        Assert.Contains(vm.Days, d => d.Date == previousStart.Date.AddDays(1) && d.IsActual);
        Assert.Equal(previousStart, vm.Chart.Document!.CycleStart);
        Assert.False(vm.CanGoToPreviousCycle);
        Assert.True(vm.CanGoToNextCycle);

        vm.GoToNextCycleCommand.Execute(null);

        Assert.Equal(CalendarMonthViewModel.FormatMonthHeading(liveStart.Date), vm.Calendar.MonthHeading);
        Assert.Equal(liveStart, vm.Chart.Document!.CycleStart);
        Assert.True(vm.CanGoToPreviousCycle);
        Assert.False(vm.CanGoToNextCycle);
    }

    [Fact]
    public void Snapshot_WhileViewingHistory_DoesNotChangeDisplayedCycle()
    {
        var calculator = new CycleCalculator();
        var previous = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 7, 1, 8, 0, 0),
            new DateTime(2026, 8, 1, 8, 0, 0));
        var live = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 8, 1, 8, 0, 0),
            new DateTime(2026, 9, 1, 8, 0, 0));
        var store = new FakePlanStore
        {
            Settings = new AppSettings
            {
                ActiveCycle = live,
                CycleHistory = [previous]
            }
        };
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        var vm = CreateViewModel(sync, store);
        vm.GoToPreviousCycleCommand.Execute(null);
        var heading = vm.Calendar.MonthHeading;

        sync.RaiseSnapshotReceived(SnapshotFor(
            new DateTime(2026, 8, 1, 8, 0, 0),
            new DateTime(2026, 9, 1, 8, 0, 0),
            12m,
            14m));

        Assert.Equal(heading, vm.Calendar.MonthHeading);
        Assert.Equal(previous.CycleStart.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture), vm.CycleStartText);
        Assert.Equal(live.CycleStart, store.Settings.ActiveCycle!.CycleStart);
    }

    [Fact]
    public void CheckForNewDay_WhileViewingHistory_UsesLiveRenewal()
    {
        var calculator = new CycleCalculator();
        var previous = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 8, 1));
        var live = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 1));
        var store = new FakePlanStore
        {
            Settings = new AppSettings
            {
                ActiveCycle = live,
                CycleHistory = [previous]
            }
        };
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        var clock = new FakeClock { Now = new DateTime(2026, 8, 18, 12, 0, 0) };
        var vm = CreateViewModel(sync, store, new FakeStartup(), clock);
        vm.GoToPreviousCycleCommand.Execute(null);

        vm.CheckForNewDay();
        Assert.Equal(0, sync.RefreshNowCount);

        clock.Now = new DateTime(2026, 9, 1, 12, 0, 0);
        vm.CheckForNewDay();
        Assert.Equal(1, sync.RefreshNowCount);
        Assert.Equal(CalendarMonthViewModel.FormatMonthHeading(previous.CycleStart.Date), vm.Calendar.MonthHeading);
    }

    [Fact]
    public void RestoreBackup_WithHistory_ShowsLiveCycle()
    {
        var calculator = new CycleCalculator();
        var previous = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 8, 1));
        var live = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 1));
        var destCycle = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 7, 1));

        var sourceStore = new FakePlanStore
        {
            Settings = new AppSettings
            {
                ActiveCycle = live,
                CycleHistory = [previous]
            }
        };
        var archive = new MemoryStream();
        new DataBackupService(sourceStore, new FakeSampleStore())
            .WriteBackup(archive, DateTimeOffset.Parse("2026-08-18T10:40:00Z"));

        var destStore = new FakePlanStore
        {
            Settings = new AppSettings { ActiveCycle = destCycle }
        };
        var vm = CreateViewModel(
            new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok },
            destStore);
        Assert.Equal(CalendarMonthViewModel.FormatMonthHeading(destCycle.CycleStart.Date), vm.Calendar.MonthHeading);

        archive.Position = 0;
        Assert.True(vm.TryRestoreBackup(archive, out var error));
        Assert.Null(error);
        Assert.Equal(CalendarMonthViewModel.FormatMonthHeading(live.CycleStart.Date), vm.Calendar.MonthHeading);
        Assert.Equal(live.CycleStart, destStore.Settings.ActiveCycle!.CycleStart);
        Assert.True(vm.CanGoToPreviousCycle);
        Assert.False(vm.CanGoToNextCycle);
    }

    [Fact]
    public async Task RunRemoteSyncAsync_WhenDisabled_DoesNotCallServer()
    {
        var remoteSync = new FakeRemoteSync();
        var vm = CreateViewModel(new FakeSync(), new FakePlanStore(), remoteSync);

        await vm.RunRemoteSyncAsync();

        Assert.Equal(0, remoteSync.SyncCount);
        Assert.Equal("Sync server is off.", vm.RemoteSyncStatusText);
    }

    [Fact]
    public async Task RunRemoteSyncAsync_WhenKeyMissing_DoesNotCallServer()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings { RemoteSyncEnabled = true, RemoteSyncUrl = "http://server:8000" }
        };
        var remoteSync = new FakeRemoteSync();
        var vm = CreateViewModel(new FakeSync(), store, remoteSync);

        await vm.RunRemoteSyncAsync();

        Assert.Equal(0, remoteSync.SyncCount);
    }

    [Fact]
    public void RemoteSyncMachineName_DefaultsToHostname()
    {
        var vm = CreateViewModel(new FakeSync());

        Assert.Equal(Environment.MachineName, vm.RemoteSyncMachineName);

        vm.RemoteSyncMachineName = string.Empty;

        Assert.Equal(Environment.MachineName, vm.RemoteSyncMachineName);
    }

    [Fact]
    public void RemoteSyncMachineName_KeepsCustomName()
    {
        var vm = CreateViewModel(new FakeSync());

        vm.RemoteSyncMachineName = "dev-box";

        Assert.Equal("dev-box", vm.RemoteSyncMachineName);
    }

    [Fact]
    public async Task RunRemoteSyncAsync_AdoptsRemoteCycleAndSamplesOnFreshMachine()
    {
        var store = ConfiguredRemoteStore();
        var sync = new FakeSync();
        var remoteSync = new FakeRemoteSync
        {
            Result = new RemoteSyncResult(
                true,
                null,
                new RemoteSyncCanonicalState(
                    AtUtc(2026, 8, 1),
                    new QuotaCycle
                    {
                        RenewalDay = 1,
                        CycleStart = new DateTime(2026, 8, 1),
                        NextRenewal = new DateTime(2026, 9, 1)
                    },
                    [],
                    [SampleAt(new DateTime(2026, 8, 10, 10, 0, 0), 10, 2)]),
                0,
                1)
        };
        var vm = CreateViewModel(sync, store, remoteSync);
        Assert.False(vm.IsInitialized);

        await vm.RunRemoteSyncAsync();

        Assert.Equal(1, remoteSync.SyncCount);
        Assert.True(vm.IsInitialized);
        Assert.NotNull(store.Settings.ActiveCycle);
        Assert.Equal(new DateTime(2026, 8, 1), store.Settings.ActiveCycle.CycleStart);
        Assert.Single(sync.SampleStore.Document.Samples);
        Assert.NotNull(store.Settings.LastRemoteSyncUtc);
        Assert.Contains("Last synced", vm.RemoteSyncStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunRemoteSyncAsync_WhenServerFails_ShowsErrorAndKeepsLocalData()
    {
        var store = ConfiguredRemoteStore();
        store.Settings.ActiveCycle = new CycleCalculator().GenerateCycleFromBounds(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 1));
        var remoteSync = new FakeRemoteSync
        {
            Result = new RemoteSyncResult(false, "Could not reach the sync server.", null, 0, 0)
        };
        var vm = CreateViewModel(new FakeSync(), store, remoteSync);

        await vm.RunRemoteSyncAsync();

        Assert.Equal("Could not reach the sync server.", vm.RemoteSyncStatusText);
        Assert.False(vm.IsRemoteSyncing);
        Assert.NotNull(store.Settings.ActiveCycle);
        Assert.Null(store.Settings.LastRemoteSyncUtc);
    }

    [Fact]
    public async Task SampleAppended_TriggersRemoteSync()
    {
        var store = ConfiguredRemoteStore();
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        var remoteSync = new FakeRemoteSync();
        var vm = CreateViewModel(sync, store, remoteSync);
        var snapshot = SnapshotFor(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1), 10, 2);

        sync.RaiseSampleAppended(snapshot);

        for (var i = 0; i < 200 && remoteSync.SyncCount == 0; i++)
            await Task.Delay(10);
        Assert.Equal(1, remoteSync.SyncCount);
        Assert.Equal("configured-machine", remoteSync.LastLocal!.MachineName);
    }

    [Fact]
    public void RemoteSyncEnabled_WhenTurnedOnWithConfig_StartsSync()
    {
        var store = new FakePlanStore
        {
            Settings = new AppSettings
            {
                RemoteSyncUrl = "http://server:8000",
                RemoteSyncApiKey = "key",
                RemoteSyncMachineName = "configured-machine"
            }
        };
        var remoteSync = new FakeRemoteSync();
        var vm = CreateViewModel(new FakeSync(), store, remoteSync);

        vm.RemoteSyncEnabled = true;

        Assert.True(store.Settings.RemoteSyncEnabled);
        Assert.True(vm.IsRemoteSyncing || remoteSync.SyncCount == 1);
    }

    [Fact]
    public async Task RunRemoteSyncAsync_MergesRemoteSamplesIntoExistingCycle()
    {
        var calculator = new CycleCalculator();
        var live = calculator.GenerateCycleFromBounds(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 1));
        var store = ConfiguredRemoteStore();
        store.Settings.ActiveCycle = live;
        var sync = new FakeSync { IsSignedIn = true, Status = SyncStatus.Ok };
        sync.SampleStore.Document = new UsageSampleDocument
        {
            CycleStartUtc = AtUtc(2026, 8, 1),
            Samples = [SampleAt(new DateTime(2026, 8, 10, 10, 0, 0), 10, 2)]
        };
        var remoteSync = new FakeRemoteSync
        {
            Result = new RemoteSyncResult(
                true,
                null,
                new RemoteSyncCanonicalState(
                    AtUtc(2026, 8, 1),
                    live,
                    [],
                    [
                        SampleAt(new DateTime(2026, 8, 10, 10, 0, 0), 10, 2),
                        SampleAt(new DateTime(2026, 8, 12, 10, 0, 0), 20, 4)
                    ]),
                0,
                2)
        };
        var vm = CreateViewModel(sync, store, remoteSync);
        Assert.True(vm.IsInitialized);

        await vm.RunRemoteSyncAsync();

        Assert.Equal(2, sync.SampleStore.Document.Samples.Count);
        Assert.Equal(live.CycleStart, store.Settings.ActiveCycle!.CycleStart);
        Assert.Contains("2 samples", vm.RemoteSyncStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoteSyncTimer_FiresAfterTenIdleMinutes()
    {
        var store = ConfiguredRemoteStore();
        var remoteSync = new FakeRemoteSync();
        var dispatcher = new FakeDispatcher();
        var vm = CreateViewModel(new FakeSync(), store, new FakeStartup(), new FakeClock(), remoteSync, dispatcher);

        await vm.RunRemoteSyncAsync();

        Assert.Equal(1, remoteSync.SyncCount);
        Assert.True(dispatcher.Timer.IsStarted);
        Assert.Equal(TimeSpan.FromMinutes(10), dispatcher.Timer.Interval);

        dispatcher.Timer.Fire();

        Assert.Equal(2, remoteSync.SyncCount);
        Assert.True(dispatcher.Timer.IsStarted);
    }

    [Fact]
    public void RemoteSyncTimer_StopsWhenDisabled()
    {
        var store = ConfiguredRemoteStore();
        var dispatcher = new FakeDispatcher();
        var vm = CreateViewModel(new FakeSync(), store, new FakeStartup(), new FakeClock(), new FakeRemoteSync(), dispatcher);

        Assert.True(dispatcher.Timer.IsStarted);

        vm.RemoteSyncEnabled = false;

        Assert.False(dispatcher.Timer.IsStarted);
    }

    private static FakePlanStore ConfiguredRemoteStore() =>
        new()
        {
            Settings = new AppSettings
            {
                RemoteSyncEnabled = true,
                RemoteSyncUrl = "http://server:8000",
                RemoteSyncApiKey = "key",
                RemoteSyncMachineName = "configured-machine"
            }
        };

    private static MainViewModel CreateViewModel(FakeSync sync, FakePlanStore store, FakeRemoteSync remoteSync) =>
        CreateViewModel(sync, store, new FakeStartup(), new FakeClock(), remoteSync, new FakeDispatcher());

    private static DateTimeOffset AtUtc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private static MainViewModel CreateViewModel(bool signedIn) =>
        CreateViewModel(new FakeSync { IsSignedIn = signedIn, Status = signedIn ? SyncStatus.Ok : SyncStatus.SignedOut });

    private static MainViewModel CreateViewModel(FakeSync sync) =>
        CreateViewModel(sync, new FakePlanStore());

    private static MainViewModel CreateViewModel(FakeSync sync, FakePlanStore store) =>
        CreateViewModel(sync, store, new FakeStartup());

    private static MainViewModel CreateViewModel(FakeSync sync, FakePlanStore store, FakeStartup startup) =>
        CreateViewModel(sync, store, startup, new FakeClock());

    private static MainViewModel CreateViewModel(
        FakeSync sync,
        FakePlanStore store,
        FakeStartup startup,
        FakeClock clock) =>
        CreateViewModel(sync, store, startup, clock, new FakeRemoteSync(), new FakeDispatcher());

    private static MainViewModel CreateViewModel(
        FakeSync sync,
        FakePlanStore store,
        FakeStartup startup,
        FakeClock clock,
        FakeRemoteSync remoteSync,
        FakeDispatcher dispatcher) =>
        new(
            clock,
            new CycleCalculator(),
            store,
            startup,
            sync,
            new DataBackupService(store, sync.SampleStore),
            remoteSync,
            dispatcher);

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

    private static UsageSnapshot SnapshotFor(
        DateTime startLocal,
        DateTime endLocal,
        decimal cursor,
        decimal other,
        DateTime? fetchedLocal = null)
    {
        return new UsageSnapshot
        {
            BillingCycleStartUtc = AtLocal(startLocal),
            BillingCycleEndUtc = AtLocal(endLocal),
            CursorModelsPercent = cursor,
            OtherModelsPercent = other,
            FetchedAtUtc = AtLocal(fetchedLocal ?? startLocal.AddHours(1))
        };
    }

    private static DateTimeOffset AtLocal(DateTime local)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset);
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
        public event EventHandler<UsageSnapshot>? SnapshotReceived;
        public event EventHandler<UsageSnapshot>? SampleAppended;
        public DateTimeOffset? SamplesCycleStartUtc => SampleStore.Document.CycleStartUtc;
        public int RefreshNowCount { get; private set; }
        public Task StartAsync(bool autoSyncEnabled, int intervalHours) => Task.CompletedTask;
        public Task RefreshNowAsync(bool allowInteractiveLogin)
        {
            RefreshNowCount++;
            return Task.CompletedTask;
        }
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
        public int MergeRemoteSamples(IReadOnlyList<UsageSample> remoteSamples, DateTimeOffset? remoteCycleStartUtc)
        {
            var merged = RemoteSyncMerge.UnionSamples(SampleStore.Document.Samples, remoteSamples);
            var added = merged.Count - SampleStore.Document.Samples.Count;
            SampleStore.Document = new UsageSampleDocument
            {
                Version = SampleStore.Document.Version,
                CycleStartUtc = RemoteSyncMerge.LatestCycleStartUtc(SampleStore.Document.CycleStartUtc, remoteCycleStartUtc),
                Samples = merged
            };
            return Math.Max(added, 0);
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

        public void RaiseSnapshotReceived(UsageSnapshot snapshot) =>
            SnapshotReceived?.Invoke(this, snapshot);

        public void RaiseSampleAppended(UsageSnapshot snapshot) =>
            SampleAppended?.Invoke(this, snapshot);
    }

    private sealed class FakeRemoteSync : IRemoteSyncService
    {
        public RemoteSyncResult Result { get; set; } =
            new(true, null, new RemoteSyncCanonicalState(null, null, [], []), 0, 0);
        public RemoteSyncLocalState? LastLocal { get; private set; }
        public int SyncCount { get; private set; }

        public Task<RemoteSyncResult> SyncAsync(RemoteSyncLocalState local, CancellationToken cancellationToken = default)
        {
            LastLocal = local;
            SyncCount++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeDispatcher : IUiDispatcher
    {
        public FakeTimer Timer { get; } = new();
        public bool CheckAccess() => true;
        public void Post(Action action) => action();
        public IUiTimer CreateTimer() => Timer;
    }

    private sealed class FakeTimer : IUiTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public bool IsStarted { get; private set; }
        public event EventHandler? Tick;

        public void Start() => IsStarted = true;

        public void Stop() => IsStarted = false;

        public void Fire() => Tick?.Invoke(this, EventArgs.Empty);
    }
}
