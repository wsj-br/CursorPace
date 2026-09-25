using CursorPace.Models;

namespace CursorPace.ViewModels;

public sealed class UsageChartViewModel : ViewModelBase
{
    public static IReadOnlyList<UsageChartRange> RangeOptions { get; } =
    [
        UsageChartRange.OneDay,
        UsageChartRange.TwoDays,
        UsageChartRange.SevenDays,
        UsageChartRange.OneWeek,
        UsageChartRange.TwoWeeks,
        UsageChartRange.OneMonth
    ];

    private UsageChartDocument? _document;
    private UsageChartRange _selectedRange = UsageChartRange.OneMonth;
    private UsageChartViewport? _customViewport;
    private DateTime? _cycleStart;
    private DateTime? _nextRenewal;

    public UsageChartDocument? Document
    {
        get => _document;
        private set => SetProperty(ref _document, value);
    }

    public UsageChartRange SelectedRange
    {
        get => _selectedRange;
        set => SelectRange(value);
    }

    public UsageChartViewport? CustomViewport
    {
        get => _customViewport;
        set
        {
            var next = Normalize(value);
            if (Nullable.Equals(_customViewport, next))
                return;

            _customViewport = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCustomViewport));
            ViewportChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsCustomViewport => _customViewport.HasValue;

    public event EventHandler? RangeChanged;

    public event EventHandler? ViewportChanged;

    public void Replace(UsageChartDocument? document) => Document = document;

    public void SelectRange(UsageChartRange range)
    {
        var rangeChanged = _selectedRange != range;
        var hadViewport = _customViewport.HasValue;
        if (!rangeChanged && !hadViewport)
            return;

        if (rangeChanged)
        {
            _selectedRange = range;
            OnPropertyChanged(nameof(SelectedRange));
        }

        if (hadViewport)
        {
            _customViewport = null;
            OnPropertyChanged(nameof(CustomViewport));
            OnPropertyChanged(nameof(IsCustomViewport));
        }

        RangeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyDisplayedCycle(DateTime? cycleStart, DateTime? nextRenewal)
    {
        var sameCycle = _cycleStart == cycleStart && _nextRenewal == nextRenewal;
        _cycleStart = cycleStart;
        _nextRenewal = nextRenewal;
        if (sameCycle || _customViewport is null)
            return;

        _customViewport = null;
        OnPropertyChanged(nameof(CustomViewport));
        OnPropertyChanged(nameof(IsCustomViewport));
    }

    private static UsageChartViewport? Normalize(UsageChartViewport? viewport)
    {
        if (viewport is not { } value)
            return null;

        var start = value.StartX < value.EndX ? value.StartX : value.EndX;
        var end = value.StartX < value.EndX ? value.EndX : value.StartX;
        return end > start ? new UsageChartViewport(start, end) : null;
    }
}
