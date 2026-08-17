using System.Globalization;
using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.ViewModels;

public sealed class DayRowViewModel : ViewModelBase
{
    private readonly QuotaDayEntry _model;
    private bool _isToday;

    public DayRowViewModel(QuotaDayEntry model)
    {
        _model = model;
    }

    public int DayNumber => _model.DayNumber;

    public string DateText => _model.Date.ToString("d", CultureInfo.CurrentCulture);

    public string CursorModelsText
    {
        get => _model.CursorModelsPercent.ToString("F2", CultureInfo.CurrentCulture);
        set
        {
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                && parsed >= 0 && parsed <= 100)
            {
                CursorModelsEdited?.Invoke(_model.DayNumber, parsed);
            }
        }
    }

    public string OtherModelsText
    {
        get => _model.OtherModelsPercent.ToString("F2", CultureInfo.CurrentCulture);
        set
        {
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                && parsed >= 0 && parsed <= 100)
            {
                OtherModelsEdited?.Invoke(_model.DayNumber, parsed);
            }
        }
    }

    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    public event Action<int, decimal>? CursorModelsEdited;
    public event Action<int, decimal>? OtherModelsEdited;

    public void UpdateFromModel(QuotaDayEntry model)
    {
        OnPropertyChanged(nameof(CursorModelsText));
        OnPropertyChanged(nameof(OtherModelsText));
    }
}
