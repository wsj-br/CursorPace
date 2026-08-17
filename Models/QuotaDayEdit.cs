namespace CursorQuotaProgress.Models;

/// <summary>
/// A user override for one day. Unedited days are not stored; their
/// percentages are derived from cycle length and the last prior edit.
/// </summary>
public sealed class QuotaDayEdit
{
    public int DayNumber { get; set; }
    public decimal? CursorModelsPercent { get; set; }
    public decimal? OtherModelsPercent { get; set; }

    public bool HasAnyValue => CursorModelsPercent.HasValue || OtherModelsPercent.HasValue;
}
