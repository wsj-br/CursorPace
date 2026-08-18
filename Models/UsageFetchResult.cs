namespace CursorUsageProgress.Models;

public sealed record UsageFetchResult(
    UsageFetchStatus Status,
    UsageSnapshot? Snapshot,
    string? Message,
    int HttpStatus);
