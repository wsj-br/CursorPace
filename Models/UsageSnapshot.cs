namespace CursorUsageProgress.Models;

public sealed class UsageSnapshot
{
    public required DateTimeOffset BillingCycleStartUtc { get; init; }
    public required DateTimeOffset BillingCycleEndUtc { get; init; }
    public required decimal CursorModelsPercent { get; init; }
    public required decimal OtherModelsPercent { get; init; }
    public required DateTimeOffset FetchedAtUtc { get; init; }
}
