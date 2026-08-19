namespace CursorUsageProgress.Models;

public sealed class QuotaCycle
{
    public required int RenewalDay { get; init; }
    public required DateTime CycleStart { get; init; }
    public required DateTime NextRenewal { get; init; }

    /// <summary>
    /// Full in-memory calendar. Not persisted.
    /// </summary>
    public List<QuotaDayEntry> Days { get; set; } = new();
}
