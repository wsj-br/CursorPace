using CursorPace.Models;

namespace CursorPace.Services;

public interface ICursorUsageClient
{
    Task<UsageFetchResult> FetchAsync(bool allowInteractiveLogin, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}
