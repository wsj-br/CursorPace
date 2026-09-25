using CursorPace.Models;
using CursorPace.ViewModels;

namespace CursorPace.Tests;

public class UsageChartViewModelTests
{
    [Fact]
    public void SelectRange_RaisesWhenThePresetChanges()
    {
        var chart = new UsageChartViewModel();
        var raised = 0;
        chart.RangeChanged += (_, _) => raised++;

        chart.SelectedRange = UsageChartRange.SevenDays;

        Assert.Equal(UsageChartRange.SevenDays, chart.SelectedRange);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void SelectRange_SamePresetWithoutZoom_DoesNotRaise()
    {
        var chart = new UsageChartViewModel();
        var raised = 0;
        chart.RangeChanged += (_, _) => raised++;

        chart.SelectRange(UsageChartRange.OneMonth);

        Assert.Equal(0, raised);
        Assert.False(chart.IsCustomViewport);
    }

    [Fact]
    public void SelectRange_SamePresetClearsZoomWithoutViewportEvent()
    {
        var chart = new UsageChartViewModel();
        chart.CustomViewport = new UsageChartViewport(10m, 40m);
        var rangeRaised = 0;
        var viewportRaised = 0;
        chart.RangeChanged += (_, _) => rangeRaised++;
        chart.ViewportChanged += (_, _) => viewportRaised++;

        chart.SelectRange(UsageChartRange.OneMonth);

        Assert.Null(chart.CustomViewport);
        Assert.False(chart.IsCustomViewport);
        Assert.Equal(1, rangeRaised);
        Assert.Equal(0, viewportRaised);
    }

    [Fact]
    public void CustomViewport_NormalizesReversedBounds()
    {
        var chart = new UsageChartViewModel();
        UsageChartViewport? seen = null;
        chart.ViewportChanged += (_, _) => seen = chart.CustomViewport;

        chart.CustomViewport = new UsageChartViewport(30m, 10m);

        Assert.Equal(new UsageChartViewport(10m, 30m), chart.CustomViewport);
        Assert.Equal(seen, chart.CustomViewport);
        Assert.True(chart.IsCustomViewport);
    }

    [Fact]
    public void CustomViewport_IgnoresAnEmptySpan()
    {
        var chart = new UsageChartViewModel();
        var raised = 0;
        chart.ViewportChanged += (_, _) => raised++;

        chart.CustomViewport = new UsageChartViewport(5m, 5m);

        Assert.Null(chart.CustomViewport);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void NotifyDisplayedCycle_KeepsZoomForTheSameBounds()
    {
        var chart = new UsageChartViewModel();
        var start = new DateTime(2026, 8, 1);
        var end = new DateTime(2026, 9, 1);
        chart.NotifyDisplayedCycle(start, end);
        chart.CustomViewport = new UsageChartViewport(1m, 2m);

        chart.NotifyDisplayedCycle(start, end);

        Assert.Equal(new UsageChartViewport(1m, 2m), chart.CustomViewport);
    }

    [Fact]
    public void NotifyDisplayedCycle_ClearsZoomWhenBoundsChangeWithoutRaising()
    {
        var chart = new UsageChartViewModel();
        chart.NotifyDisplayedCycle(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        chart.CustomViewport = new UsageChartViewport(1m, 2m);
        var raised = 0;
        chart.ViewportChanged += (_, _) => raised++;

        chart.NotifyDisplayedCycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));

        Assert.Null(chart.CustomViewport);
        Assert.False(chart.IsCustomViewport);
        Assert.Equal(0, raised);
    }
}
