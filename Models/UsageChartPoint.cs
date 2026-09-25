namespace CursorPace.Models;

public sealed class UsageChartPoint
{
    public required decimal X { get; init; }
    public required decimal Y { get; init; }

    /// <summary>
    /// True for an observed sample. Viewport clipping can add boundary points, and those stay unmarked.
    /// </summary>
    public bool IsMeasured { get; init; }
}
