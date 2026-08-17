using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.Services;

public interface ICycleCalculator
{
    QuotaCycle GenerateCycle(int renewalDay, DateTime referenceDate);
    DateTime FindCycleStart(int renewalDay, DateTime referenceDate);
    DateTime FindNextRenewal(int renewalDay, DateTime cycleStart);
    int TotalDays(QuotaCycle cycle);
    decimal LinearPercent(int dayNumber, int totalDays);
    decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber);
    void RebuildDays(QuotaCycle cycle);
    void SetManual(QuotaCycle cycle, QuotaKind kind, int dayNumber, decimal percent);
    void ClearManual(QuotaCycle cycle, QuotaKind kind, int dayNumber);
}
