using CursorUsageProgress.Models;
using CursorUsageProgress.Services;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Tests;

public class MainViewModelTests
{
    [Fact]
    public void StartEditingDay_WhenDisconnected_OpensEditPanel()
    {
        var vm = CreateViewModel(signedIn: false);

        vm.StartEditingDay(SampleDay());

        Assert.True(vm.CanEditDays);
        Assert.True(vm.IsEditingDay);
        Assert.True(vm.ApplyEditCommand.CanExecute(null));
    }

    [Fact]
    public void StartEditingDay_WhenConnected_DoesNotOpenEditPanel()
    {
        var vm = CreateViewModel(signedIn: true);

        vm.StartEditingDay(SampleDay());

        Assert.False(vm.CanEditDays);
        Assert.False(vm.IsEditingDay);
        Assert.False(vm.ApplyEditCommand.CanExecute(null));
        Assert.False(vm.ResetDayCommand.CanExecute(null));
    }

    [Fact]
    public void ResetCycleCommand_WhenConnected_CannotExecute()
    {
        var vm = CreateInitializedViewModel(signedIn: true);

        Assert.False(vm.ResetCycleCommand.CanExecute(null));
    }

    [Fact]
    public void ResetCycleCommand_WhenDisconnected_CanExecute()
    {
        var vm = CreateInitializedViewModel(signedIn: false);

        Assert.True(vm.ResetCycleCommand.CanExecute(null));
    }

    [Fact]
    public void SignIn_DisablesResetCycleCommand()
    {
        var sync = new FakeSync { IsSignedIn = false };
        var vm = CreateInitializedViewModel(sync);

        Assert.True(vm.ResetCycleCommand.CanExecute(null));

        sync.SetSignedIn(true);

        Assert.False(vm.ResetCycleCommand.CanExecute(null));
    }

    [Fact]
    public void SignIn_ClosesOpenDayEditPanel()
    {
        var sync = new FakeSync { IsSignedIn = false };
        var vm = CreateViewModel(sync);
        vm.StartEditingDay(SampleDay());
        Assert.True(vm.IsEditingDay);

        sync.SetSignedIn(true);

        Assert.False(vm.CanEditDays);
        Assert.False(vm.IsEditingDay);
        Assert.False(vm.ApplyEditCommand.CanExecute(null));
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
        var cycle = calculator.GenerateCycle(1, new DateTime(2026, 8, 18));
        var lastLocal = new DateTime(2026, 8, 18, 20, 0, 0);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart, 0m, 0m),
            SampleAt(lastLocal, 75m, 70m)
        };
        var store = new FakePlanStore
        {
            Settings = new AppSettings { RenewalDay = 1, ActiveCycle = cycle }
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

    private static MainViewModel CreateViewModel(bool signedIn) =>
        CreateViewModel(new FakeSync { IsSignedIn = signedIn, Status = signedIn ? SyncStatus.Ok : SyncStatus.SignedOut });

    private static MainViewModel CreateViewModel(FakeSync sync) =>
        CreateViewModel(sync, new FakePlanStore());

    private static MainViewModel CreateViewModel(FakeSync sync, FakePlanStore store) =>
        new(
            new FakeClock(),
            new CycleCalculator(),
            store,
            new FakeStartup(),
            sync);

    private static MainViewModel CreateInitializedViewModel(bool signedIn) =>
        CreateInitializedViewModel(new FakeSync
        {
            IsSignedIn = signedIn,
            Status = signedIn ? SyncStatus.Ok : SyncStatus.SignedOut
        });

    private static MainViewModel CreateInitializedViewModel(FakeSync sync)
    {
        var calculator = new CycleCalculator();
        var cycle = calculator.GenerateCycle(1, new DateTime(2026, 8, 18));
        var store = new FakePlanStore
        {
            Settings = new AppSettings { RenewalDay = 1, ActiveCycle = cycle }
        };
        return CreateViewModel(sync, store);
    }

    private static DayRowViewModel SampleDay() =>
        new(
            new QuotaDayEntry { DayNumber = 2, Date = new DateTime(2026, 8, 3) },
            expectedQuotaCursor: 10,
            expectedQuotaOther: 20,
            projectedQuotaCursor: null,
            projectedQuotaOther: null,
            cursorWillRunOut: false,
            otherWillRunOut: false);

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
        public void Register() => IsRegistered = true;
        public void Unregister() => IsRegistered = false;
    }

    private sealed class FakeSync : IUsageSyncService
    {
        public SyncStatus Status { get; set; } = SyncStatus.SignedOut;
        public bool IsSignedIn { get; set; }
        public string StatusText { get; set; } = "Not signed in";
        public DateTimeOffset? LastSuccessUtc { get; set; }
        public IReadOnlyList<UsageSample> Samples { get; set; } = [];
        public event EventHandler? StateChanged;
#pragma warning disable CS0067 // Required by IUsageSyncService
        public event EventHandler<UsageSnapshot>? SnapshotReceived;
#pragma warning restore CS0067
        public Task StartAsync(bool autoSyncEnabled, int intervalHours) => Task.CompletedTask;
        public Task RefreshNowAsync(bool allowInteractiveLogin) => Task.CompletedTask;
        public Task SignInAsync() => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public void SetIntervalHours(int hours) { }
        public void SetAutoSyncEnabled(bool enabled) { }
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
