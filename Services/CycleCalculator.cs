using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.Services;

public sealed class CycleCalculator : ICycleCalculator
{
    public QuotaCycle GenerateCycle(int renewalDay, DateTime referenceDate)
    {
        if (renewalDay < 1 || renewalDay > 31)
            throw new ArgumentOutOfRangeException(nameof(renewalDay));

        var cycleStart = FindCycleStart(renewalDay, referenceDate);
        var nextRenewal = FindNextRenewal(renewalDay, cycleStart);
        var totalDays = (nextRenewal - cycleStart).Days;

        var days = new List<QuotaDayEntry>(totalDays);
        for (int k = 0; k < totalDays; k++)
        {
            var date = cycleStart.AddDays(k);
            var defaultPercent = 100m * k / totalDays;

            days.Add(new QuotaDayEntry
            {
                DayNumber = k + 1,
                Date = date,
                CursorModelsPercent = defaultPercent,
                OtherModelsPercent = defaultPercent,
                CursorModelsIsManual = false,
                OtherModelsIsManual = false
            });
        }

        return new QuotaCycle
        {
            RenewalDay = renewalDay,
            CycleStart = cycleStart,
            NextRenewal = nextRenewal,
            Days = days
        };
    }

    public DateTime FindCycleStart(int renewalDay, DateTime referenceDate)
    {
        var candidate = new DateTime(referenceDate.Year, referenceDate.Month, 1);

        // Find valid renewal in current month or previous
        if (DateTime.DaysInMonth(candidate.Year, candidate.Month) >= renewalDay)
        {
            candidate = new DateTime(candidate.Year, candidate.Month, renewalDay);
            if (candidate <= referenceDate)
                return candidate;
        }

        // Search backwards for valid renewal day
        while (true)
        {
            candidate = candidate.AddMonths(-1);
            if (DateTime.DaysInMonth(candidate.Year, candidate.Month) >= renewalDay)
            {
                return new DateTime(candidate.Year, candidate.Month, renewalDay);
            }
        }
    }

    public DateTime FindNextRenewal(int renewalDay, DateTime cycleStart)
    {
        var candidate = cycleStart.AddMonths(1);

        while (true)
        {
            if (DateTime.DaysInMonth(candidate.Year, candidate.Month) >= renewalDay)
            {
                return new DateTime(candidate.Year, candidate.Month, renewalDay);
            }
            candidate = candidate.AddMonths(1);
        }
    }

    public void RecalculateQuota(QuotaCycle cycle, QuotaKind kind, int fromDayNumber)
    {
        if (fromDayNumber < 1 || fromDayNumber > cycle.Days.Count)
            throw new ArgumentOutOfRangeException(nameof(fromDayNumber));

        var fromIndex = fromDayNumber - 1;
        var entry = cycle.Days[fromIndex];
        var startPercent = kind == QuotaKind.CursorModels ? entry.CursorModelsPercent : entry.OtherModelsPercent;

        var remainingIntervals = cycle.Days.Count - fromIndex;
        var dailyIncrement = (100m - startPercent) / remainingIntervals;

        for (int i = fromIndex + 1; i < cycle.Days.Count; i++)
        {
            var newPercent = startPercent + (i - fromIndex) * dailyIncrement;

            if (kind == QuotaKind.CursorModels)
            {
                cycle.Days[i].CursorModelsPercent = newPercent;
                cycle.Days[i].CursorModelsIsManual = false;
            }
            else
            {
                cycle.Days[i].OtherModelsPercent = newPercent;
                cycle.Days[i].OtherModelsIsManual = false;
            }
        }
    }
}
