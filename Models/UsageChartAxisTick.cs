namespace CursorUsageProgress.Models;

public sealed class UsageChartAxisTick
{
    public required int DayNumber { get; init; }
    public required DateTime Date { get; init; }
    public required decimal X { get; init; }
}
