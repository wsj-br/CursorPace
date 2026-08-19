using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Views;

public sealed partial class CalendarControl : UserControl
{
    public static readonly DependencyProperty WeeksProperty =
        DependencyProperty.Register(
            nameof(Weeks),
            typeof(ObservableCollection<CalendarWeekViewModel>),
            typeof(CalendarControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MonthHeadingProperty =
        DependencyProperty.Register(
            nameof(MonthHeading),
            typeof(string),
            typeof(CalendarControl),
            new PropertyMetadata(string.Empty, OnMonthHeadingChanged));

    public CalendarControl()
    {
        InitializeComponent();
        DayNames = BuildDayNames();
    }

    public ObservableCollection<CalendarWeekViewModel> Weeks
    {
        get => (ObservableCollection<CalendarWeekViewModel>)GetValue(WeeksProperty);
        set => SetValue(WeeksProperty, value);
    }

    public string MonthHeading
    {
        get => (string)GetValue(MonthHeadingProperty);
        set => SetValue(MonthHeadingProperty, value);
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

    private static void OnMonthHeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CalendarControl)d;
        if (control.MonthHeadingText != null)
            control.MonthHeadingText.Text = e.NewValue as string ?? string.Empty;
    }
}
