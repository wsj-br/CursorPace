using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using CursorPace.ViewModels;

namespace CursorPace.Views;

public partial class CalendarControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<CalendarWeekViewModel>?> WeeksProperty =
        AvaloniaProperty.Register<CalendarControl, ObservableCollection<CalendarWeekViewModel>?>(nameof(Weeks));

    public static readonly StyledProperty<string> MonthHeadingProperty =
        AvaloniaProperty.Register<CalendarControl, string>(nameof(MonthHeading), string.Empty);

    public static readonly StyledProperty<string> LastSyncTextProperty =
        AvaloniaProperty.Register<CalendarControl, string>(nameof(LastSyncText), string.Empty);

    public static readonly StyledProperty<bool> IsSyncingProperty =
        AvaloniaProperty.Register<CalendarControl, bool>(nameof(IsSyncing));

    public static readonly StyledProperty<bool> ShowLastSyncProperty =
        AvaloniaProperty.Register<CalendarControl, bool>(nameof(ShowLastSync), true);

    public CalendarControl()
    {
        InitializeComponent();
        DayNames = BuildDayNames();
        DayNamesList.ItemsSource = DayNames;
        MonthHeadingProperty.Changed.AddClassHandler<CalendarControl>(OnMonthHeadingChanged);
    }

    public ObservableCollection<CalendarWeekViewModel>? Weeks
    {
        get => GetValue(WeeksProperty);
        set => SetValue(WeeksProperty, value);
    }

    public string MonthHeading
    {
        get => GetValue(MonthHeadingProperty);
        set => SetValue(MonthHeadingProperty, value);
    }

    public string LastSyncText
    {
        get => GetValue(LastSyncTextProperty);
        set => SetValue(LastSyncTextProperty, value);
    }

    public bool IsSyncing
    {
        get => GetValue(IsSyncingProperty);
        set => SetValue(IsSyncingProperty, value);
    }

    public bool ShowLastSync
    {
        get => GetValue(ShowLastSyncProperty);
        set => SetValue(ShowLastSyncProperty, value);
    }

    public List<string> DayNames { get; }

    private static List<string> BuildDayNames()
    {
        var format = CultureInfo.CurrentCulture.DateTimeFormat;
        var first = (int)format.FirstDayOfWeek;
        var names = new List<string>(7);
        for (var i = 0; i < 7; i++)
            names.Add(format.AbbreviatedDayNames[(first + i) % 7]);
        return names;
    }

    private static void OnMonthHeadingChanged(CalendarControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (control.MonthHeadingText != null)
            control.MonthHeadingText.Text = e.NewValue as string ?? string.Empty;
    }
}
