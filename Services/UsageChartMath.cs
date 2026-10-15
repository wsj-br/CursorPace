using CursorPace.Models;

namespace CursorPace.Services;

public static class UsageChartMath
{
    public const decimal YTickStep = 10m;
    public const decimal YMaxPercent = 120m;
    public const double HoverSnapAcquirePixels = 14;
    public const double HoverSnapReleasePixels = 20;
    private const double LabelGapEpsilon = 1e-6;

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
        if (yMax > YMaxPercent)
            yMax = YMaxPercent;
        if (yMax <= yMin)
            yMin = yMax - YTickStep;

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

    public static List<UsageChartPoint> ClipToYRange(
        IReadOnlyList<UsageChartPoint> points,
        decimal yMin,
        decimal yMax)
    {
        if (points.Count == 0 || yMax < yMin)
            return [];

        var sorted = points.OrderBy(point => point.X).ToList();
        if (sorted.Count == 1)
        {
            return InYRange(sorted[0].Y, yMin, yMax)
                ? [sorted[0]]
                : [];
        }

        var result = new List<UsageChartPoint>();
        for (var i = 0; i < sorted.Count - 1; i++)
        {
            foreach (var point in ClipSegmentY(sorted[i], sorted[i + 1], yMin, yMax))
                AddPoint(result, point);
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

    /// <summary>
    /// Spreads endpoint-label centers so later labels stay at least <paramref name="minimumGap"/> below earlier ones.
    /// <paramref name="minimumGap"/> is center spacing (label height plus padding). Results stay inside the plot, inset by half <paramref name="labelHeight"/>.
    /// </summary>
    public static double[] ResolveEndpointLabelOffsets(
        IReadOnlyList<double> anchorYs,
        double labelHeight,
        double minimumGap,
        double plotTop,
        double plotBottom)
    {
        ArgumentNullException.ThrowIfNull(anchorYs);

        var count = anchorYs.Count;
        var result = new double[count];
        if (count == 0)
            return result;

        var height = labelHeight > 0 ? labelHeight : 0;
        var gap = minimumGap > 0 ? minimumGap : 0;
        if (plotBottom < plotTop)
            (plotTop, plotBottom) = (plotBottom, plotTop);

        var half = height / 2;
        var minCenter = plotTop + half;
        var maxCenter = plotBottom - half;
        if (maxCenter < minCenter)
        {
            var mid = (plotTop + plotBottom) / 2;
            minCenter = mid;
            maxCenter = mid;
        }

        var order = new int[count];
        for (var i = 0; i < count; i++)
            order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            var compare = anchorYs[a].CompareTo(anchorYs[b]);
            return compare != 0 ? compare : a.CompareTo(b);
        });

        var sorted = new double[count];
        for (var i = 0; i < count; i++)
            sorted[i] = anchorYs[order[i]];

        var ranges = new List<(int Start, int End)>();
        var clusterStart = 0;
        for (var i = 1; i <= count; i++)
        {
            var split = i == count || sorted[i] - sorted[i - 1] >= gap - LabelGapEpsilon;
            if (!split)
                continue;

            ranges.Add((clusterStart, i - 1));
            clusterStart = i;
        }

        var placed = new double[count];
        var guard = 0;
        var merged = true;
        while (merged && guard++ <= count)
        {
            merged = false;
            foreach (var (start, end) in ranges)
                PlaceEndpointCluster(sorted, placed, start, end, gap, minCenter, maxCenter);

            for (var i = 0; i < ranges.Count - 1; i++)
            {
                var left = ranges[i];
                var right = ranges[i + 1];
                if (placed[right.Start] - placed[left.End] >= gap - LabelGapEpsilon)
                    continue;

                ranges[i] = (left.Start, right.End);
                ranges.RemoveAt(i + 1);
                merged = true;
                break;
            }
        }

        for (var i = 0; i < count; i++)
            result[order[i]] = placed[i];
        return result;
    }

    /// <summary>
    /// Places endpoint-label centers just above each anchor, or just below when the plot has no room above.
    /// Close labels stay in top-to-bottom order and keep at least <paramref name="labelHeight"/> + <paramref name="padding"/> between centers.
    /// </summary>
    public static double[] ResolveVerticalEndpointLabels(
        IReadOnlyList<double> anchorYs,
        double labelHeight,
        double padding,
        double plotTop,
        double plotBottom)
    {
        ArgumentNullException.ThrowIfNull(anchorYs);

        var count = anchorYs.Count;
        var result = new double[count];
        if (count == 0)
            return result;

        var height = labelHeight > 0 ? labelHeight : 0;
        var pad = padding > 0 ? padding : 0;
        if (plotBottom < plotTop)
            (plotTop, plotBottom) = (plotBottom, plotTop);

        var half = height / 2;
        var minCenter = plotTop + half;
        var maxCenter = plotBottom - half;
        if (maxCenter < minCenter)
        {
            var mid = (plotTop + plotBottom) / 2;
            minCenter = mid;
            maxCenter = mid;
        }

        var clearance = half + pad;
        var minimumGap = Math.Max(height + pad, clearance);
        var order = new int[count];
        for (var i = 0; i < count; i++)
            order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            var compare = anchorYs[a].CompareTo(anchorYs[b]);
            return compare != 0 ? compare : a.CompareTo(b);
        });

        var sorted = new double[count];
        for (var i = 0; i < count; i++)
            sorted[i] = anchorYs[order[i]];

        var placed = PlaceVerticalEndpointCenters(sorted, clearance, minimumGap, minCenter, maxCenter);
        for (var i = 0; i < count; i++)
            result[order[i]] = placed[i];
        return result;
    }

    /// <summary>
    /// Snaps <paramref name="pointerX"/> to a nearby measured sample. A snap stays until the pointer
    /// moves past <see cref="HoverSnapReleasePixels"/>, unless a closer sample is inside
    /// <see cref="HoverSnapAcquirePixels"/>.
    /// </summary>
    public static decimal ResolveHoverX(
        decimal pointerX,
        decimal? heldX,
        IReadOnlyList<decimal> measuredXs,
        decimal visibleSpan,
        double plotWidth)
    {
        if (measuredXs.Count == 0 || plotWidth <= 0 || visibleSpan <= 0)
            return pointerX;

        var secondsPerPixel = visibleSpan / (decimal)plotWidth;
        var acquire = secondsPerPixel * (decimal)HoverSnapAcquirePixels;
        var release = secondsPerPixel * (decimal)HoverSnapReleasePixels;
        var (nearest, nearestDistance) = NearestX(measuredXs, pointerX);
        if (heldX is decimal held && ContainsX(measuredXs, held))
        {
            var heldDistance = Abs(pointerX - held);
            var nearerAcquire = nearestDistance < heldDistance && nearestDistance <= acquire;
            if (!nearerAcquire && heldDistance <= release)
                return held;
        }

        return nearestDistance <= acquire ? nearest : pointerX;
    }

    public static UsageChartHoverReadout Read(UsageChartDocument document, decimal x)
    {
        var clamped = x < document.VisibleStartX
            ? document.VisibleStartX
            : x > document.VisibleEndX
                ? document.VisibleEndX
                : x;
        var (cursor, cursorEstimated) = ObservedOrEstimated(document.CursorUsage, document.CursorEstimated, clamped);
        var (other, otherEstimated) = ObservedOrEstimated(document.OtherUsage, document.OtherEstimated, clamped);
        var expected = ExpectedAt(document, clamped);
        var (cursorStart, cursorStartEstimated) = ObservedOrEstimated(
            document.CursorUsage,
            document.CursorEstimated,
            document.VisibleStartX);
        var (otherStart, otherStartEstimated) = ObservedOrEstimated(
            document.OtherUsage,
            document.OtherEstimated,
            document.VisibleStartX);
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
            expected - expectedStart,
            cursorEstimated,
            otherEstimated,
            cursorStartEstimated,
            otherStartEstimated);
    }

    private static (decimal? Value, bool IsEstimated) ObservedOrEstimated(
        IReadOnlyList<UsageChartPoint> observed,
        IReadOnlyList<UsageChartPoint> estimated,
        decimal x)
    {
        var measured = InterpolateY(observed, x);
        if (measured.HasValue)
            return (measured, false);

        var forecast = InterpolateY(estimated, x);
        return (forecast, forecast.HasValue);
    }

    private static (decimal X, decimal Distance) NearestX(IReadOnlyList<decimal> xs, decimal pointerX)
    {
        var nearest = xs[0];
        var nearestDistance = Abs(pointerX - nearest);
        for (var i = 1; i < xs.Count; i++)
        {
            var distance = Abs(pointerX - xs[i]);
            if (distance < nearestDistance || (distance == nearestDistance && xs[i] < nearest))
            {
                nearest = xs[i];
                nearestDistance = distance;
            }
        }

        return (nearest, nearestDistance);
    }

    private static bool ContainsX(IReadOnlyList<decimal> xs, decimal x)
    {
        foreach (var candidate in xs)
        {
            if (candidate == x)
                return true;
        }

        return false;
    }

    private static decimal Abs(decimal value) => value < 0 ? -value : value;

    private static void PlaceEndpointCluster(
        double[] anchors,
        double[] placed,
        int start,
        int end,
        double gap,
        double minCenter,
        double maxCenter)
    {
        var count = end - start + 1;
        if (count == 1)
        {
            placed[start] = ClampDouble(anchors[start], minCenter, maxCenter);
            return;
        }

        double sum = 0;
        for (var i = start; i <= end; i++)
            sum += anchors[i];
        var centroid = sum / count;
        var span = (count - 1) * gap;
        var available = maxCenter - minCenter;
        if (span > available)
        {
            var step = available / (count - 1);
            for (var i = 0; i < count; i++)
                placed[start + i] = minCenter + i * step;
            return;
        }

        var first = centroid - span / 2;
        if (first < minCenter)
            first = minCenter;
        else if (first + span > maxCenter)
            first = maxCenter - span;

        for (var i = 0; i < count; i++)
            placed[start + i] = first + i * gap;
    }

    private static double[] PlaceVerticalEndpointCenters(
        double[] anchors,
        double clearance,
        double minimumGap,
        double minCenter,
        double maxCenter)
    {
        var count = anchors.Length;
        var centers = new double[count];
        for (var i = 0; i < count; i++)
        {
            var above = anchors[i] - clearance;
            var below = anchors[i] + clearance;
            if (above >= minCenter - LabelGapEpsilon)
                centers[i] = above;
            else if (below <= maxCenter + LabelGapEpsilon)
                centers[i] = below;
            else
                centers[i] = ClampDouble(anchors[i], minCenter, maxCenter);
        }

        PushEndpointGapsDown(centers, minimumGap);
        PullEndpointGapsUp(centers, minimumGap, minCenter, maxCenter);

        for (var pass = 0; pass < count * 2; pass++)
        {
            var moved = false;
            for (var i = 0; i < count; i++)
            {
                if (ClearsEndpointAnchor(centers[i], anchors[i], clearance))
                    continue;
                if (!TryNearestClearEndpointCenter(
                        anchors[i],
                        centers[i],
                        clearance,
                        minCenter,
                        maxCenter,
                        out var clear))
                    continue;
                if (Math.Abs(clear - centers[i]) <= LabelGapEpsilon)
                    continue;

                centers[i] = clear;
                PullEarlierEndpointLabels(centers, i, minimumGap, minCenter);
                PushLaterEndpointLabels(centers, i, minimumGap, maxCenter);
                moved = true;
            }

            if (!moved)
                break;
        }

        for (var i = 0; i < count; i++)
            centers[i] = ClampDouble(centers[i], minCenter, maxCenter);
        return centers;
    }

    private static void PushEndpointGapsDown(double[] centers, double minimumGap)
    {
        for (var i = 1; i < centers.Length; i++)
        {
            var minY = centers[i - 1] + minimumGap;
            if (centers[i] < minY)
                centers[i] = minY;
        }
    }

    private static void PullEndpointGapsUp(
        double[] centers,
        double minimumGap,
        double minCenter,
        double maxCenter)
    {
        if (centers.Length == 0)
            return;

        if (centers[^1] > maxCenter)
            centers[^1] = maxCenter;

        for (var i = centers.Length - 2; i >= 0; i--)
        {
            var maxY = centers[i + 1] - minimumGap;
            if (centers[i] > maxY)
                centers[i] = maxY;
            if (centers[i] < minCenter)
                centers[i] = minCenter;
        }
    }

    private static void PullEarlierEndpointLabels(
        double[] centers,
        int index,
        double minimumGap,
        double minCenter)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            var maxY = centers[i + 1] - minimumGap;
            if (centers[i] <= maxY + LabelGapEpsilon)
                break;

            centers[i] = Math.Max(minCenter, maxY);
        }
    }

    private static void PushLaterEndpointLabels(
        double[] centers,
        int index,
        double minimumGap,
        double maxCenter)
    {
        for (var i = index + 1; i < centers.Length; i++)
        {
            var minY = centers[i - 1] + minimumGap;
            if (centers[i] >= minY - LabelGapEpsilon)
                break;

            centers[i] = Math.Min(maxCenter, minY);
        }
    }

    private static bool TryNearestClearEndpointCenter(
        double anchor,
        double current,
        double clearance,
        double minCenter,
        double maxCenter,
        out double center)
    {
        center = current;
        double? best = null;
        var bestDistance = double.MaxValue;
        Consider(anchor - clearance);
        Consider(anchor + clearance);
        if (best is not double chosen)
            return false;

        center = chosen;
        return true;

        void Consider(double candidate)
        {
            if (candidate < minCenter - LabelGapEpsilon || candidate > maxCenter + LabelGapEpsilon)
                return;

            var distance = Math.Abs(candidate - current);
            var closer = distance < bestDistance - LabelGapEpsilon;
            var tiePreferAbove = Math.Abs(distance - bestDistance) <= LabelGapEpsilon
                && best is double existing
                && candidate < existing;
            if (best is null || closer || tiePreferAbove)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
    }

    private static bool ClearsEndpointAnchor(double center, double anchor, double clearance) =>
        Math.Abs(center - anchor) >= clearance - LabelGapEpsilon;

    private static double ClampDouble(double value, double min, double max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
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

    private static IEnumerable<UsageChartPoint> ClipSegmentY(
        UsageChartPoint left,
        UsageChartPoint right,
        decimal yMin,
        decimal yMax)
    {
        var ySpan = right.Y - left.Y;
        if (ySpan == 0)
        {
            if (InYRange(left.Y, yMin, yMax))
            {
                yield return left;
                yield return right;
            }

            yield break;
        }

        var tAtMin = (yMin - left.Y) / ySpan;
        var tAtMax = (yMax - left.Y) / ySpan;
        var tEnter = tAtMin < tAtMax ? tAtMin : tAtMax;
        var tExit = tAtMin < tAtMax ? tAtMax : tAtMin;

        var t0 = tEnter < 0m ? 0m : tEnter;
        var t1 = tExit > 1m ? 1m : tExit;
        if (t0 > t1)
            yield break;

        yield return PointAt(left, right, t0, tAtMin, tAtMax, yMin, yMax);
        yield return PointAt(left, right, t1, tAtMin, tAtMax, yMin, yMax);
    }

    private static UsageChartPoint PointAt(
        UsageChartPoint left,
        UsageChartPoint right,
        decimal t,
        decimal tAtMin,
        decimal tAtMax,
        decimal yMin,
        decimal yMax)
    {
        if (t <= 0m)
            return left;
        if (t >= 1m)
            return right;

        var y = t == tAtMin ? yMin : t == tAtMax ? yMax : left.Y + t * (right.Y - left.Y);
        return new UsageChartPoint
        {
            X = left.X + t * (right.X - left.X),
            Y = y
        };
    }

    private static bool InYRange(decimal y, decimal yMin, decimal yMax) =>
        y >= yMin && y <= yMax;

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
    decimal ExpectedDelta,
    bool CursorIsEstimated,
    bool OtherIsEstimated,
    bool CursorStartIsEstimated,
    bool OtherStartIsEstimated);
