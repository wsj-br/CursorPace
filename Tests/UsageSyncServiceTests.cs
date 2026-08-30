using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

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

        public Task<UsageFetchResult> FetchAsync(bool allowInteractiveLogin, CancellationToken cancellationToken = default) =>
            Task.FromResult(FetchResult);

        public Task DisconnectAsync() => Task.CompletedTask;
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime Now { get; } = now;
        public DateTime Today => Now.Date;
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
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
}

