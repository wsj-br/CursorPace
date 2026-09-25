using System.Globalization;
using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class UsageChartRangeTests
{
    private readonly CycleCalculator _calculator = new();
    private readonly UsageChartSeriesBuilder _builder = new();

    [Fact]
    public void Ranges_UseElapsedDurationsAndCapAtCycleStart()
    {
        var start = new DateTime(2026, 8, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, new DateTime(2026, 9, 1));
        var now = new DateTime(2026, 8, 5, 15, 0, 0);

        var oneDay = Bounds(cycle, UsageChartRange.OneDay, now, true);
        var twoDays = Bounds(cycle, UsageChartRange.TwoDays, now, true);
        var sevenDays = Bounds(cycle, UsageChartRange.SevenDays, now, true);
        var oneWeek = Bounds(cycle, UsageChartRange.OneWeek, now, true);
        var twoWeeks = Bounds(cycle, UsageChartRange.TwoWeeks, now, true);
        var month = Bounds(cycle, UsageChartRange.OneMonth, now, true);

        Assert.Equal(now.AddDays(-1), oneDay.Start);
        Assert.Equal(now, oneDay.End);
        Assert.Equal(TimeSpan.FromDays(2), twoDays.End - twoDays.Start);
        Assert.Equal(start, sevenDays.Start);
        Assert.Equal(now, sevenDays.End);
        Assert.Equal(sevenDays, oneWeek);
        Assert.Equal(start, twoWeeks.Start);
        Assert.Equal(now, twoWeeks.End);
        Assert.Equal(cycle.CycleStart, month.Start);
        Assert.Equal(cycle.NextRenewal, month.End);
    }

    [Fact]
    public void ArchivedRange_EndsAtRenewal()
    {
        var cycle = _calculator.GenerateCycleFromBounds(new DateTime(2026, 7, 1), new DateTime(2026, 8, 1));
        var bounds = Bounds(cycle, UsageChartRange.SevenDays, new DateTime(2026, 7, 4), false);

        Assert.Equal(cycle.NextRenewal, bounds.End);
        Assert.Equal(cycle.NextRenewal.AddDays(-7), bounds.Start);
    }

    [Fact]
    public void OneDayAndTwoDays_KeepRawSamples()
    {
        var cycle = MidnightCycle();
        var now = cycle.CycleStart.AddDays(4).AddHours(12);
        var morning = now.AddHours(-5);
        var evening = now.AddHours(-1);
        var previous = now.AddHours(-36);
        var samples = new List<UsageSample>
        {
            SampleAt(previous, 1m, 1m),
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m)
        };

        var oneDay = _builder.Build(cycle, _calculator, samples, UsageChartRange.OneDay, now, true);
        var twoDays = _builder.Build(cycle, _calculator, samples, UsageChartRange.TwoDays, now, true);

        Assert.True(oneDay.UsesIntradayAxis);
        Assert.Contains(oneDay.CursorUsage, point => point.Y == 5m && point.IsMeasured);
        Assert.Contains(oneDay.CursorUsage, point => point.Y == 8m && point.IsMeasured);
        Assert.Contains(oneDay.OtherUsage, point => point.Y == 6m && point.IsMeasured);
        Assert.Contains(oneDay.OtherUsage, point => point.Y == 9m && point.IsMeasured);
        Assert.DoesNotContain(oneDay.CursorUsage, point => point.Y == 1m);
        Assert.All(oneDay.ExpectedUsage, point => Assert.False(point.IsMeasured));
        Assert.All(oneDay.CursorEstimated, point => Assert.False(point.IsMeasured));
        Assert.Equal(8m, oneDay.LastMeasuredCursor!.Y);
        Assert.Equal(9m, oneDay.LastMeasuredOther!.Y);
        Assert.Contains(twoDays.CursorUsage, point => point.Y == 1m);
        Assert.Contains(twoDays.CursorUsage, point => point.Y == 8m);
    }

    [Fact]
    public void SevenDays_CollapsesToLastSamplePerDay()
    {
        var cycle = MidnightCycle();
        var now = cycle.CycleStart.AddDays(10);
        var morning = now.AddDays(-2).AddHours(8);
        var evening = now.AddDays(-2).AddHours(20);
        var previousDay = now.AddDays(-3).AddHours(12);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m),
            SampleAt(previousDay, 3m, 4m)
        };

        var document = _builder.Build(cycle, _calculator, samples, UsageChartRange.SevenDays, now, true);

        Assert.False(document.UsesIntradayAxis);
        Assert.Equal(2, document.CursorUsage.Count);
        Assert.All(document.CursorUsage, point => Assert.True(point.IsMeasured));
        Assert.Equal(3m, document.CursorUsage[0].Y);
        Assert.Equal(8m, document.CursorUsage[1].Y);
        Assert.DoesNotContain(document.CursorUsage, point => point.Y == 5m);
    }

    [Fact]
    public void Clip_AddsViewportBoundaryPoints()
    {
        var clipped = UsageChartMath.ClipToViewport(
        [
            new UsageChartPoint { X = 0m, Y = 0m },
            new UsageChartPoint { X = 100m, Y = 50m }
        ], 40m, 80m);

        Assert.Equal(40m, clipped[0].X);
        Assert.Equal(20m, clipped[0].Y);
        Assert.Equal(80m, clipped[1].X);
        Assert.Equal(40m, clipped[1].Y);
        Assert.All(clipped, point => Assert.False(point.IsMeasured));
    }

    [Fact]
    public void Clip_KeepsMeasuredSamplesAndOmitsViewportEdges()
    {
        var clipped = UsageChartMath.ClipToViewport(
        [
            new UsageChartPoint { X = 0m, Y = 0m, IsMeasured = true },
            new UsageChartPoint { X = 50m, Y = 25m, IsMeasured = true },
            new UsageChartPoint { X = 100m, Y = 50m, IsMeasured = true }
        ], 40m, 80m);

        Assert.Equal([40m, 50m, 80m], clipped.Select(point => point.X));
        Assert.Equal([false, true, false], clipped.Select(point => point.IsMeasured));
    }

    [Fact]
    public void Hover_InterpolatesDisplayedSeriesAndLeavesGapsBlank()
    {
        var cycle = MidnightCycle();
        var first = cycle.CycleStart.AddDays(10);
        var second = cycle.CycleStart.AddDays(20);
        var samples = new List<UsageSample>
        {
            SampleAt(first, 20m, 40m),
            SampleAt(second, 40m, 80m)
        };
        var document = _builder.Build(cycle, _calculator, samples);
        var midX = (UsageChartSeriesBuilder.ToAxisX(cycle, first) + UsageChartSeriesBuilder.ToAxisX(cycle, second)) / 2m;
        var before = UsageChartSeriesBuilder.ToAxisX(cycle, cycle.CycleStart.AddDays(1));

        var midpoint = UsageChartMath.Read(document, midX);
        var gap = UsageChartMath.Read(document, before);

        Assert.Equal(30m, midpoint.CursorPercent);
        Assert.Equal(60m, midpoint.OtherPercent);
        Assert.Equal(UsageChartSeriesBuilder.LinearExpectedPercent(document.CycleSeconds, midX), midpoint.ExpectedPercent);
        Assert.Null(gap.CursorPercent);
        Assert.Null(gap.OtherPercent);
        Assert.Equal(UsageChartSeriesBuilder.LinearExpectedPercent(document.CycleSeconds, before), gap.ExpectedPercent);
    }

    [Theory]
    [InlineData("99", "90", "100")]
    [InlineData("100", "100", "110")]
    [InlineData("100.1", "100", "110")]
    [InlineData("109.9", "100", "110")]
    [InlineData("110", "110", "120")]
    [InlineData("121", "120", "130")]
    public void YBounds_RoundToTenPercentAndKeepValuesInside(string valueText, string expectedMin, string expectedMax)
    {
        var value = decimal.Parse(valueText, CultureInfo.InvariantCulture);
        var bounds = UsageChartMath.YBounds([value]);

        Assert.Equal(decimal.Parse(expectedMin, CultureInfo.InvariantCulture), bounds.Min);
        Assert.Equal(decimal.Parse(expectedMax, CultureInfo.InvariantCulture), bounds.Max);
        Assert.True(bounds.Min <= value);
        Assert.True(bounds.Max >= value);
        Assert.True(bounds.Max > bounds.Min);
    }

    [Fact]
    public void CustomViewport_UpToFourDays_KeepsEverySample()
    {
        var cycle = MidnightCycle();
        var now = cycle.CycleStart.AddDays(10);
        var start = cycle.CycleStart.AddDays(4);
        var morning = start.AddHours(3);
        var evening = start.AddHours(20);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m)
        };
        var viewport = new UsageChartViewport(
            UsageChartSeriesBuilder.ToAxisX(cycle, start),
            UsageChartSeriesBuilder.ToAxisX(cycle, start.AddDays(4)));

        var document = _builder.Build(
            cycle,
            _calculator,
            samples,
            UsageChartRange.OneMonth,
            now,
            true,
            viewport);

        Assert.True(document.IsCustomViewport);
        Assert.True(document.UsesIntradayAxis);
        Assert.Equal(start, document.VisibleStart);
        Assert.Equal(start.AddDays(4), document.VisibleEnd);
        Assert.Equal([5m, 8m], document.CursorUsage.Select(point => point.Y));
        Assert.Equal([6m, 9m], document.OtherUsage.Select(point => point.Y));
    }

    [Fact]
    public void CustomViewport_LongerThanFourDays_KeepsLastSamplePerDay()
    {
        var cycle = MidnightCycle();
        var start = cycle.CycleStart.AddDays(2);
        var morning = start.AddHours(8);
        var evening = start.AddHours(20);
        var nextDay = start.AddDays(1).AddHours(12);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m),
            SampleAt(nextDay, 15m, 16m)
        };
        var viewport = new UsageChartViewport(
            UsageChartSeriesBuilder.ToAxisX(cycle, start),
            UsageChartSeriesBuilder.ToAxisX(cycle, start.AddDays(4).AddSeconds(1)));

        var document = _builder.Build(cycle, _calculator, samples, viewport: viewport);

        Assert.True(document.IsCustomViewport);
        Assert.False(document.UsesIntradayAxis);
        Assert.Equal([8m, 15m], document.CursorUsage.Select(point => point.Y));
        Assert.DoesNotContain(document.CursorUsage, point => point.Y == 5m);
    }

    [Fact]
    public void SampleDetail_TwoDays_CollapsesAFourDayZoom()
    {
        var cycle = MidnightCycle();
        var start = cycle.CycleStart.AddDays(2);
        var morning = start.AddHours(8);
        var evening = start.AddHours(20);
        var nextDay = start.AddDays(1).AddHours(12);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m),
            SampleAt(nextDay, 15m, 16m)
        };
        var viewport = new UsageChartViewport(
            UsageChartSeriesBuilder.ToAxisX(cycle, start),
            UsageChartSeriesBuilder.ToAxisX(cycle, start.AddDays(4)));

        var document = _builder.Build(
            cycle,
            _calculator,
            samples,
            viewport: viewport,
            rawSampleMaxDays: 2);

        Assert.False(document.UsesIntradayAxis);
        Assert.Equal([8m, 15m], document.CursorUsage.Select(point => point.Y));
        Assert.DoesNotContain(document.CursorUsage, point => point.Y == 5m);
    }

    [Fact]
    public void SampleDetail_SevenDays_PlotsEverySampleOnTheSevenDayPreset()
    {
        var cycle = MidnightCycle();
        var now = cycle.CycleStart.AddDays(10);
        var morning = now.AddDays(-2).AddHours(8);
        var evening = now.AddDays(-2).AddHours(20);
        var previousDay = now.AddDays(-3).AddHours(12);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m),
            SampleAt(previousDay, 3m, 4m)
        };

        var document = _builder.Build(
            cycle,
            _calculator,
            samples,
            UsageChartRange.SevenDays,
            now,
            true,
            rawSampleMaxDays: 7);

        Assert.True(document.UsesIntradayAxis);
        Assert.Equal([3m, 5m, 8m], document.CursorUsage.Select(point => point.Y));
        Assert.False(_builder.Build(
            cycle,
            _calculator,
            samples,
            UsageChartRange.TwoWeeks,
            now,
            true,
            rawSampleMaxDays: 7).UsesIntradayAxis);
    }

    [Fact]
    public void SevenDays_StaysDailyWhenTheVisibleWindowIsUnderTwoDays()
    {
        var cycle = MidnightCycle();
        var now = cycle.CycleStart.AddHours(20);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(2), 5m, 6m),
            SampleAt(cycle.CycleStart.AddHours(8), 8m, 9m),
            SampleAt(cycle.CycleStart.AddHours(15), 11m, 12m)
        };

        var preset = _builder.Build(cycle, _calculator, samples, UsageChartRange.SevenDays, now, true);
        var zoom = _builder.Build(
            cycle,
            _calculator,
            samples,
            UsageChartRange.SevenDays,
            now,
            true,
            new UsageChartViewport(0m, UsageChartSeriesBuilder.ToAxisX(cycle, now)));

        Assert.False(preset.IsCustomViewport);
        Assert.False(preset.UsesIntradayAxis);
        Assert.Empty(preset.CursorUsage);
        Assert.True(zoom.IsCustomViewport);
        Assert.True(zoom.UsesIntradayAxis);
        Assert.Equal([5m, 8m, 11m], zoom.CursorUsage.Select(point => point.Y));
    }

    [Fact]
    public void CustomViewport_ClampsReversedAndOutOfCycleBounds()
    {
        var cycle = MidnightCycle();
        var reversed = _builder.Build(
            cycle,
            _calculator,
            samples: null,
            viewport: new UsageChartViewport(2_000m, 1_000m));
        var outside = _builder.Build(
            cycle,
            _calculator,
            samples: null,
            viewport: new UsageChartViewport(-100m, reversed.CycleSeconds + 50m));

        Assert.Equal(1_000m, reversed.VisibleStartX);
        Assert.Equal(2_000m, reversed.VisibleEndX);
        Assert.Equal(0m, outside.VisibleStartX);
        Assert.Equal(outside.CycleSeconds, outside.VisibleEndX);
        Assert.Equal(cycle.CycleStart, outside.VisibleStart);
        Assert.Equal(cycle.NextRenewal, outside.VisibleEnd);
    }

    [Theory]
    [InlineData(12, 800, 40, 1)]
    [InlineData(24, 700, 48, 2)]
    [InlineData(96, 700, 48, 8)]
    [InlineData(168, 400, 48, 24)]
    public void IntradayLabelStep_WidensUntilTimeLabelsFit(int slotCount, double plotWidth, double minSpacing, int expectedStep)
    {
        Assert.Equal(expectedStep, UsageChartMath.IntradayLabelStep(slotCount, plotWidth, minSpacing));
    }

    [Fact]
    public void IntradayLabelIndexes_KeepARegularStepAndTheLastSlot()
    {
        Assert.Equal([0, 6, 12, 18, 20], UsageChartMath.IntradayLabelIndexes(21, 6));
        Assert.Equal([0, 8, 16], UsageChartMath.IntradayLabelIndexes(17, 8));
    }

    [Fact]
    public void DragSelection_IgnoresShortSpansAndClampsTheRest()
    {
        Assert.Null(UsageChartMath.ViewportFromDrag(1m, 9m, 0m, 100m, pixelSpan: 3, minimumPixelSpan: 4));

        var zoom = UsageChartMath.ViewportFromDrag(80m, 5m, 0m, 50m, pixelSpan: 40, minimumPixelSpan: 4);

        Assert.Equal(5m, zoom!.Value.StartX);
        Assert.Equal(50m, zoom.Value.EndX);
    }

    [Fact]
    public void Hover_ReportsVisibleStartAndChangeForEachMetric()
    {
        var cycle = MidnightCycle();
        var first = cycle.CycleStart.AddDays(4);
        var second = cycle.CycleStart.AddDays(6);
        var samples = new List<UsageSample>
        {
            SampleAt(first, 20m, 40m),
            SampleAt(second, 50m, 10m)
        };
        var viewport = new UsageChartViewport(
            UsageChartSeriesBuilder.ToAxisX(cycle, first),
            UsageChartSeriesBuilder.ToAxisX(cycle, second));
        var document = _builder.Build(cycle, _calculator, samples, viewport: viewport);

        var readout = UsageChartMath.Read(document, viewport.EndX);

        Assert.Equal(50m, readout.CursorPercent);
        Assert.Equal(10m, readout.OtherPercent);
        Assert.Equal(20m, readout.CursorStartPercent);
        Assert.Equal(40m, readout.OtherStartPercent);
        Assert.Equal(30m, readout.CursorDelta);
        Assert.Equal(-30m, readout.OtherDelta);
        Assert.Equal(readout.ExpectedPercent - readout.ExpectedStartPercent, readout.ExpectedDelta);
    }

    [Fact]
    public void Hover_LeavesObservedDeltaBlankWhenTheRangeStartHasNoSample()
    {
        var cycle = MidnightCycle();
        var first = cycle.CycleStart.AddDays(10);
        var second = cycle.CycleStart.AddDays(20);
        var samples = new List<UsageSample>
        {
            SampleAt(first, 20m, 40m),
            SampleAt(second, 40m, 80m)
        };
        var document = _builder.Build(cycle, _calculator, samples);

        var readout = UsageChartMath.Read(document, UsageChartSeriesBuilder.ToAxisX(cycle, second));

        Assert.Null(readout.CursorStartPercent);
        Assert.Null(readout.OtherStartPercent);
        Assert.Null(readout.CursorDelta);
        Assert.Null(readout.OtherDelta);
        Assert.Equal(0m, readout.ExpectedStartPercent);
        Assert.Equal(readout.ExpectedPercent, readout.ExpectedDelta);
    }

    [Fact]
    public void VisibleSeries_FitInsideRoundedYAxis()
    {
        var cycle = MidnightCycle();
        var now = cycle.CycleStart.AddDays(2);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(1), 0m, 0m),
            SampleAt(now.AddHours(-1), 84m, 91m)
        };

        var document = _builder.Build(cycle, _calculator, samples, UsageChartRange.TwoDays, now, true);
        var values = document.ExpectedUsage.Concat(document.CursorUsage)
            .Concat(document.OtherUsage)
            .Concat(document.CursorEstimated)
            .Concat(document.OtherEstimated)
            .Select(point => point.Y);

        Assert.All(values, value =>
        {
            Assert.True(value >= document.YMin);
            Assert.True(value <= document.YMax);
        });
        Assert.Equal(0m, document.YMin % 10m);
        Assert.Equal(0m, document.YMax % 10m);
    }

    private (DateTime Start, DateTime End) Bounds(
        QuotaCycle cycle,
        UsageChartRange range,
        DateTime now,
        bool isLive) =>
        UsageChartMath.VisibleBounds(cycle, range, now, isLive);

    private QuotaCycle MidnightCycle() =>
        _calculator.GenerateCycleFromBounds(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

    private static UsageSample SampleAt(DateTime local, decimal cursor, decimal other)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new UsageSample
        {
            TimestampUtc = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset),
            CursorModelsPercent = cursor,
            OtherModelsPercent = other
        };
    }
}
