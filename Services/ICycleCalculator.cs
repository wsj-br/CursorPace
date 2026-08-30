using CursorPace.Models;

namespace CursorPace.Services;

public interface ICycleCalculator
{
    QuotaCycle GenerateCycleFromBounds(DateTime startLocal, DateTime endLocal);
    int TotalDays(QuotaCycle cycle);
    decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber);
    decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber, IReadOnlyList<UsageSample>? samples);
    decimal ExpectedPercentAt(QuotaCycle cycle, QuotaKind kind, DateTime local, IReadOnlyList<UsageSample>? samples);
    decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind);
    decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples);
    decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber);
    decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber, IReadOnlyList<UsageSample>? samples);
    decimal? ProjectedPercentAt(QuotaCycle cycle, QuotaKind kind, DateTime local, IReadOnlyList<UsageSample>? samples);
    bool TryGetLastUpdate(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples, out DateTime instant, out decimal percent);
    DateTime? EstimateRunOutInstant(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples);
    int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind);
    int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples);
    void RebuildDays(QuotaCycle cycle);
    void RebuildDays(QuotaCycle cycle, IReadOnlyList<UsageSample>? samples, DateTime? today);
}
