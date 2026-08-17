namespace CursorQuotaProgress.Models;

public sealed class QuotaDayEntry
{
    public int DayNumber { get; init; }
    public DateTime Date { get; init; }
    public decimal CursorModelsPercent { get; set; }
    public decimal OtherModelsPercent { get; set; }
    public bool CursorModelsIsManual { get; set; }
    public bool OtherModelsIsManual { get; set; }
}
