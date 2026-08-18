namespace CursorUsageProgress.Models;

public sealed class UsageChartMarker
{
    public required ChartMarkerKind MarkerKind { get; init; }
    public QuotaKind? QuotaKind { get; init; }
    public required decimal X { get; init; }
    public required decimal Y { get; init; }
    public DateTime Instant { get; init; }
}
