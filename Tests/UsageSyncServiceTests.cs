using System.Globalization;
using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class UsageSyncServiceTests
{
    [Fact]
    public void Constructor_WhenFlagFalseAndNoPriorSync_StartsSignedOut()
    {
        // The WebView profile folder is not a reliable "signed in" signal: the
        // browser engine writes cache/HSTS/storage housekeeping files to it as
        // soon as it is first used, regardless of whether login ever succeeded.
        var sync = CreateService(new AppSettings { CursorAccountConnected = false });

        Assert.False(sync.IsSignedIn);
        Assert.Equal(SyncStatus.SignedOut, sync.Status);
    }

    [Fact]
    public void Constructor_WhenPriorSyncExists_StartsSignedIn()
    {
        var cycle = new CycleCalculator().GenerateCycleFromBounds(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 1));
        var sync = CreateService(new AppSettings
        {
            CursorAccountConnected = false,
            ActiveCycle = cycle,
            LastUsageSyncUtc = DateTimeOffset.Parse("2026-08-18T10:00:00Z")
        });

        Assert.True(sync.IsSignedIn);
        var last = DateTimeOffset.Parse("2026-08-18T10:00:00Z");
        Assert.Equal(
            "Cursor " + last.ToLocalTime().DateTime.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture),
            sync.StatusText);
    }

    [Fact]
    public async Task AuthRequired_WhenAlreadySignedIn_KeepsSignedIn()
    {
        var client = new FakeUsageClient
        {
            FetchResult = new UsageFetchResult(
                UsageFetchStatus.AuthRequired,
                null,
                "Sign in to Cursor to sync usage.",
                401)
        };
        var sync = CreateService(new AppSettings { CursorAccountConnected = true }, client);

        Assert.True(sync.IsSignedIn);

        await sync.RefreshNowAsync(allowInteractiveLogin: false);

        Assert.True(sync.IsSignedIn);
        Assert.Equal(SyncStatus.AuthRequired, sync.Status);
    }

    [Fact]
    public async Task AuthRequired_WhenNotYetSignedIn_StaysSignedOut()
    {
        // Covers clicking Continue in the sign-in window before Cursor actually
        // accepts a session: a failed/cancelled attempt must not flip the app to
        // "connected".
        var client = new FakeUsageClient
        {
            FetchResult = new UsageFetchResult(
                UsageFetchStatus.AuthRequired,
                null,
                "Sign in to Cursor to sync usage.",
                401)
        };
        var sync = CreateService(new AppSettings { CursorAccountConnected = false }, client);

        Assert.False(sync.IsSignedIn);

        await sync.RefreshNowAsync(allowInteractiveLogin: false);

        Assert.False(sync.IsSignedIn);
        Assert.Equal(SyncStatus.AuthRequired, sync.Status);
    }

    [Fact]
    public void MergeRemoteSamples_UnionsSamplesAndUpdatesCycleStart()
    {
        var sync = CreateService(new AppSettings { CursorAccountConnected = true });
        var remote = new List<UsageSample>
        {
            new()
            {
                TimestampUtc = new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero),
                CursorModelsPercent = 10,
                OtherModelsPercent = 2
            }
        };

        var added = sync.MergeRemoteSamples(
            remote,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(1, added);
        Assert.Single(sync.Samples);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            sync.SamplesCycleStartUtc);

        Assert.Equal(0, sync.MergeRemoteSamples(
            remote,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task SampleAppended_RaisedOnlyWhenSampleStored()
    {
        var snapshot = new UsageSnapshot
        {
            BillingCycleStartUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            BillingCycleEndUtc = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            CursorModelsPercent = 10,
            OtherModelsPercent = 2,
            FetchedAtUtc = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero)
        };
        var client = new FakeUsageClient
        {
            FetchResult = new UsageFetchResult(UsageFetchStatus.Ok, snapshot, null, 200)
        };
        var sync = CreateService(new AppSettings { CursorAccountConnected = true }, client);
        var appended = 0;
        var received = 0;
        sync.SampleAppended += (_, _) => appended++;
        sync.SnapshotReceived += (_, _) => received++;

        await sync.RefreshNowAsync(allowInteractiveLogin: false);
        await sync.RefreshNowAsync(allowInteractiveLogin: false);

        Assert.Equal(2, received);
        Assert.Equal(1, appended);
    }

    [Fact]
    public async Task TimerTick_DoesNotFetchUntilPostedTurnRuns()
    {
        var client = new FakeUsageClient();
        var dispatcher = new QueuingUiDispatcher();
        var sync = new UsageSyncService(
            dispatcher,
            client,
            new FakeUsageSampleStore(),
            new FixedClock(new DateTime(2026, 8, 18, 12, 0, 0)),
            new FakeUsagePlanStore
            {
                Settings = new AppSettings
                {
                    CursorAccountConnected = true,
                    LastUsageSyncUtc = new DateTimeOffset(new DateTime(2026, 8, 18, 11, 50, 0))
                }
            });

        await sync.StartAsync(autoSyncEnabled: true, intervalHours: 1);
        Assert.Equal(0, client.FetchCount);

        dispatcher.Timer.RaiseTick();

        Assert.Equal(0, client.FetchCount);
        Assert.Equal(1, dispatcher.PendingPosts);

        dispatcher.RunNext();

        Assert.Equal(1, client.FetchCount);
    }

    private static UsageSyncService CreateService(AppSettings settings) =>
        CreateService(settings, new FakeUsageClient());

    private static UsageSyncService CreateService(
        AppSettings settings,
        FakeUsageClient client)
    {
        var store = new FakeUsagePlanStore { Settings = settings };
        return new UsageSyncService(
            new ImmediateUiDispatcher(),
            client,
            new FakeUsageSampleStore(),
            new FixedClock(new DateTime(2026, 8, 18, 12, 0, 0)),
            store);
    }

    private sealed class FakeUsagePlanStore : IPlanStore
    {
        public AppSettings Settings { get; set; } = new();
        public AppSettings Load() => Settings;
        public void Save(AppSettings settings) => Settings = settings;
    }

    private sealed class FakeUsageSampleStore : IUsageSampleStore
    {
        public UsageSampleDocument Load() => new();
        public void Save(UsageSampleDocument document)
        {
        }
    }

    private sealed class FakeUsageClient : ICursorUsageClient
    {
        public UsageFetchResult FetchResult { get; set; } = new(
            UsageFetchStatus.Ok,
            null,
            null,
            200);

        public int FetchCount { get; private set; }

        public Task<UsageFetchResult> FetchAsync(bool allowInteractiveLogin, CancellationToken cancellationToken = default)
        {
            FetchCount++;
            return Task.FromResult(FetchResult);
        }

        public Task DisconnectAsync() => Task.CompletedTask;
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime Now { get; } = now;
        public DateTime Today => Now.Date;
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public void Post(Action action) => action();

        public IUiTimer CreateTimer() => new NoOpUiTimer();
    }

    private sealed class NoOpUiTimer : IUiTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public event EventHandler? Tick
        {
            add { }
            remove { }
        }
        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    private sealed class QueuingUiDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> _posted = new();

        public ControllableTimer Timer { get; } = new();

        public int PendingPosts => _posted.Count;

        public bool CheckAccess() => true;

        public void Post(Action action) => _posted.Enqueue(action);

        public IUiTimer CreateTimer() => Timer;

        public void RunNext() => _posted.Dequeue().Invoke();
    }

    private sealed class ControllableTimer : IUiTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public event EventHandler? Tick;

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
    }
}

