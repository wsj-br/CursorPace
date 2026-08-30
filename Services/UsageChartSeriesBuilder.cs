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
        var cursorExpected = BuildExpected(cycle, QuotaKind.CursorModels, samples);
        var otherExpected = BuildExpected(cycle, QuotaKind.OtherModels, samples);
        var cursorEstimated = BuildEstimated(cycle, calculator, QuotaKind.CursorModels, samples);
        var otherEstimated = BuildEstimated(cycle, calculator, QuotaKind.OtherModels, samples);
        var yMax = ComputeYMax(cursorExpected, otherExpected, cursorEstimated, otherEstimated);

        return new UsageChartDocument
        {
            CursorExpected = cursorExpected,
            OtherExpected = otherExpected,
            CursorEstimated = cursorEstimated,
            OtherEstimated = otherEstimated,
            Markers = BuildMarkers(cycle, samples),
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

    private static List<UsageChartPoint> BuildExpected(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples)
    {
        var points = new List<UsageChartPoint>
        {
            new() { X = 0m, Y = 0m }
        };

        if (samples != null)
        {
            foreach (var sample in samples.OrderBy(s => s.TimestampUtc))
            {
                var local = sample.TimestampUtc.LocalDateTime;
                if (local < cycle.CycleStart || local >= cycle.NextRenewal)
                    continue;

                var x = CycleCalculator.AxisSeconds(cycle, local);
                if (x <= 0m)
                    continue;

                points.Add(new UsageChartPoint { X = x, Y = sample.GetPercent(kind) });
            }
        }

        points.Add(new UsageChartPoint
        {
            X = CycleCalculator.CycleSeconds(cycle),
            Y = 100m
        });
        return points;
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

    private static List<UsageChartMarker> BuildMarkers(QuotaCycle cycle, IReadOnlyList<UsageSample>? samples)
    {
        var markers = new List<UsageChartMarker>
        {
            new()
            {
                MarkerKind = ChartMarkerKind.Origin,
                QuotaKind = null,
                X = 0m,
                Y = 0m,
                Instant = cycle.CycleStart
            }
        };

        if (samples == null)
            return markers;

        foreach (var sample in samples)
        {
            var local = sample.TimestampUtc.LocalDateTime;
            if (local < cycle.CycleStart || local >= cycle.NextRenewal)
                continue;

            var x = CycleCalculator.AxisSeconds(cycle, local);
            AddKindMarker(markers, ChartMarkerKind.Sample, QuotaKind.CursorModels, x, sample.CursorModelsPercent, local);
            AddKindMarker(markers, ChartMarkerKind.Sample, QuotaKind.OtherModels, x, sample.OtherModelsPercent, local);
        }

        return markers;
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

    private static void AddKindMarker(
        List<UsageChartMarker> markers,
        ChartMarkerKind markerKind,
        QuotaKind kind,
        decimal x,
        decimal y,
        DateTime instant)
    {
        markers.Add(new UsageChartMarker
        {
            MarkerKind = markerKind,
            QuotaKind = kind,
            X = x,
            Y = y,
            Instant = instant
        });
    }

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
