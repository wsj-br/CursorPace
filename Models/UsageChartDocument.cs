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
    public decimal VisibleStartX { get; init; }
    public decimal VisibleEndX { get; init; }
    public DateTime VisibleStart { get; init; }
    public DateTime VisibleEnd { get; init; }
    public UsageChartRange Range { get; init; } = UsageChartRange.OneMonth;
    public bool IsCustomViewport { get; init; }
    public bool UsesIntradayAxis { get; init; }
    public UsageChartPoint? LastMeasuredCursor { get; init; }
    public UsageChartPoint? LastMeasuredOther { get; init; }
    public decimal YMin { get; init; }
    public decimal YMax { get; init; }
    public decimal YTickStep { get; init; } = 10m;
    public decimal UsageLimitPercent { get; init; } = 100m;

    public bool HasCursorUsage => CursorUsage.Count >= 2;
    public bool HasOtherUsage => OtherUsage.Count >= 2;
    public bool HasCursorEstimated => CursorEstimated.Count > 0;
    public bool HasOtherEstimated => OtherEstimated.Count > 0;
}
