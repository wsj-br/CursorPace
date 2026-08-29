using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public interface IUsageSyncService : IDisposable
{
    SyncStatus Status { get; }
    bool IsSignedIn { get; }
    string StatusText { get; }
    DateTimeOffset? LastSuccessUtc { get; }
    IReadOnlyList<UsageSample> Samples { get; }
    event EventHandler? StateChanged;
    event EventHandler<UsageSnapshot>? SnapshotReceived;
    Task StartAsync(bool autoSyncEnabled, int intervalHours);
    Task RefreshNowAsync(bool allowInteractiveLogin);
    Task SignInAsync();
    Task DisconnectAsync();
    void SetIntervalHours(int hours);
    void SetAutoSyncEnabled(bool enabled);
    void ReloadPersistedUsage(DateTimeOffset? lastSuccessUtc);
}
