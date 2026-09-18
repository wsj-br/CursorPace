using CursorPace.Models;

namespace CursorPace.Services;

/// <summary>
/// Local snapshot pushed to the sync server.
/// </summary>
public sealed record RemoteSyncLocalState(
    string BaseUrl,
    string ApiKey,
    string MachineName,
    DateTimeOffset? CycleStartUtc,
    QuotaCycle? ActiveCycle,
    IReadOnlyList<QuotaCycle> CycleHistory,
    IReadOnlyList<UsageSample> Samples);

/// <summary>
/// Canonical state pulled from the sync server after the push merge.
/// </summary>
public sealed record RemoteSyncCanonicalState(
    DateTimeOffset? CycleStartUtc,
    QuotaCycle? ActiveCycle,
    IReadOnlyList<QuotaCycle> CycleHistory,
    IReadOnlyList<UsageSample> Samples);

public sealed record RemoteSyncResult(
    bool Success,
    string? ErrorMessage,
    RemoteSyncCanonicalState? Canonical,
    int PushedNewCount,
    int ServerTotal);

public interface IRemoteSyncService
{
    /// <summary>
    /// Pushes local state to the server, then pulls the merged canonical state.
    /// Never throws for transport or protocol failures; reports them in the result.
    /// </summary>
    Task<RemoteSyncResult> SyncAsync(RemoteSyncLocalState local, CancellationToken cancellationToken = default);
}
