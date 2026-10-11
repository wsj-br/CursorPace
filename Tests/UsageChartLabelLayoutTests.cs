using CursorPace.Services;

namespace CursorPace.Tests;

public class UsageChartLabelLayoutTests
{
    [Fact]
    public void OverlappingPair_SpreadsSymmetricallyToTheMinimumGap()
    {
        var placed = UsageChartMath.ResolveEndpointLabelOffsets([100, 104], 10, 16, 0, 200);

        Assert.Equal(94, placed[0], precision: 6);
        Assert.Equal(110, placed[1], precision: 6);
        Assert.Equal(102, (placed[0] + placed[1]) / 2, precision: 6);
    }

    [Fact]
    public void ThreeWayCluster_PreservesTopToBottomOrder()
    {
        var placed = UsageChartMath.ResolveEndpointLabelOffsets([30, 10, 20], 10, 12, 0, 200);

        Assert.Equal(32, placed[0], precision: 6);
        Assert.Equal(8, placed[1], precision: 6);
        Assert.Equal(20, placed[2], precision: 6);
        Assert.True(placed[1] < placed[2]);
        Assert.True(placed[2] < placed[0]);
    }

    [Fact]
    public void SeparatedLabels_StayOnTheirAnchors()
    {
        var placed = UsageChartMath.ResolveEndpointLabelOffsets([10, 40, 70], 10, 12, 0, 200);

        Assert.Equal([10d, 40d, 70d], placed);
    }

    [Fact]
    public void LabelsAtTheMinimumGap_StayOnTheirAnchors()
    {
        var placed = UsageChartMath.ResolveEndpointLabelOffsets([10, 22], 10, 12, 0, 200);

        Assert.Equal([10d, 22d], placed);
    }

    [Fact]
    public void PlotEdge_ClampsTheSpreadInsideTheLabelInset()
    {
        var top = UsageChartMath.ResolveEndpointLabelOffsets([0, 4], 10, 16, 0, 100);
        Assert.Equal(5, top[0], precision: 6);
        Assert.Equal(21, top[1], precision: 6);

        var bottom = UsageChartMath.ResolveEndpointLabelOffsets([96, 100], 10, 16, 0, 100);
        Assert.Equal(79, bottom[0], precision: 6);
        Assert.Equal(95, bottom[1], precision: 6);
    }

    [Fact]
    public void PlotEdge_PacksLabelsWhenThePlotCannotFitTheGap()
    {
        var placed = UsageChartMath.ResolveEndpointLabelOffsets([0, 1, 2], 10, 16, 0, 20);

        Assert.Equal(5, placed[0], precision: 6);
        Assert.Equal(10, placed[1], precision: 6);
        Assert.Equal(15, placed[2], precision: 6);
    }

    [Fact]
    public void ExpandedClusters_MergeWhenTheSpreadWouldOverlap()
    {
        var placed = UsageChartMath.ResolveEndpointLabelOffsets([0, 2, 20, 22], 8, 11, -100, 200);

        Assert.Equal(-5.5, placed[0], precision: 6);
        Assert.Equal(5.5, placed[1], precision: 6);
        Assert.Equal(16.5, placed[2], precision: 6);
        Assert.Equal(27.5, placed[3], precision: 6);
    }

    [Fact]
    public void VerticalLabels_SitAboveSeparatedAnchors()
    {
        var placed = UsageChartMath.ResolveVerticalEndpointLabels([80, 220, 258], 10, 2, 0, 400);

        Assert.Equal(73, placed[0], precision: 6);
        Assert.Equal(213, placed[1], precision: 6);
        Assert.Equal(251, placed[2], precision: 6);
    }

    [Fact]
    public void VerticalLabels_DropBelowWhenThePlotHasNoRoomAbove()
    {
        var placed = UsageChartMath.ResolveVerticalEndpointLabels([8], 10, 2, 0, 200);

        Assert.Equal(15, placed[0], precision: 6);
    }

    [Fact]
    public void VerticalLabels_PutAClosePairOnOppositeSides()
    {
        var placed = UsageChartMath.ResolveVerticalEndpointLabels([100, 104], 10, 2, 0, 200);

        Assert.Equal(93, placed[0], precision: 6);
        Assert.Equal(111, placed[1], precision: 6);
        Assert.True(placed[1] - placed[0] >= 12);
    }

    [Fact]
    public void VerticalLabels_KeepThreeCloseValuesOffTheirAnchors()
    {
        var placed = UsageChartMath.ResolveVerticalEndpointLabels([106, 100, 103], 10, 2, 0, 400);

        Assert.Equal(122, placed[0], precision: 6);
        Assert.Equal(93, placed[1], precision: 6);
        Assert.Equal(110, placed[2], precision: 6);
    }

    [Fact]
    public void VerticalLabels_ShiftEarlierLabelsUpToClearTheBottomEdge()
    {
        var placed = UsageChartMath.ResolveVerticalEndpointLabels([180, 184, 188], 10, 2, 0, 200);

        Assert.Equal(165, placed[0], precision: 6);
        Assert.Equal(177, placed[1], precision: 6);
        Assert.Equal(195, placed[2], precision: 6);
    }
}
