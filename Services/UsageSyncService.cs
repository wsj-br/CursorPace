using System.Globalization;
using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public sealed class UsageSyncService : IUsageSyncService
{
    private static readonly TimeSpan SampleMinGap = TimeSpan.FromSeconds(30);

    private readonly IUiDispatcher _dispatcher;
    private readonly ICursorUsageClient _client;
    private readonly IUsageSampleStore _sampleStore;
    private readonly IClock _clock;
    private readonly IUiTimer _timer;

    private UsageSampleDocument _document;
    private DateTimeOffset? _lastSuccessUtc;
    private bool _autoSyncEnabled = true;
    private int _intervalHours = 1;
    private DateTimeOffset? _backoffUntilUtc;
    private bool _started;

    public UsageSyncService(
        IUiDispatcher dispatcher,
        ICursorUsageClient client,
        IUsageSampleStore sampleStore,
        IClock clock,
        IPlanStore planStore)
    {
        _dispatcher = dispatcher;
        _client = client;
        _sampleStore = sampleStore;
        _clock = clock;
        _document = _sampleStore.Load();

        var settings = planStore.Load();
        _lastSuccessUtc = settings.LastUsageSyncUtc
            ?? (_document.Samples.Count == 0 ? null : _document.Samples[^1].TimestampUtc);

        _timer = dispatcher.CreateTimer();
        _timer.IsRepeating = false;
        _timer.Tick += OnTimerTick;

        var connected = settings.CursorAccountConnected
            || _client.HasPersistedProfile
            || (settings.ActiveCycle != null && settings.LastUsageSyncUtc != null);
        if (connected)
        {
            Status = _lastSuccessUtc != null || _document.Samples.Count > 0
                ? SyncStatus.Ok
                : SyncStatus.Idle;
            IsSignedIn = true;
            StatusText = Status == SyncStatus.Ok ? FormatUpdatedText() : "Connected";
        }
        else
        {
            Status = SyncStatus.SignedOut;
            IsSignedIn = false;
            StatusText = "Not signed in";
        }
    }

    public SyncStatus Status { get; private set; }
    public bool IsSignedIn { get; private set; }
    public string StatusText { get; private set; } = "Not signed in";
    public DateTimeOffset? LastSuccessUtc =>
        _lastSuccessUtc ?? (_document.Samples.Count == 0 ? null : _document.Samples[^1].TimestampUtc);
    public IReadOnlyList<UsageSample> Samples => _document.Samples;

    public event EventHandler? StateChanged;
    public event EventHandler<UsageSnapshot>? SnapshotReceived;

    public async Task StartAsync(bool autoSyncEnabled, int intervalHours)
    {
        _autoSyncEnabled = autoSyncEnabled;
        _intervalHours = SyncInterval.Clamp(intervalHours);
        _started = true;

        if (SyncSchedule.ShouldRefreshOnStart(
                _autoSyncEnabled,
                IsSignedIn,
                new DateTimeOffset(_clock.Now),
                LastSuccessUtc,
                _intervalHours))
        {
            await RefreshNowAsync(allowInteractiveLogin: false);
        }

        ResetTimer();
    }

    public Task RefreshNowAsync(bool allowInteractiveLogin) =>
        RunFetchAsync(allowInteractiveLogin);

    public Task SignInAsync() =>
        RunFetchAsync(allowInteractiveLogin: true);

    public async Task DisconnectAsync()
    {
        await _client.DisconnectAsync();
        SetStatus(SyncStatus.SignedOut, "Not signed in");
        ResetTimer();
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

    public void ReloadPersistedUsage(DateTimeOffset? lastSuccessUtc)
    {
        _document = _sampleStore.Load();
        _lastSuccessUtc = lastSuccessUtc
            ?? (_document.Samples.Count == 0 ? null : _document.Samples[^1].TimestampUtc);
        if (Status == SyncStatus.Ok)
            StatusText = FormatUpdatedText();
        RaiseStateChanged();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private async void OnTimerTick(object? sender, EventArgs args)
    {
        try
        {
            if (!_autoSyncEnabled
                || Status is SyncStatus.AuthRequired or SyncStatus.SignedOut or SyncStatus.Syncing)
            {
                ResetTimer();
                return;
            }

            if (_backoffUntilUtc is { } until && new DateTimeOffset(_clock.Now) < until)
            {
                ResetTimer();
                return;
            }

            await RunFetchAsync(allowInteractiveLogin: false);
        }
        catch (Exception ex)
        {
            SetStatus(
                SyncStatus.Error,
                string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not update usage."
                    : ex.Message);
            ResetTimer();
        }
    }

    private async Task RunFetchAsync(bool allowInteractiveLogin)
    {
        if (Status == SyncStatus.Syncing)
            return;

        SetStatus(SyncStatus.Syncing, "Updating…");
        var result = await _client.FetchAsync(allowInteractiveLogin);
        ApplyFetchResult(result);
        ResetTimer();
    }

    private void ApplyFetchResult(UsageFetchResult result)
    {
        switch (result.Status)
        {
            case UsageFetchStatus.Ok when result.Snapshot != null:
                _backoffUntilUtc = null;
                _lastSuccessUtc = result.Snapshot.FetchedAtUtc;
                var changed = UsageSampleAppender.ApplySnapshot(
                    _document,
                    result.Snapshot,
                    SampleMinGap,
                    out _);
                if (changed)
                    _sampleStore.Save(_document);
                SetStatus(SyncStatus.Ok, FormatUpdatedText());
                RaiseSnapshotReceived(result.Snapshot);
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
        if (!_started || !_autoSyncEnabled || !IsSignedIn)
            return;
        if (Status is SyncStatus.AuthRequired or SyncStatus.SignedOut)
            return;

        var now = _clock.Now;
        var next = SyncSchedule.NextAlignedLocal(now, _intervalHours);
        if (_backoffUntilUtc is { } until)
        {
            var untilLocal = until.LocalDateTime;
            if (untilLocal > next)
                next = untilLocal;
        }

        var delay = next - now;
        if (delay < TimeSpan.FromSeconds(1))
            delay = TimeSpan.FromSeconds(1);

        _timer.Interval = delay;
        _timer.Start();
    }

    private void SetStatus(SyncStatus status, string text)
    {
        Status = status;
        StatusText = status == SyncStatus.Ok ? FormatUpdatedText() : text;
        IsSignedIn = status switch
        {
            SyncStatus.Ok or SyncStatus.Idle => true,
            SyncStatus.SignedOut => false,
            SyncStatus.AuthRequired => IsSignedIn || _client.HasPersistedProfile,
            SyncStatus.Syncing or SyncStatus.RateLimited or SyncStatus.Error => IsSignedIn,
            _ => IsSignedIn,
        };
        RaiseStateChanged();
    }

    private void RaiseStateChanged() =>
        _dispatcher.Post(() => StateChanged?.Invoke(this, EventArgs.Empty));

    private void RaiseSnapshotReceived(UsageSnapshot snapshot) =>
        _dispatcher.Post(() => SnapshotReceived?.Invoke(this, snapshot));

    private string FormatUpdatedText()
    {
        if (LastSuccessUtc is not { } last)
            return "Connected";

        var local = last.ToLocalTime().DateTime;
        return "Updated " + local.ToString("dd/MM HH:mm", CultureInfo.CurrentCulture);
    }
}
