using System.Globalization;
using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.ViewModels;

public sealed class DayRowViewModel : ViewModelBase
{
    private readonly QuotaDayEntry _model;
    private bool _isToday;
    private readonly double _expectedQuotaCursor;
    private readonly double _expectedQuotaOther;
    private readonly double? _projectedQuotaCursor;
    private readonly double? _projectedQuotaOther;
    private readonly bool _cursorWillRunOut;
    private readonly bool _otherWillRunOut;

    public DayRowViewModel(
        QuotaDayEntry model,
        double expectedQuotaCursor,
        double expectedQuotaOther,
        decimal? projectedQuotaCursor,
        decimal? projectedQuotaOther,
        bool cursorWillRunOut,
        bool otherWillRunOut)
    {
        _model = model;
        _expectedQuotaCursor = expectedQuotaCursor;
        _expectedQuotaOther = expectedQuotaOther;
        _projectedQuotaCursor = projectedQuotaCursor is null ? null : (double)projectedQuotaCursor.Value;
        _projectedQuotaOther = projectedQuotaOther is null ? null : (double)projectedQuotaOther.Value;
        _cursorWillRunOut = cursorWillRunOut;
        _otherWillRunOut = otherWillRunOut;
    }

    public int DayNumber => _model.DayNumber;
    public DateTime Date => _model.Date;
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

    public double CursorModelsValue => (double)_model.CursorModelsPercent;
    public double OtherModelsValue => (double)_model.OtherModelsPercent;

    public double ExpectedQuotaCursor => _expectedQuotaCursor;
    public double ExpectedQuotaOther => _expectedQuotaOther;
    public double? ProjectedQuotaCursor => _projectedQuotaCursor;
    public double? ProjectedQuotaOther => _projectedQuotaOther;
    public bool HasCursorProjection => _projectedQuotaCursor.HasValue;
    public bool HasOtherProjection => _projectedQuotaOther.HasValue;
    public bool CursorWillRunOut => _cursorWillRunOut;
    public bool OtherWillRunOut => _otherWillRunOut;
    public bool IsRunOutDay => _cursorWillRunOut || _otherWillRunOut;

    public bool IsModified => _model.CursorModelsIsManual || _model.OtherModelsIsManual;

    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    public bool IsManuallyEdited => _model.CursorModelsIsManual || _model.OtherModelsIsManual;

    public event Action<int, decimal>? CursorModelsEdited;
    public event Action<int, decimal>? OtherModelsEdited;

    public void UpdateFromModel(QuotaDayEntry model)
    {
        OnPropertyChanged(nameof(CursorModelsText));
        OnPropertyChanged(nameof(OtherModelsText));
        OnPropertyChanged(nameof(CursorModelsValue));
        OnPropertyChanged(nameof(OtherModelsValue));
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsManuallyEdited));
    }
}
