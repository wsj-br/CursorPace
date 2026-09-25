using CursorPace.Models;

namespace CursorPace.Services;

public static class UsageChartMath
{
    public const decimal YTickStep = 10m;

    public static bool UsesRawSamples(UsageChartRange range, decimal maxSeconds) =>
        range switch
        {
            UsageChartRange.OneMonth => false,
            UsageChartRange.OneDay or UsageChartRange.TwoDays or UsageChartRange.SevenDays
                or UsageChartRange.OneWeek or UsageChartRange.TwoWeeks =>
                (decimal)Duration(range).TotalSeconds <= maxSeconds,
            _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
        };

    public static bool UsesRawSamples(decimal elapsedSeconds, decimal maxSeconds) =>
        elapsedSeconds <= maxSeconds;

    public static readonly int[] IntradayLabelSteps = [1, 2, 3, 4, 6, 8, 12, 24];

    public static int IntradayLabelStep(int slotCount, double plotWidth, double minSpacing)
    {
        if (slotCount <= 1 || plotWidth <= 0 || minSpacing <= 0)
            return 1;

        var slotWidth = plotWidth / slotCount;
        foreach (var step in IntradayLabelSteps)
        {
            if (step * slotWidth >= minSpacing)
                return step;
        }

        return IntradayLabelSteps[^1];
    }

    public static List<int> IntradayLabelIndexes(int slotCount, int step)
    {
        if (slotCount <= 0)
            return [];

        if (step < 1)
            step = 1;

        var indexes = new List<int>();
        var last = slotCount - 1;
        for (var i = 0; i < slotCount; i += step)
            indexes.Add(i);
        if (indexes[^1] != last)
            indexes.Add(last);
        return indexes;
    }

    public static string Label(UsageChartRange range) =>
        range switch
        {
            UsageChartRange.OneDay => "1D",
            UsageChartRange.TwoDays => "2D",
            UsageChartRange.SevenDays => "7D",
            UsageChartRange.OneWeek => "1W",
            UsageChartRange.TwoWeeks => "2W",
            UsageChartRange.OneMonth => "1M",
            _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
        };

    public static (DateTime Start, DateTime End) VisibleBounds(
        QuotaCycle cycle,
        UsageChartRange range,
        DateTime now,
        bool isLiveCycle)
    {
        if (range == UsageChartRange.OneMonth)
            return (cycle.CycleStart, cycle.NextRenewal);

        var duration = Duration(range);
        var end = isLiveCycle
            ? Clamp(now, cycle.CycleStart, cycle.NextRenewal)
            : cycle.NextRenewal;
        var start = end - duration;
        if (start < cycle.CycleStart)
            start = cycle.CycleStart;
        if (start > end)
            start = end;
        return (start, end);
    }

    public static (decimal Min, decimal Max) YBounds(IEnumerable<decimal> values)
    {
        var any = false;
        var min = 0m;
        var max = 0m;
        foreach (var value in values)
        {
            if (!any)
            {
                min = value;
                max = value;
                any = true;
                continue;
            }

            if (value < min)
                min = value;
            if (value > max)
                max = value;
        }

        if (!any)
        {
            min = 0m;
            max = 100m;
        }

        var yMin = decimal.Floor(min / YTickStep) * YTickStep;
        var yMax = decimal.Ceiling(max / YTickStep) * YTickStep;
        if (yMax <= yMin)
            yMax = yMin + YTickStep;
        return (yMin, yMax);
    }

    public static List<UsageChartPoint> ClipToViewport(
        IReadOnlyList<UsageChartPoint> points,
        decimal xMin,
        decimal xMax)
    {
        if (points.Count == 0 || xMax < xMin)
            return [];

        var sorted = points.OrderBy(point => point.X).ToList();
        if (sorted.Count == 1)
        {
            return sorted[0].X >= xMin && sorted[0].X <= xMax
                ? [sorted[0]]
                : [];
        }

        var result = new List<UsageChartPoint>();
        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var left = sorted[i];
            var right = sorted[i + 1];
            if (right.X < xMin || left.X > xMax)
                continue;

            if (left.X >= xMin && left.X <= xMax)
                AddPoint(result, left);
            else if (left.X < xMin && right.X >= xMin)
                AddPoint(result, new UsageChartPoint { X = xMin, Y = Lerp(left, right, xMin) });

            if (right.X >= xMin && right.X <= xMax)
                AddPoint(result, right);
            else if (right.X > xMax && left.X <= xMax)
                AddPoint(result, new UsageChartPoint { X = xMax, Y = Lerp(left, right, xMax) });
        }

        return result;
    }

    public static decimal? InterpolateY(IReadOnlyList<UsageChartPoint> points, decimal x)
    {
        if (points.Count == 0 || x < points[0].X || x > points[^1].X)
            return null;

        for (var i = 1; i < points.Count; i++)
        {
            var right = points[i];
            if (x > right.X)
                continue;

            return Lerp(points[i - 1], right, x);
        }

        return points[^1].Y;
    }

    public static UsageChartViewport? ClampViewport(
        UsageChartViewport? viewport,
        decimal minX,
        decimal maxX)
    {
        if (viewport is not { } value || maxX <= minX)
            return null;

        var start = value.StartX < value.EndX ? value.StartX : value.EndX;
        var end = value.StartX < value.EndX ? value.EndX : value.StartX;
        if (start < minX)
            start = minX;
        if (end > maxX)
            end = maxX;
        return end > start ? new UsageChartViewport(start, end) : null;
    }

    public static UsageChartViewport? ViewportFromDrag(
        decimal originX,
        decimal currentX,
        decimal minX,
        decimal maxX,
        double pixelSpan,
        double minimumPixelSpan)
    {
        if (pixelSpan < minimumPixelSpan)
            return null;

        return ClampViewport(new UsageChartViewport(originX, currentX), minX, maxX);
    }

    public static UsageChartHoverReadout Read(UsageChartDocument document, decimal x)
    {
        var clamped = x < document.VisibleStartX
            ? document.VisibleStartX
            : x > document.VisibleEndX
                ? document.VisibleEndX
                : x;
        var cursor = InterpolateY(document.CursorUsage, clamped);
        var other = InterpolateY(document.OtherUsage, clamped);
        var expected = ExpectedAt(document, clamped);
        var cursorStart = InterpolateY(document.CursorUsage, document.VisibleStartX);
        var otherStart = InterpolateY(document.OtherUsage, document.VisibleStartX);
        var expectedStart = ExpectedAt(document, document.VisibleStartX);
        return new UsageChartHoverReadout(
            document.CycleStart.AddTicks(SecondsToTicks(clamped)),
            cursor,
            other,
            expected,
            cursorStart,
            otherStart,
            expectedStart,
            Delta(cursor, cursorStart),
            Delta(other, otherStart),
            expected - expectedStart);
    }

    private static decimal ExpectedAt(UsageChartDocument document, decimal x) =>
        InterpolateY(document.ExpectedUsage, x)
        ?? UsageChartSeriesBuilder.LinearExpectedPercent(document.CycleSeconds, x);

    private static decimal? Delta(decimal? current, decimal? start) =>
        current.HasValue && start.HasValue ? current.Value - start.Value : null;

    public static long SecondsToTicks(decimal seconds) =>
        (long)decimal.Round(seconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero);

    private static TimeSpan Duration(UsageChartRange range) =>
        range switch
        {
            UsageChartRange.OneDay => TimeSpan.FromDays(1),
            UsageChartRange.TwoDays => TimeSpan.FromDays(2),
            UsageChartRange.SevenDays or UsageChartRange.OneWeek => TimeSpan.FromDays(7),
            UsageChartRange.TwoWeeks => TimeSpan.FromDays(14),
            UsageChartRange.OneMonth => throw new ArgumentOutOfRangeException(nameof(range), range, null),
            _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
        };

    private static DateTime Clamp(DateTime value, DateTime min, DateTime max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private static decimal Lerp(UsageChartPoint left, UsageChartPoint right, decimal x)
    {
        var span = right.X - left.X;
        if (span == 0)
            return right.Y;
        return left.Y + (x - left.X) * (right.Y - left.Y) / span;
    }

    private static void AddPoint(List<UsageChartPoint> points, UsageChartPoint point)
    {
        if (points.Count > 0 && points[^1].X == point.X)
            return;
        points.Add(point);
    }
}

public readonly record struct UsageChartHoverReadout(
    DateTime LocalTime,
    decimal? CursorPercent,
    decimal? OtherPercent,
    decimal ExpectedPercent,
    decimal? CursorStartPercent,
    decimal? OtherStartPercent,
    decimal ExpectedStartPercent,
    decimal? CursorDelta,
    decimal? OtherDelta,
    decimal ExpectedDelta);
