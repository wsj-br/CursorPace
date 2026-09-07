namespace CursorPace.Models;

public sealed class UsageChartDocument
{
    public IReadOnlyList<UsageChartPoint> ExpectedUsage { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> CursorUsage { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> OtherUsage { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> CursorEstimated { get; init; } = [];
    public IReadOnlyList<UsageChartPoint> OtherEstimated { get; init; } = [];
    public IReadOnlyList<UsageChartSlot> Slots { get; init; } = [];
    public decimal CycleSeconds { get; init; }
    public DateTime CycleStart { get; init; }
    public DateTime NextRenewal { get; init; }
    public decimal YMax { get; init; }
    public decimal UsageLimitPercent { get; init; } = 100m;

    public bool HasCursorUsage => CursorUsage.Count >= 2;
    public bool HasOtherUsage => OtherUsage.Count >= 2;
    public bool HasCursorEstimated => CursorEstimated.Count > 0;
    public bool HasOtherEstimated => OtherEstimated.Count > 0;
}
