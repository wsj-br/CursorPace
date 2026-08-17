namespace CursorQuotaProgress.Models;

public sealed class QuotaCycle
{
    public required int RenewalDay { get; init; }
    public required DateTime CycleStart { get; init; }
    public required DateTime NextRenewal { get; init; }
    public required List<QuotaDayEntry> Days { get; init; }
}
