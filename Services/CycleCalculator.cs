using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public sealed class CycleCalculator : ICycleCalculator
{
    private const decimal SecondsPerDay = 86400m;

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

    public int TotalDays(QuotaCycle cycle)
    {
        var lastDate = LastInclusiveCalendarDate(cycle);
        var totalDays = (lastDate - cycle.CycleStart.Date).Days + 1;
        if (totalDays <= 0)
            throw new InvalidOperationException("Cycle has no days.");
        return totalDays;
    }

    /// <summary>
    /// Last local date that still has time in <c>[CycleStart, NextRenewal)</c>.
    /// When renewal is exactly midnight that date is excluded.
    /// </summary>
    public static DateTime LastInclusiveCalendarDate(QuotaCycle cycle) =>
        cycle.NextRenewal > cycle.NextRenewal.Date
            ? cycle.NextRenewal.Date
            : cycle.NextRenewal.Date.AddDays(-1);

    // Wall-clock elapsed time: DST can shift a cycle by one hour, which is intentional.
    public static decimal CycleSeconds(QuotaCycle cycle) =>
        TicksToSeconds((cycle.NextRenewal - cycle.CycleStart).Ticks);

    public static decimal AxisSeconds(QuotaCycle cycle, DateTime local) =>
        TicksToSeconds((local - cycle.CycleStart).Ticks);

    public decimal ExpectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber) =>
        ExpectedPercent(cycle, kind, dayNumber, samples: null);

    public decimal ExpectedPercent(
        QuotaCycle cycle,
        QuotaKind kind,
        int dayNumber,
        IReadOnlyList<UsageSample>? samples)
    {
        return ExpectedPercentAt(cycle, kind, InstantForDay(cycle, dayNumber), samples);
    }

    public decimal ExpectedPercentAt(
        QuotaCycle cycle,
        QuotaKind kind,
        DateTime local,
        IReadOnlyList<UsageSample>? samples)
    {
        var anchors = CollectExpectedAnchors(cycle, kind, samples);
        if (local <= cycle.CycleStart)
            return anchors[0].Percent;
        if (local >= cycle.NextRenewal)
            return 100m;

        return Interpolate(anchors, AxisSeconds(cycle, local));
    }

    public decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind) =>
        EstimateDailyUsage(cycle, kind, samples: null);

    public decimal? EstimateDailyUsage(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples)
    {
        if (!TryEstimateRate(cycle, kind, samples, out var ratePerSecond, out _))
            return null;

        return ratePerSecond * SecondsPerDay;
    }

    public decimal? ProjectedPercent(QuotaCycle cycle, QuotaKind kind, int dayNumber) =>
        ProjectedPercent(cycle, kind, dayNumber, samples: null);

    public decimal? ProjectedPercent(
        QuotaCycle cycle,
        QuotaKind kind,
        int dayNumber,
        IReadOnlyList<UsageSample>? samples) =>
        ProjectedPercentAt(cycle, kind, InstantForDay(cycle, dayNumber), samples);

    public decimal? ProjectedPercentAt(
        QuotaCycle cycle,
        QuotaKind kind,
        DateTime local,
        IReadOnlyList<UsageSample>? samples)
    {
        if (!TryEstimateRate(cycle, kind, samples, out var ratePerSecond, out var points)
            || points.Count == 0)
        {
            return null;
        }

        var last = points[^1];
        var targetX = AxisSeconds(cycle, local);
        return last.Percent + ratePerSecond * (targetX - last.X);
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

        foreach (var sample in EnumerateInCycle(cycle, samples))
        {
            var local = sample.TimestampUtc.LocalDateTime;
            if (best is null || local > best)
            {
                best = local;
                bestPercent = sample.GetPercent(kind);
            }
        }

        if (best is null)
            return false;

        instant = best.Value;
        percent = bestPercent;
        return true;
    }

    public DateTime? EstimateRunOutInstant(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        if (!TryEstimateRate(cycle, kind, samples, out var ratePerSecond, out var points)
            || points.Count == 0)
        {
            return null;
        }

        var last = points[^1];
        var lastInstant = cycle.CycleStart.AddTicks(SecondsToTicks(last.X));
        if (last.Percent >= 100m)
            return lastInstant;

        if (ratePerSecond <= 0m)
            return null;

        var delta = (100m - last.Percent) / ratePerSecond;
        var runOut = cycle.CycleStart.AddTicks(SecondsToTicks(last.X + delta));
        if (runOut <= lastInstant || runOut >= cycle.NextRenewal)
            return null;

        return runOut;
    }

    public int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind) =>
        EstimateRunOutDayNumber(cycle, kind, samples: null);

    public int? EstimateRunOutDayNumber(QuotaCycle cycle, QuotaKind kind, IReadOnlyList<UsageSample>? samples)
    {
        var instant = EstimateRunOutInstant(cycle, kind, samples);
        if (instant is null)
            return null;

        var dayNumber = (instant.Value.Date - cycle.CycleStart.Date).Days + 1;
        var totalDays = TotalDays(cycle);
        if (dayNumber < 1 || dayNumber > totalDays)
            return null;

        return dayNumber;
    }

    public void RebuildDays(QuotaCycle cycle) =>
        RebuildDays(cycle, samples: null, today: null);

    public void RebuildDays(QuotaCycle cycle, IReadOnlyList<UsageSample>? samples, DateTime? today)
    {
        var totalDays = TotalDays(cycle);
        var days = new List<QuotaDayEntry>(totalDays);

        for (var k = 0; k < totalDays; k++)
        {
            var dayNumber = k + 1;
            var date = cycle.CycleStart.Date.AddDays(k);
            var cursorExpected = ExpectedPercent(cycle, QuotaKind.CursorModels, dayNumber, samples);
            var otherExpected = ExpectedPercent(cycle, QuotaKind.OtherModels, dayNumber, samples);
            var sample = today.HasValue && date > today.Value.Date
                ? null
                : FindLastSampleForDate(cycle, samples, date);

            days.Add(new QuotaDayEntry
            {
                DayNumber = dayNumber,
                Date = date,
                CursorModelsPercent = sample?.CursorModelsPercent ?? cursorExpected,
                OtherModelsPercent = sample?.OtherModelsPercent ?? otherExpected,
                CursorModelsIsActual = sample != null,
                OtherModelsIsActual = sample != null
            });
        }

        cycle.Days = days;
    }

    private DateTime InstantForDay(QuotaCycle cycle, int dayNumber)
    {
        var totalDays = TotalDays(cycle);
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));

        var midnight = cycle.CycleStart.Date.AddDays(dayNumber - 1);
        return midnight < cycle.CycleStart ? cycle.CycleStart : midnight;
    }

    private static List<(decimal X, decimal Percent)> CollectExpectedAnchors(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        var anchors = new List<(decimal X, decimal Percent)> { (0m, 0m) };
        foreach (var sample in EnumerateInCycle(cycle, samples).OrderBy(s => s.TimestampUtc))
        {
            var x = AxisSeconds(cycle, sample.TimestampUtc.LocalDateTime);
            if (x <= 0m)
                continue;

            anchors.Add((x, sample.GetPercent(kind)));
        }

        var endX = CycleSeconds(cycle);
        if (anchors[^1].X < endX)
            anchors.Add((endX, 100m));

        return anchors;
    }

    private static bool TryEstimateRate(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples,
        out decimal ratePerSecond,
        out List<(decimal X, decimal Percent)> points)
    {
        ratePerSecond = 0m;
        points = CollectEstimatePoints(cycle, kind, samples);
        if (points.Count < 2)
            return false;

        var rate = MedianOfPairwiseSlopes(points);
        if (rate is null)
            return false;

        ratePerSecond = rate.Value;
        return true;
    }

    private static List<(decimal X, decimal Percent)> CollectEstimatePoints(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        var points = new List<(decimal X, decimal Percent)>();
        var lastByDate = new Dictionary<DateTime, UsageSample>();
        foreach (var sample in EnumerateInCycle(cycle, samples))
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
            points.Add((AxisSeconds(cycle, sample.TimestampUtc.LocalDateTime), sample.GetPercent(kind)));

        points.Sort((left, right) => left.X.CompareTo(right.X));
        return points;
    }

    private static IEnumerable<UsageSample> EnumerateInCycle(
        QuotaCycle cycle,
        IReadOnlyList<UsageSample>? samples)
    {
        if (samples == null || samples.Count == 0)
            yield break;

        foreach (var sample in samples)
        {
            var local = sample.TimestampUtc.LocalDateTime;
            if (local < cycle.CycleStart || local >= cycle.NextRenewal)
                continue;

            yield return sample;
        }
    }

    private static UsageSample? FindLastSampleForDate(
        QuotaCycle cycle,
        IReadOnlyList<UsageSample>? samples,
        DateTime dayDate)
    {
        UsageSample? last = null;
        var date = dayDate.Date;
        foreach (var sample in EnumerateInCycle(cycle, samples))
        {
            if (sample.TimestampUtc.LocalDateTime.Date != date)
                continue;
            if (last == null || sample.TimestampUtc > last.TimestampUtc)
                last = sample;
        }

        return last;
    }

    private static decimal Interpolate(List<(decimal X, decimal Percent)> anchors, decimal x)
    {
        for (var i = 1; i < anchors.Count; i++)
        {
            var right = anchors[i];
            if (x > right.X)
                continue;

            var left = anchors[i - 1];
            var span = right.X - left.X;
            if (span <= 0)
                return right.Percent;

            return left.Percent + (x - left.X) * (right.Percent - left.Percent) / span;
        }

        return anchors[^1].Percent;
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

    private static decimal TicksToSeconds(long ticks) =>
        (decimal)ticks / TimeSpan.TicksPerSecond;

    private static long SecondsToTicks(decimal seconds) =>
        (long)decimal.Round(seconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero);
}
