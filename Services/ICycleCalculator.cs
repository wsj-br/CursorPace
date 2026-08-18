using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public interface ICycleCalculator
{
    QuotaCycle GenerateCycle(int renewalDay, DateTime referenceDate);
    QuotaCycle GenerateCycleFromBounds(DateTime startLocal, DateTime endLocal);
    DateTime FindCycleStart(int renewalDay, DateTime referenceDate);
    DateTime FindNextRenewal(int renewalDay, DateTime cycleStart);
    int TotalDays(QuotaCycle cycle);
    decimal LinearPercent(int dayNumber, int totalDays);
    decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber);
    decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber, IReadOnlyList<UsageSample>? samples);
    decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind);
    decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples);
    decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber);
    decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber, IReadOnlyList<UsageSample>? samples);
    decimal? ProjectedPercentAt(QuotaCycle cycle, QuotaKind kind, DateTime local, IReadOnlyList<UsageSample>? samples);
    bool TryGetLastUpdate(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples, out DateTime instant, out decimal percent);
    int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind);
    int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples);
    void RebuildDays(QuotaCycle cycle);
    void RebuildDays(QuotaCycle cycle, IReadOnlyList<UsageSample>? samples, DateTime? today);
    void SetManual(QuotaCycle cycle, QuotaKind kind, int dayNumber, decimal percent);
    void ClearManual(QuotaCycle cycle, QuotaKind kind, int dayNumber);
}
