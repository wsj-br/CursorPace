using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

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
        var totalDays = calculator.TotalDays(cycle);
        var renewalX = ToAxisX(cycle, cycle.NextRenewal);
        var slotEndX = totalDays + 1m;
        var plotEndX = renewalX;

        var cursorExpected = BuildExpected(cycle, calculator, QuotaKind.CursorModels, totalDays, renewalX, samples);
        var otherExpected = BuildExpected(cycle, calculator, QuotaKind.OtherModels, totalDays, renewalX, samples);
        var cursorEstimated = BuildEstimated(cycle, calculator, QuotaKind.CursorModels, totalDays, samples);
        var otherEstimated = BuildEstimated(cycle, calculator, QuotaKind.OtherModels, totalDays, samples);

        var dayTicks = new List<UsageChartAxisTick>(totalDays);
        for (var day = 1; day <= totalDays; day++)
        {
            dayTicks.Add(new UsageChartAxisTick
            {
                DayNumber = day,
                Date = cycle.CycleStart.Date.AddDays(day - 1),
                X = day
            });
        }

        var yMax = ComputeYMax(cursorExpected, otherExpected, cursorEstimated, otherEstimated);

        return new UsageChartDocument
        {
            CursorExpected = cursorExpected,
            OtherExpected = otherExpected,
            CursorEstimated = cursorEstimated,
            OtherEstimated = otherEstimated,
            Markers = BuildMarkers(cycle, samples),
            DayTicks = dayTicks,
            PlotEndX = plotEndX,
            SlotEndX = slotEndX,
            RenewalX = renewalX,
            CycleStart = cycle.CycleStart,
            NextRenewal = cycle.NextRenewal,
            YMax = yMax,
            UsageLimitPercent = UsageLimitPercent
        };
    }

    public static decimal ToAxisX(DateTime cycleStart, DateTime local) =>
        CycleCalculator.AxisX(cycleStart, local);

    public static decimal ToAxisX(QuotaCycle cycle, DateTime local) =>
        CycleCalculator.AxisX(cycle, local);

    private static List<UsageChartPoint> BuildExpected(
        QuotaCycle cycle,
        ICycleCalculator calculator,
        QuotaKind kind,
        int totalDays,
        decimal renewalX,
        IReadOnlyList<UsageSample>? samples)
    {
        var startX = ToAxisX(cycle, cycle.CycleStart);
        var points = new List<UsageChartPoint>(totalDays + 2)
        {
            new()
            {
                X = startX,
                Y = calculator.ExpectedPercent(cycle, kind, 1, samples)
            }
        };

        for (var day = 1; day <= totalDays; day++)
        {
            if (day <= startX)
                continue;

            points.Add(new UsageChartPoint
            {
                X = day,
                Y = calculator.ExpectedPercent(cycle, kind, day, samples)
            });
        }

        points.Add(new UsageChartPoint { X = renewalX, Y = 100m });
        return points;
    }

    private static List<UsageChartPoint> BuildEstimated(
        QuotaCycle cycle,
        ICycleCalculator calculator,
        QuotaKind kind,
        int totalDays,
        IReadOnlyList<UsageSample>? samples)
    {
        if (calculator.ProjectedPercent(cycle, kind, 1, samples) is null)
            return [];

        if (!calculator.TryGetLastUpdate(cycle, kind, samples, out var instant, out var percent))
            return [];

        var startX = ToAxisX(cycle, instant);
        var points = new List<UsageChartPoint>
        {
            new() { X = startX, Y = percent }
        };

        for (var day = 1; day <= totalDays; day++)
        {
            var midnight = cycle.CycleStart.Date.AddDays(day - 1);
            if (midnight <= instant)
                continue;

            var value = calculator.ProjectedPercentAt(cycle, kind, midnight, samples);
            if (value is null)
                return [];

            points.Add(new UsageChartPoint
            {
                X = ToAxisX(cycle, midnight),
                Y = value.Value
            });
        }

        var endY = calculator.ProjectedPercentAt(cycle, kind, cycle.NextRenewal, samples);
        if (endY is null)
            return [];

        points.Add(new UsageChartPoint
        {
            X = ToAxisX(cycle, cycle.NextRenewal),
            Y = endY.Value
        });
        return points;
    }

    private static List<UsageChartMarker> BuildMarkers(QuotaCycle cycle, IReadOnlyList<UsageSample>? samples)
    {
        var markers = new List<UsageChartMarker>
        {
            new()
            {
                MarkerKind = ChartMarkerKind.Origin,
                QuotaKind = null,
                X = ToAxisX(cycle, cycle.CycleStart),
                Y = 0m,
                Instant = cycle.CycleStart
            }
        };

        if (samples != null)
        {
            foreach (var sample in samples)
            {
                var local = sample.TimestampUtc.LocalDateTime;
                if (local < cycle.CycleStart || local >= cycle.NextRenewal)
                    continue;

                var x = ToAxisX(cycle, local);
                AddKindMarker(markers, ChartMarkerKind.Sample, QuotaKind.CursorModels, x, sample.CursorModelsPercent, local);
                AddKindMarker(markers, ChartMarkerKind.Sample, QuotaKind.OtherModels, x, sample.OtherModelsPercent, local);
            }
        }

        foreach (var edit in cycle.Edits)
        {
            if (!edit.HasAnyValue)
                continue;

            var instant = cycle.CycleStart.Date.AddDays(edit.DayNumber - 1);
            if (edit.CursorModelsPercent is decimal cursor)
                AddKindMarker(markers, ChartMarkerKind.Edit, QuotaKind.CursorModels, edit.DayNumber, cursor, instant);
            if (edit.OtherModelsPercent is decimal other)
                AddKindMarker(markers, ChartMarkerKind.Edit, QuotaKind.OtherModels, edit.DayNumber, other, instant);
        }

        return markers;
    }

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
