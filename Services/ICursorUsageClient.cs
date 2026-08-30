using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public interface ICursorUsageClient
{
    Task<UsageFetchResult> FetchAsync(bool allowInteractiveLogin, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}
