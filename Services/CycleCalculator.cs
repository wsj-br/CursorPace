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

        var cycle = new QuotaCycle
        {
            RenewalDay = renewalDay,
            CycleStart = cycleStart,
            NextRenewal = nextRenewal
        };

        RebuildDays(cycle);
        return cycle;
    }

    public DateTime FindCycleStart(int renewalDay, DateTime referenceDate)
    {
        var candidate = new DateTime(referenceDate.Year, referenceDate.Month, 1);

        if (DateTime.DaysInMonth(candidate.Year, candidate.Month) >= renewalDay)
        {
            candidate = new DateTime(candidate.Year, candidate.Month, renewalDay);
            if (candidate <= referenceDate)
                return candidate;
        }

        while (true)
        {
            candidate = candidate.AddMonths(-1);
            if (DateTime.DaysInMonth(candidate.Year, candidate.Month) >= renewalDay)
                return new DateTime(candidate.Year, candidate.Month, renewalDay);
        }
    }

    public DateTime FindNextRenewal(int renewalDay, DateTime cycleStart)
    {
        var candidate = cycleStart.AddMonths(1);

        while (true)
        {
            if (DateTime.DaysInMonth(candidate.Year, candidate.Month) >= renewalDay)
                return new DateTime(candidate.Year, candidate.Month, renewalDay);

            candidate = candidate.AddMonths(1);
        }
    }

    public int TotalDays(QuotaCycle cycle)
    {
        var totalDays = (cycle.NextRenewal - cycle.CycleStart).Days;
        if (totalDays <= 0)
            throw new InvalidOperationException("Cycle has no days.");
        return totalDays;
    }

    public decimal LinearPercent(int dayNumber, int totalDays)
    {
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));
        if (totalDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalDays));

        return 100m * (dayNumber - 1) / totalDays;
    }

    public decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber)
    {
        var totalDays = TotalDays(cycle);
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));

        var edit = GetEditValue(cycle, kind, dayNumber);
        if (edit.HasValue)
            return edit.Value;

        // One segment only: previous anchor → next anchor.
        // Anchors are cycle start (day 1 at 0%), each edit, then renewal (100%).
        var previous = FindPreviousEdit(cycle, kind, dayNumber);
        var startDay = previous?.DayNumber ?? 1;
        var startPercent = previous != null
            ? GetEditValue(previous, kind)!.Value
            : 0m;

        var next = FindNextEdit(cycle, kind, dayNumber);
        var endDay = next?.DayNumber ?? (totalDays + 1);
        var endPercent = next != null
            ? GetEditValue(next, kind)!.Value
            : 100m;

        var span = endDay - startDay;
        if (span <= 0)
            return startPercent;

        return startPercent + (dayNumber - startDay) * (endPercent - startPercent) / span;
    }

    public void RebuildDays(QuotaCycle cycle)
    {
        var totalDays = TotalDays(cycle);
        var days = new List<QuotaDayEntry>(totalDays);

        for (int k = 0; k < totalDays; k++)
        {
            var dayNumber = k + 1;
            days.Add(new QuotaDayEntry
            {
                DayNumber = dayNumber,
                Date = cycle.CycleStart.AddDays(k),
                CursorModelsPercent = ExpectedPercent(cycle, QuotaKind.CursorModels, dayNumber),
                OtherModelsPercent = ExpectedPercent(cycle, QuotaKind.OtherModels, dayNumber),
                CursorModelsIsManual = GetEditValue(cycle, QuotaKind.CursorModels, dayNumber).HasValue,
                OtherModelsIsManual = GetEditValue(cycle, QuotaKind.OtherModels, dayNumber).HasValue
            });
        }

        cycle.Days = days;
    }

    public void SetManual(QuotaCycle cycle, QuotaKind kind, int dayNumber, decimal percent)
    {
        var totalDays = TotalDays(cycle);
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));

        var edit = cycle.Edits.FirstOrDefault(e => e.DayNumber == dayNumber);
        if (edit == null)
        {
            edit = new QuotaDayEdit { DayNumber = dayNumber };
            cycle.Edits.Add(edit);
        }

        SetEditValue(edit, kind, percent);
        RebuildDays(cycle);
    }

    public void ClearManual(QuotaCycle cycle, QuotaKind kind, int dayNumber)
    {
        var edit = cycle.Edits.FirstOrDefault(e => e.DayNumber == dayNumber);
        if (edit == null)
            return;

        SetEditValue(edit, kind, null);
        if (!edit.HasAnyValue)
            cycle.Edits.Remove(edit);

        RebuildDays(cycle);
    }

    private static QuotaDayEdit? FindPreviousEdit(QuotaCycle cycle, QuotaKind kind, int dayNumber)
    {
        QuotaDayEdit? previous = null;
        foreach (var edit in cycle.Edits)
        {
            if (edit.DayNumber >= dayNumber || !GetEditValue(edit, kind).HasValue)
                continue;
            if (previous == null || edit.DayNumber > previous.DayNumber)
                previous = edit;
        }

        return previous;
    }

    private static QuotaDayEdit? FindNextEdit(QuotaCycle cycle, QuotaKind kind, int dayNumber)
    {
        QuotaDayEdit? next = null;
        foreach (var edit in cycle.Edits)
        {
            if (edit.DayNumber <= dayNumber || !GetEditValue(edit, kind).HasValue)
                continue;
            if (next == null || edit.DayNumber < next.DayNumber)
                next = edit;
        }

        return next;
    }

    private static decimal? GetEditValue(QuotaCycle cycle, QuotaKind kind, int dayNumber)
    {
        var edit = cycle.Edits.FirstOrDefault(e => e.DayNumber == dayNumber);
        return edit == null ? null : GetEditValue(edit, kind);
    }

    private static decimal? GetEditValue(QuotaDayEdit edit, QuotaKind kind) =>
        kind == QuotaKind.CursorModels ? edit.CursorModelsPercent : edit.OtherModelsPercent;

    private static void SetEditValue(QuotaDayEdit edit, QuotaKind kind, decimal? percent)
    {
        if (kind == QuotaKind.CursorModels)
            edit.CursorModelsPercent = percent;
        else
            edit.OtherModelsPercent = percent;
    }
}
