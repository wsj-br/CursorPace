using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.Services;

public interface ICycleCalculator
{
    QuotaCycle GenerateCycle(int renewalDay, DateTime referenceDate);
    DateTime FindCycleStart(int renewalDay, DateTime referenceDate);
    DateTime FindNextRenewal(int renewalDay, DateTime cycleStart);
    void RecalculateQuota(QuotaCycle cycle, QuotaKind kind, int fromDayNumber);
}
