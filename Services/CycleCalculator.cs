using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

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

    public QuotaCycle GenerateCycleFromBounds(DateTime startLocal, DateTime endLocal)
    {
        if (endLocal <= startLocal)
            throw new ArgumentOutOfRangeException(nameof(endLocal), "Cycle end must be after cycle start.");

        var cycle = new QuotaCycle
        {
            RenewalDay = startLocal.Day,
            CycleStart = startLocal,
            NextRenewal = endLocal
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

    public decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber) =>
        ExpectedPercent(cycle, kind, dayNumber, samples: null);

    public decimal ExpectedPercent(
        QuotaCycle cycle,
        QuotaKind kind,
        int dayNumber,
        IReadOnlyList<UsageSample>? samples)
    {
        var totalDays = TotalDays(cycle);
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));

        var anchors = CollectObservedAnchors(cycle, kind, samples);
        (int Day, decimal Percent)? previous = null;
        foreach (var anchor in anchors)
        {
            if (anchor.Day == dayNumber)
                return anchor.Percent;
            if (anchor.Day < dayNumber)
                previous = anchor;
        }

        if (previous is { } pin)
            return InterpolateToRenewal(cycle, pin.Day, pin.Percent, dayNumber);

        return LinearPercent(dayNumber, totalDays);
    }

    public static decimal AxisX(DateTime cycleStart, DateTime local) =>
        1m + (decimal)(local - cycleStart.Date).TotalDays;

    public static decimal AxisX(QuotaCycle cycle, DateTime local) =>
        AxisX(cycle.CycleStart, local);

    public decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind) =>
        EstimateDailyUsage(cycle, kind, samples: null);

    public decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples)
    {
        if (TryEstimateFromSamples(cycle, kind, samples, out var rate, out _))
            return rate;

        var points = CollectUsagePoints(cycle, kind);
        return MedianOfPairwiseSlopes(points.Select(p => ((decimal)p.Day, p.Percent)).ToList());
    }

    public decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber) =>
        ProjectedPercent(cycle, kind, dayNumber, samples: null);

    public decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber, IReadOnlyList<UsageSample>? samples)
    {
        var totalDays = TotalDays(cycle);
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));

        if (TryEstimateFromSamples(cycle, kind, samples, out var sampleRate, out var samplePoints)
            && samplePoints.Count > 0)
        {
            var last = samplePoints[^1];
            var targetX = dayNumber - 1m;
            return last.Percent + sampleRate!.Value * (targetX - last.X);
        }

        var rate = EstimateDailyUsage(cycle, kind);
        if (rate is null)
            return null;

        var lastEdit = CollectUsagePoints(cycle, kind)[^1];
        return lastEdit.Percent + rate.Value * (dayNumber - lastEdit.Day);
    }

    public decimal? ProjectedPercentAt(
        QuotaCycle cycle,
        QuotaKind kind,
        DateTime local,
        IReadOnlyList<UsageSample>? samples)
    {
        if (TryEstimateFromSamples(cycle, kind, samples, out var sampleRate, out var samplePoints)
            && samplePoints.Count > 0)
        {
            var last = samplePoints[^1];
            var targetX = (decimal)(local - cycle.CycleStart).TotalDays;
            return last.Percent + sampleRate!.Value * (targetX - last.X);
        }

        var rate = EstimateDailyUsage(cycle, kind);
        if (rate is null)
            return null;

        var lastEdit = CollectUsagePoints(cycle, kind)[^1];
        var lastInstant = cycle.CycleStart.Date.AddDays(lastEdit.Day - 1);
        var elapsed = (decimal)(local - lastInstant).TotalDays;
        return lastEdit.Percent + rate.Value * elapsed;
    }

    public bool TryGetLastUpdate(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples,
        out DateTime instant,
        out decimal percent)
    {
        instant = default;
        percent = 0m;
        DateTime? best = null;
        var bestPercent = 0m;

        if (samples != null)
        {
            foreach (var sample in samples)
            {
                var local = sample.TimestampUtc.LocalDateTime;
                if (local < cycle.CycleStart || local >= cycle.NextRenewal)
                    continue;
                if (best is null || local > best)
                {
                    best = local;
                    bestPercent = sample.GetPercent(kind);
                }
            }
        }

        foreach (var edit in cycle.Edits)
        {
            var value = GetEditValue(edit, kind);
            if (!value.HasValue)
                continue;

            var editInstant = cycle.CycleStart.Date.AddDays(edit.DayNumber - 1);
            if (best is null || editInstant > best)
            {
                best = editInstant;
                bestPercent = value.Value;
            }
        }

        if (best is null)
            return false;

        instant = best.Value;
        percent = bestPercent;
        return true;
    }

    public int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind) =>
        EstimateRunOutDayNumber(cycle, kind, samples: null);

    public int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples)
    {
        if (TryEstimateFromSamples(cycle, kind, samples, out var sampleRate, out var samplePoints)
            && samplePoints.Count > 0)
        {
            var last = samplePoints[^1];
            var lastDay = DayNumberFromOffset(cycle, last.X);
            if (last.Percent >= 100m)
                return lastDay;

            if (sampleRate is null || sampleRate.Value <= 0m)
                return null;

            var totalDays = TotalDays(cycle);
            var delta = (100m - last.Percent) / sampleRate.Value;
            var runOutLocal = cycle.CycleStart.AddDays((double)(last.X + delta));
            var runOutDay = (runOutLocal.Date - cycle.CycleStart.Date).Days + 1;
            if (runOutDay <= lastDay || runOutDay > totalDays)
                return null;

            return runOutDay;
        }

        var points = CollectUsagePoints(cycle, kind);
        if (points.Count == 0)
            return null;

        var lastEdit = points[^1];
        if (lastEdit.Percent >= 100m)
            return lastEdit.Day;

        var rate = EstimateDailyUsage(cycle, kind);
        if (rate is null || rate.Value <= 0m)
            return null;

        var total = TotalDays(cycle);
        var editDelta = (100m - lastEdit.Percent) / rate.Value;
        var editRunOutDay = lastEdit.Day + (int)decimal.Ceiling(editDelta);
        if (editRunOutDay <= lastEdit.Day || editRunOutDay > total)
            return null;

        return editRunOutDay;
    }

    public void RebuildDays(QuotaCycle cycle) =>
        RebuildDays(cycle, samples: null, today: null);

    public void RebuildDays(QuotaCycle cycle, IReadOnlyList<UsageSample>? samples, DateTime? today)
    {
        var totalDays = TotalDays(cycle);
        var days = new List<QuotaDayEntry>(totalDays);

        for (int k = 0; k < totalDays; k++)
        {
            var dayNumber = k + 1;
            var date = cycle.CycleStart.Date.AddDays(k);
            var cursorExpected = ExpectedPercent(cycle, QuotaKind.CursorModels, dayNumber, samples);
            var otherExpected = ExpectedPercent(cycle, QuotaKind.OtherModels, dayNumber, samples);
            var sample = today.HasValue && date > today.Value.Date
                ? null
                : FindLastSampleForDate(samples, date);

            days.Add(new QuotaDayEntry
            {
                DayNumber = dayNumber,
                Date = date,
                CursorModelsPercent = sample?.CursorModelsPercent ?? cursorExpected,
                OtherModelsPercent = sample?.OtherModelsPercent ?? otherExpected,
                CursorModelsIsManual = GetEditValue(cycle, QuotaKind.CursorModels, dayNumber).HasValue,
                OtherModelsIsManual = GetEditValue(cycle, QuotaKind.OtherModels, dayNumber).HasValue,
                CursorModelsIsActual = sample != null,
                OtherModelsIsActual = sample != null
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

    private List<(int Day, decimal Percent)> CollectObservedAnchors(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        var totalDays = TotalDays(cycle);
        var anchors = new List<(int Day, decimal Percent)>();
        for (var day = 1; day <= totalDays; day++)
        {
            var date = cycle.CycleStart.Date.AddDays(day - 1);
            var sample = FindLastSampleForDate(samples, date);
            if (sample != null)
            {
                anchors.Add((day, sample.GetPercent(kind)));
                continue;
            }

            var edit = GetEditValue(cycle, kind, day);
            if (edit.HasValue)
                anchors.Add((day, edit.Value));
        }

        return anchors;
    }

    private static decimal InterpolateToRenewal(
        QuotaCycle cycle,
        int startDay,
        decimal startPercent,
        int dayNumber)
    {
        var startX = (decimal)startDay;
        var endX = AxisX(cycle, cycle.NextRenewal);
        var span = endX - startX;
        if (span <= 0)
            return startPercent;

        return startPercent + (dayNumber - startX) * (100m - startPercent) / span;
    }

    private static decimal? GetEditValue(QuotaCycle cycle, QuotaKind kind, int dayNumber)
    {
        var edit = cycle.Edits.FirstOrDefault(e => e.DayNumber == dayNumber);
        return edit == null ? null : GetEditValue(edit, kind);
    }

    private static decimal? GetEditValue(QuotaDayEdit edit, QuotaKind kind) =>
        kind switch
        {
            QuotaKind.CursorModels => edit.CursorModelsPercent,
            QuotaKind.OtherModels => edit.OtherModelsPercent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private static void SetEditValue(QuotaDayEdit edit, QuotaKind kind, decimal? percent)
    {
        switch (kind)
        {
            case QuotaKind.CursorModels:
                edit.CursorModelsPercent = percent;
                break;
            case QuotaKind.OtherModels:
                edit.OtherModelsPercent = percent;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static List<(int Day, decimal Percent)> CollectUsagePoints(QuotaCycle cycle, QuotaKind kind)
    {
        var points = new List<(int Day, decimal Percent)>();
        var day1Edit = GetEditValue(cycle, kind, 1);
        points.Add((1, day1Edit ?? 0m));

        foreach (var edit in cycle.Edits.OrderBy(e => e.DayNumber))
        {
            if (edit.DayNumber <= 1)
                continue;

            var value = GetEditValue(edit, kind);
            if (value.HasValue)
                points.Add((edit.DayNumber, value.Value));
        }

        return points;
    }

    private static bool TryEstimateFromSamples(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples,
        out decimal? rate,
        out List<(decimal X, decimal Percent)> points)
    {
        rate = null;
        points = CollectSamplePoints(cycle, kind, samples);
        if (points.Count < 2)
            return false;

        rate = MedianOfPairwiseSlopes(points);
        return rate.HasValue;
    }

    private static List<(decimal X, decimal Percent)> CollectSamplePoints(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        var points = new List<(decimal X, decimal Percent)>();
        if (samples == null || samples.Count == 0)
            return points;

        var lastByDate = new Dictionary<DateTime, UsageSample>();
        foreach (var sample in samples)
        {
            var date = sample.TimestampUtc.LocalDateTime.Date;
            if (!lastByDate.TryGetValue(date, out var existing)
                || sample.TimestampUtc > existing.TimestampUtc)
            {
                lastByDate[date] = sample;
            }
        }

        if (!lastByDate.ContainsKey(cycle.CycleStart.Date))
            points.Add((0m, 0m));

        foreach (var sample in lastByDate.Values)
            points.Add((SampleOffsetDays(cycle, sample), sample.GetPercent(kind)));

        points.Sort((left, right) => left.X.CompareTo(right.X));
        return points;
    }

    private static decimal SampleOffsetDays(QuotaCycle cycle, UsageSample sample)
    {
        var local = sample.TimestampUtc.LocalDateTime;
        return (decimal)(local - cycle.CycleStart).TotalDays;
    }

    private static int DayNumberFromOffset(QuotaCycle cycle, decimal offsetDays)
    {
        var local = cycle.CycleStart.AddDays((double)offsetDays);
        return (local.Date - cycle.CycleStart.Date).Days + 1;
    }

    private static UsageSample? FindLastSampleForDate(IReadOnlyList<UsageSample>? samples, DateTime dayDate)
    {
        if (samples == null || samples.Count == 0)
            return null;

        UsageSample? last = null;
        var date = dayDate.Date;
        foreach (var sample in samples)
        {
            if (sample.TimestampUtc.LocalDateTime.Date != date)
                continue;
            if (last == null || sample.TimestampUtc > last.TimestampUtc)
                last = sample;
        }

        return last;
    }

    private static decimal? MedianOfPairwiseSlopes(List<(decimal X, decimal Percent)> points)
    {
        if (points.Count < 2)
            return null;

        var slopes = new List<decimal>();
        for (var i = 0; i < points.Count; i++)
        {
            for (var j = i + 1; j < points.Count; j++)
            {
                var span = points[j].X - points[i].X;
                if (span <= 0)
                    continue;
                slopes.Add((points[j].Percent - points[i].Percent) / span);
            }
        }

        if (slopes.Count == 0)
            return null;

        return Median(slopes);
    }

    private static decimal Median(List<decimal> values)
    {
        values.Sort();
        var count = values.Count;
        if (count % 2 == 1)
            return values[count / 2];

        return (values[count / 2 - 1] + values[count / 2]) / 2m;
    }
}
