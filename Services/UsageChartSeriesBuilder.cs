using CursorPace.Models;

namespace CursorPace.Services;

public sealed class UsageChartSeriesBuilder
{
    public const decimal DefaultYMax = 120m;
    public const decimal YMaxStep = 20m;
    public const decimal UsageLimitPercent = 100m;

    public UsageChartDocument Build(
        QuotaCycle cycle,
        ICycleCalculator calculator,
        IReadOnlyList<UsageSample>? samples)
    {
        var cycleSeconds = CycleCalculator.CycleSeconds(cycle);
        var expectedUsage = BuildExpected(cycle);
        var cursorUsage = BuildUsage(cycle, QuotaKind.CursorModels, samples);
        var otherUsage = BuildUsage(cycle, QuotaKind.OtherModels, samples);
        var cursorEstimated = BuildEstimated(cycle, calculator, QuotaKind.CursorModels, samples);
        var otherEstimated = BuildEstimated(cycle, calculator, QuotaKind.OtherModels, samples);
        var yMax = ComputeYMax(cursorUsage, otherUsage, cursorEstimated, otherEstimated);

        return new UsageChartDocument
        {
            ExpectedUsage = expectedUsage,
            CursorUsage = cursorUsage,
            OtherUsage = otherUsage,
            CursorEstimated = cursorEstimated,
            OtherEstimated = otherEstimated,
            Slots = BuildSlots(cycle),
            CycleSeconds = cycleSeconds,
            CycleStart = cycle.CycleStart,
            NextRenewal = cycle.NextRenewal,
            YMax = yMax,
            UsageLimitPercent = UsageLimitPercent
        };
    }

    public static decimal ToAxisX(QuotaCycle cycle, DateTime local) =>
        CycleCalculator.AxisSeconds(cycle, local);

    private static List<UsageChartPoint> BuildExpected(QuotaCycle cycle) =>
    [
        new() { X = 0m, Y = 0m },
        new() { X = CycleCalculator.CycleSeconds(cycle), Y = 100m }
    ];

    private static List<UsageChartPoint> BuildUsage(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        if (samples == null || samples.Count == 0)
            return [];

        var lastByDate = new Dictionary<DateTime, UsageSample>();
        foreach (var sample in samples)
        {
            var local = sample.TimestampUtc.LocalDateTime;
            if (local < cycle.CycleStart || local >= cycle.NextRenewal)
                continue;

            var date = local.Date;
            if (!lastByDate.TryGetValue(date, out var existing)
                || sample.TimestampUtc > existing.TimestampUtc)
            {
                lastByDate[date] = sample;
            }
        }

        if (lastByDate.Count < 2)
            return [];

        return lastByDate.Values
            .OrderBy(s => s.TimestampUtc)
            .Select(s => new UsageChartPoint
            {
                X = CycleCalculator.AxisSeconds(cycle, s.TimestampUtc.LocalDateTime),
                Y = s.GetPercent(kind)
            })
            .ToList();
    }

    private static List<UsageChartPoint> BuildEstimated(
        QuotaCycle cycle,
        ICycleCalculator calculator,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        if (!calculator.TryGetLastUpdate(cycle, kind, samples, out var instant, out var percent))
            return [];

        var endY = calculator.ProjectedPercentAt(cycle, kind, cycle.NextRenewal, samples);
        if (endY is null)
            return [];

        return
        [
            new UsageChartPoint { X = CycleCalculator.AxisSeconds(cycle, instant), Y = percent },
            new UsageChartPoint { X = CycleCalculator.CycleSeconds(cycle), Y = endY.Value }
        ];
    }

    private static List<UsageChartSlot> BuildSlots(QuotaCycle cycle)
    {
        var cycleSeconds = CycleCalculator.CycleSeconds(cycle);
        var midnights = new List<DateTime>();
        // Midnight after CycleStart.Date is always later than CycleStart.
        var cursor = cycle.CycleStart.Date.AddDays(1);

        while (cursor < cycle.NextRenewal)
        {
            midnights.Add(cursor);
            cursor = cursor.AddDays(1);
        }

        var slots = new List<UsageChartSlot>(midnights.Count + 1);
        var previous = cycle.CycleStart;
        var previousX = 0m;

        foreach (var midnight in midnights)
        {
            var x = CycleCalculator.AxisSeconds(cycle, midnight);
            slots.Add(SlotFor(previous, previousX, x));
            previous = midnight;
            previousX = x;
        }

        slots.Add(SlotFor(previous, previousX, cycleSeconds));
        return slots;
    }

    private static UsageChartSlot SlotFor(DateTime start, decimal startX, decimal endX) =>
        new()
        {
            Date = start.Date,
            StartX = startX,
            EndX = endX,
            IsLeadingPartial = start != start.Date
        };

    private static decimal ComputeYMax(params IReadOnlyList<UsageChartPoint>[] series)
    {
        var yMax = DefaultYMax;
        foreach (var points in series)
        {
            foreach (var point in points)
            {
                if (point.Y > yMax)
                    yMax = point.Y;
            }
        }

        if (yMax <= DefaultYMax)
            return DefaultYMax;

        return decimal.Ceiling(yMax / YMaxStep) * YMaxStep;
    }
}
