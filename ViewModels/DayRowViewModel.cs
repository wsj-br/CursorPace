using System.Globalization;
using CursorUsageProgress.Models;

namespace CursorUsageProgress.ViewModels;

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

    public double CursorModelsValue => (double)_model.CursorModelsPercent;
    public double OtherModelsValue => (double)_model.OtherModelsPercent;

    public double ExpectedQuotaCursor => _expectedQuotaCursor;
    public double ExpectedQuotaOther => _expectedQuotaOther;
    public double? ProjectedQuotaCursor => _projectedQuotaCursor;
    public double? ProjectedQuotaOther => _projectedQuotaOther;

    public int ShownExpectedCursor => (int)_expectedQuotaCursor;
    public int ShownExpectedOther => (int)_expectedQuotaOther;
    public int? ShownProjectedCursor => ToShownProjected(_projectedQuotaCursor);
    public int? ShownProjectedOther => ToShownProjected(_projectedQuotaOther);

    private static int? ToShownProjected(double? value) =>
        value is double percent
            ? (int)Math.Round(percent, MidpointRounding.AwayFromZero)
            : null;

    public bool HasCursorProjection => _projectedQuotaCursor.HasValue;
    public bool HasOtherProjection => _projectedQuotaOther.HasValue;
    public bool CursorWillRunOut => _cursorWillRunOut;
    public bool OtherWillRunOut => _otherWillRunOut;
    public bool IsRunOutDay => _cursorWillRunOut || _otherWillRunOut;

    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    public bool IsActual => _model.CursorModelsIsActual || _model.OtherModelsIsActual;
}
