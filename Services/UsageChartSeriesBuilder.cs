using System.Globalization;
using CursorPace.Models;

namespace CursorPace.Services;

public sealed class UsageChartSeriesBuilder
{
    public const decimal YTickStep = UsageChartMath.YTickStep;
    public const decimal UsageLimitPercent = 100m;

    public UsageChartDocument Build(
        QuotaCycle cycle,
        ICycleCalculator calculator,
        IReadOnlyList<UsageSample>? samples,
        UsageChartRange range = UsageChartRange.OneMonth,
        DateTime? now = null,
        bool isLiveCycle = true,
        UsageChartViewport? viewport = null,
        int rawSampleMaxDays = SampleDetail.DefaultDays)
    {
        var cycleSeconds = CycleCalculator.CycleSeconds(cycle);
        var (presetStart, presetEnd) = UsageChartMath.VisibleBounds(
            cycle,
            range,
            now ?? cycle.NextRenewal,
            isLiveCycle && now.HasValue);
        var presetStartX = ToAxisX(cycle, presetStart);
        var presetEndX = ToAxisX(cycle, presetEnd);
        if (presetEndX <= presetStartX)
            presetEndX = presetStartX + 1m;

        var zoom = UsageChartMath.ClampViewport(viewport, 0m, cycleSeconds);
        var isCustom = zoom.HasValue;
        var visibleStartX = zoom?.StartX ?? presetStartX;
        var visibleEndX = zoom?.EndX ?? presetEndX;
        if (visibleEndX <= visibleStartX)
            visibleEndX = visibleStartX + 1m;
        var visibleStart = isCustom
            ? cycle.CycleStart.AddTicks(UsageChartMath.SecondsToTicks(visibleStartX))
            : presetStart;
        var visibleEnd = isCustom
            ? cycle.CycleStart.AddTicks(UsageChartMath.SecondsToTicks(visibleEndX))
            : presetEnd;
        var rawSampleMaxSeconds = SampleDetail.MaxSeconds(rawSampleMaxDays);
        var rawSamples = isCustom
            ? UsageChartMath.UsesRawSamples(visibleEndX - visibleStartX, rawSampleMaxSeconds)
            : UsageChartMath.UsesRawSamples(range, rawSampleMaxSeconds);

        var expectedUsage = UsageChartMath.ClipToViewport(BuildExpected(cycle), visibleStartX, visibleEndX);
        var cursorSource = BuildUsage(cycle, QuotaKind.CursorModels, samples, rawSamples);
        var otherSource = BuildUsage(cycle, QuotaKind.OtherModels, samples, rawSamples);
        var cursorUsage = UsageChartMath.ClipToViewport(cursorSource, visibleStartX, visibleEndX);
        var otherUsage = UsageChartMath.ClipToViewport(otherSource, visibleStartX, visibleEndX);
        var cursorEstimated = UsageChartMath.ClipToViewport(
            BuildEstimated(cycle, calculator, QuotaKind.CursorModels, samples),
            visibleStartX,
            visibleEndX);
        var otherEstimated = UsageChartMath.ClipToViewport(
            BuildEstimated(cycle, calculator, QuotaKind.OtherModels, samples),
            visibleStartX,
            visibleEndX);
        var (yMin, yMax) = UsageChartMath.YBounds(
            expectedUsage.Select(point => point.Y)
                .Concat(cursorUsage.Select(point => point.Y))
                .Concat(otherUsage.Select(point => point.Y))
                .Concat(cursorEstimated.Select(point => point.Y))
                .Concat(otherEstimated.Select(point => point.Y)));

        return new UsageChartDocument
        {
            ExpectedUsage = expectedUsage,
            CursorUsage = cursorUsage,
            OtherUsage = otherUsage,
            CursorEstimated = cursorEstimated,
            OtherEstimated = otherEstimated,
            Slots = BuildVisibleSlots(cycle, rawSamples, visibleStart, visibleEnd, visibleStartX, visibleEndX, cycleSeconds),
            CycleSeconds = cycleSeconds,
            CycleStart = cycle.CycleStart,
            NextRenewal = cycle.NextRenewal,
            VisibleStartX = visibleStartX,
            VisibleEndX = visibleEndX,
            VisibleStart = visibleStart,
            VisibleEnd = visibleEnd,
            Range = range,
            IsCustomViewport = isCustom,
            UsesIntradayAxis = rawSamples,
            LastMeasuredCursor = LastMeasured(cursorSource, visibleStartX, visibleEndX),
            LastMeasuredOther = LastMeasured(otherSource, visibleStartX, visibleEndX),
            YMin = yMin,
            YMax = yMax,
            YTickStep = YTickStep,
            UsageLimitPercent = UsageLimitPercent
        };
    }

    public static decimal ToAxisX(QuotaCycle cycle, DateTime local) =>
        CycleCalculator.AxisSeconds(cycle, local);

    public static decimal LinearExpectedPercent(decimal cycleSeconds, decimal elapsedSeconds)
    {
        if (cycleSeconds <= 0 || elapsedSeconds <= 0)
            return 0m;
        if (elapsedSeconds >= cycleSeconds)
            return 100m;
        return 100m * elapsedSeconds / cycleSeconds;
    }

    public static string FormatEndpointPercent(decimal percent) =>
        percent.ToString("0.0", CultureInfo.CurrentCulture) + "%";

    public static string FormatSignedEndpointPercent(decimal percent) =>
        percent.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "%";

    private static List<UsageChartPoint> BuildExpected(QuotaCycle cycle) =>
    [
        new() { X = 0m, Y = 0m },
        new() { X = CycleCalculator.CycleSeconds(cycle), Y = 100m }
    ];

    private static List<UsageChartPoint> BuildUsage(
        QuotaCycle cycle,
        QuotaKind kind,
        IReadOnlyList<UsageSample>? samples,
        bool rawSamples)
    {
        if (samples == null || samples.Count == 0)
            return [];

        if (rawSamples)
        {
            return samples
                .Select(sample => (Sample: sample, Local: sample.TimestampUtc.LocalDateTime))
                .Where(item => item.Local >= cycle.CycleStart && item.Local < cycle.NextRenewal)
                .OrderBy(item => item.Sample.TimestampUtc)
                .Select(item => new UsageChartPoint
                {
                    X = CycleCalculator.AxisSeconds(cycle, item.Local),
                    Y = item.Sample.GetPercent(kind),
                    IsMeasured = true
                })
                .ToList();
        }

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
            .OrderBy(sample => sample.TimestampUtc)
            .Select(sample => new UsageChartPoint
            {
                X = CycleCalculator.AxisSeconds(cycle, sample.TimestampUtc.LocalDateTime),
                Y = sample.GetPercent(kind),
                IsMeasured = true
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

    private static UsageChartPoint? LastMeasured(
        IReadOnlyList<UsageChartPoint> source,
        decimal visibleStartX,
        decimal visibleEndX)
    {
        UsageChartPoint? last = null;
        foreach (var point in source)
        {
            if (point.X < visibleStartX || point.X > visibleEndX)
                continue;
            if (last == null || point.X >= last.X)
                last = point;
        }

        return last;
    }

    private static List<UsageChartSlot> BuildVisibleSlots(
        QuotaCycle cycle,
        bool rawSamples,
        DateTime visibleStart,
        DateTime visibleEnd,
        decimal visibleStartX,
        decimal visibleEndX,
        decimal cycleSeconds)
    {
        if (rawSamples)
            return BuildHourSlots(cycle, visibleStart, visibleEnd);

        var slots = BuildSlots(cycle);
        if (visibleStart == cycle.CycleStart && visibleEnd == cycle.NextRenewal && visibleEndX == cycleSeconds)
            return slots;

        return ClipSlots(slots, visibleStartX, visibleEndX);
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
            slots.Add(DaySlot(previous, previousX, x));
            previous = midnight;
            previousX = x;
        }

        slots.Add(DaySlot(previous, previousX, cycleSeconds));
        return slots;
    }

    private static List<UsageChartSlot> ClipSlots(
        IReadOnlyList<UsageChartSlot> slots,
        decimal xMin,
        decimal xMax)
    {
        var clipped = new List<UsageChartSlot>();
        foreach (var slot in slots)
        {
            if (slot.EndX <= xMin || slot.StartX >= xMax)
                continue;

            var startX = slot.StartX < xMin ? xMin : slot.StartX;
            var endX = slot.EndX > xMax ? xMax : slot.EndX;
            if (endX <= startX)
                continue;

            clipped.Add(new UsageChartSlot
            {
                Date = slot.Date,
                StartX = startX,
                EndX = endX,
                IsLeadingPartial = slot.IsLeadingPartial || startX > slot.StartX
            });
        }

        return clipped;
    }

    private static List<UsageChartSlot> BuildHourSlots(
        QuotaCycle cycle,
        DateTime visibleStart,
        DateTime visibleEnd)
    {
        var slots = new List<UsageChartSlot>();
        var previous = visibleStart;
        var previousX = CycleCalculator.AxisSeconds(cycle, visibleStart);
        var cursor = new DateTime(
            visibleStart.Year,
            visibleStart.Month,
            visibleStart.Day,
            visibleStart.Hour,
            0,
            0,
            visibleStart.Kind);
        if (cursor <= visibleStart)
            cursor = cursor.AddHours(1);

        while (cursor < visibleEnd)
        {
            var x = CycleCalculator.AxisSeconds(cycle, cursor);
            slots.Add(HourSlot(previous, previousX, x));
            previous = cursor;
            previousX = x;
            cursor = cursor.AddHours(1);
        }

        slots.Add(HourSlot(previous, previousX, CycleCalculator.AxisSeconds(cycle, visibleEnd)));
        return slots;
    }

    private static UsageChartSlot DaySlot(DateTime start, decimal startX, decimal endX) =>
        new()
        {
            Date = start.Date,
            StartX = startX,
            EndX = endX,
            IsLeadingPartial = start != start.Date
        };

    private static UsageChartSlot HourSlot(DateTime start, decimal startX, decimal endX) =>
        new()
        {
            Date = start,
            StartX = startX,
            EndX = endX,
            IsLeadingPartial = start.Ticks % TimeSpan.TicksPerHour != 0
        };
}
