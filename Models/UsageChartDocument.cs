namespace CursorUsageProgress.Models;

public sealed class UsageChartDocument
{
    public IReadOnlyList<UsageChartPoint> CursorExpected { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> OtherExpected { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> CursorEstimated { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> OtherEstimated { get; init; } = [];
    public IReadOnlyList<UsageChartMarker> Markers { get; init; } = [];
    public IReadOnlyList<UsageChartAxisTick> DayTicks { get; init; } = [];
    public decimal PlotEndX { get; init; }
    public decimal SlotEndX { get; init; }
    public decimal RenewalX { get; init; }
    public DateTime CycleStart { get; init; }
    public DateTime NextRenewal { get; init; }
    public decimal YMax { get; init; }
    public decimal UsageLimitPercent { get; init; } = 100m;

    public bool HasCursorEstimated => CursorEstimated.Count > 0;
    public bool HasOtherEstimated => OtherEstimated.Count > 0;
}
