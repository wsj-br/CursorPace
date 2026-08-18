namespace CursorUsageProgress.Models;

public sealed class QuotaCycle
{
    public required int RenewalDay { get; init; }
    public required DateTime CycleStart { get; init; }
    public required DateTime NextRenewal { get; init; }

    /// <summary>
    /// Only days the user has edited. Remaining days are computed.
    /// </summary>
    public List<QuotaDayEdit> Edits { get; set; } = new();

    /// <summary>
    /// Full in-memory calendar. Not persisted.
    /// </summary>
    public List<QuotaDayEntry> Days { get; set; } = new();
}
