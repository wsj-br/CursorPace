using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public interface ICursorUsageClient
{
    bool HasPersistedProfile { get; }
    Task<UsageFetchResult> FetchAsync(bool allowInteractiveLogin, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}
