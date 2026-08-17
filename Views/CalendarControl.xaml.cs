using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using CursorQuotaProgress.ViewModels;

namespace CursorQuotaProgress.Views;

public sealed partial class CalendarControl : UserControl
{
    public static readonly DependencyProperty WeeksProperty =
        DependencyProperty.Register(
            nameof(Weeks),
            typeof(ObservableCollection<CalendarWeekViewModel>),
            typeof(CalendarControl),
            new PropertyMetadata(null));

    public event EventHandler<CalendarCellViewModel>? CellSelected;

    public CalendarControl()
    {
        InitializeComponent();

        // Set day names for header
        DayNames = new List<string> { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    }

    public ObservableCollection<CalendarWeekViewModel> Weeks
    {
        get => (ObservableCollection<CalendarWeekViewModel>)GetValue(WeeksProperty);
        set => SetValue(WeeksProperty, value);
    }

    public List<string> DayNames { get; }

    private void OnCellPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is CalendarCellViewModel cell)
        {
            if (cell.DayData != null) // Only select cells with data
            {
                CellSelected?.Invoke(this, cell);
            }
        }
    }
}
