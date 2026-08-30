namespace CursorPace.Models;

public sealed class QuotaDayEntry
{
    public int DayNumber { get; init; }
    public DateTime Date { get; init; }
    public decimal CursorModelsPercent { get; set; }
    public decimal OtherModelsPercent { get; set; }
    public bool CursorModelsIsActual { get; set; }
    public bool OtherModelsIsActual { get; set; }
}
