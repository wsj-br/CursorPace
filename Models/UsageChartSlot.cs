namespace CursorPace.Models;

/// <summary>
/// One axis slot. Day ranges are delimited by local midnights and labelled with the
/// day of the month. Intraday ranges are delimited by hours and keep the slot time.
/// </summary>
public sealed class UsageChartSlot
{
    public required DateTime Date { get; init; }
    public required decimal StartX { get; init; }
    public required decimal EndX { get; init; }

    /// <summary>
    /// True for the truncated opening slot of a cycle that starts mid-day. It is too
    /// narrow to label, and its day of the month already appears on the next cycle.
    /// </summary>
    public required bool IsLeadingPartial { get; init; }

    public decimal MidX => (StartX + EndX) / 2m;
}
