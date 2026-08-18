namespace CursorUsageProgress.Models;

public sealed class UsageSampleDocument
{
    public int Version { get; set; } = 1;
    public DateTimeOffset? CycleStartUtc { get; set; }
    public List<UsageSample> Samples { get; set; } = new();
}
