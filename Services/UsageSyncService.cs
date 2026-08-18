using System.Globalization;
using CursorUsageProgress.Models;
using Microsoft.UI.Dispatching;

namespace CursorUsageProgress.Services;

public sealed class UsageSyncService : IUsageSyncService
{
    private static readonly TimeSpan SampleMinGap = TimeSpan.FromSeconds(30);

    private readonly DispatcherQueue _dispatcher;
    private readonly ICursorUsageClient _client;
    private readonly IUsageSampleStore _sampleStore;
    private readonly IClock _clock;
    private readonly DispatcherQueueTimer _timer;

    private UsageSampleDocument _document;
    private bool _autoSyncEnabled = true;
    private int _intervalHours = 1;
    private DateTimeOffset? _backoffUntilUtc;
    private bool _started;

    public UsageSyncService(
        DispatcherQueue dispatcher,
        ICursorUsageClient client,
        IUsageSampleStore sampleStore,
        IClock clock)
    {
        _dispatcher = dispatcher;
        _client = client;
        _sampleStore = sampleStore;
        _clock = clock;
        _document = _sampleStore.Load();

        _timer = _dispatcher.CreateTimer();
        _timer.Tick += OnTimerTick;

        Status = _document.Samples.Count > 0
            ? SyncStatus.Ok
            : (_client.HasPersistedProfile ? SyncStatus.Idle : SyncStatus.SignedOut);
        IsSignedIn = Status is SyncStatus.Ok or SyncStatus.Idle;
    }

    public SyncStatus Status { get; private set; }
    public bool IsSignedIn { get; private set; }
    public string StatusText { get; private set; } = "Not signed in";
    public DateTimeOffset? LastSuccessUtc =>
        _document.Samples.Count == 0 ? null : _document.Samples[^1].TimestampUtc;
    public IReadOnlyList<UsageSample> Samples => _document.Samples;

    public event EventHandler? StateChanged;
    public event EventHandler<UsageSnapshot>? SnapshotReceived;

    public async Task StartAsync(bool autoSyncEnabled, int intervalHours)
    {
        _autoSyncEnabled = autoSyncEnabled;
        _intervalHours = SyncInterval.Clamp(intervalHours);
        _started = true;
        ResetTimer();

        if (_autoSyncEnabled)
            await RefreshNowAsync(allowInteractiveLogin: false);
    }

    public Task RefreshNowAsync(bool allowInteractiveLogin) =>
        RunFetchAsync(allowInteractiveLogin);

    public Task SignInAsync() =>
        RunFetchAsync(allowInteractiveLogin: true);

    public async Task DisconnectAsync()
    {
        await _client.DisconnectAsync();
        SetStatus(SyncStatus.SignedOut, "Not signed in");
    }

    public void SetIntervalHours(int hours)
    {
        _intervalHours = SyncInterval.Clamp(hours);
        ResetTimer();
    }

    public void SetAutoSyncEnabled(bool enabled)
    {
        _autoSyncEnabled = enabled;
        ResetTimer();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private async void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (!_autoSyncEnabled)
            return;
        if (Status is SyncStatus.AuthRequired or SyncStatus.SignedOut or SyncStatus.Syncing)
            return;
        if (_backoffUntilUtc is { } until && new DateTimeOffset(_clock.Now) < until)
            return;

        await RunFetchAsync(allowInteractiveLogin: false);
    }

    private async Task RunFetchAsync(bool allowInteractiveLogin)
    {
        if (Status == SyncStatus.Syncing)
            return;

        SetStatus(SyncStatus.Syncing, "Updating…");
        var result = await _client.FetchAsync(allowInteractiveLogin);
        ApplyFetchResult(result);
    }

    private void ApplyFetchResult(UsageFetchResult result)
    {
        switch (result.Status)
        {
            case UsageFetchStatus.Ok when result.Snapshot != null:
                _backoffUntilUtc = null;
                var changed = UsageSampleAppender.ApplySnapshot(
                    _document,
                    result.Snapshot,
                    SampleMinGap,
                    out _);
                if (changed)
                    _sampleStore.Save(_document);
                SetStatus(SyncStatus.Ok, FormatUpdatedText());
                SnapshotReceived?.Invoke(this, result.Snapshot);
                break;

            case UsageFetchStatus.AuthRequired:
                SetStatus(SyncStatus.AuthRequired, result.Message ?? "Sign in to Cursor to sync usage.");
                break;

            case UsageFetchStatus.RateLimited:
                _backoffUntilUtc = new DateTimeOffset(_clock.Now).AddHours(_intervalHours);
                SetStatus(SyncStatus.RateLimited, result.Message ?? "Cursor rate-limited the request. Will retry later.");
                break;

            default:
                SetStatus(
                    SyncStatus.Error,
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Could not update usage."
                        : result.Message);
                break;
        }
    }

    private void ResetTimer()
    {
        _timer.Stop();
        if (!_started || !_autoSyncEnabled)
            return;

        _timer.Interval = TimeSpan.FromHours(_intervalHours);
        _timer.Start();
    }

    private void SetStatus(SyncStatus status, string text)
    {
        Status = status;
        StatusText = status == SyncStatus.Ok ? FormatUpdatedText() : text;
        IsSignedIn = status switch
        {
            SyncStatus.Ok or SyncStatus.Idle => true,
            SyncStatus.SignedOut or SyncStatus.AuthRequired => false,
            SyncStatus.Syncing or SyncStatus.RateLimited or SyncStatus.Error => IsSignedIn,
            _ => IsSignedIn,
        };
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private string FormatUpdatedText()
    {
        if (LastSuccessUtc is not { } last)
            return "Connected";

        var local = last.ToLocalTime().DateTime;
        return "Updated " + local.ToString("HH:mm", CultureInfo.CurrentCulture);
    }
}
