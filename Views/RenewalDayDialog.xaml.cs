using System.Globalization;
using System.Windows;
using CursorQuotaProgress.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace CursorQuotaProgress.Views;

public partial class RenewalDayDialog : Window
{
    private readonly ICycleCalculator _calculator;
    private readonly IClock _clock;

    public int? RenewalDay { get; private set; }

    public RenewalDayDialog(ICycleCalculator calculator, IClock clock, int defaultDay = 15)
    {
        InitializeComponent();

        _calculator = calculator;
        _clock = clock;

        RenewalDayTextBox.Text = defaultDay.ToString();
        RenewalDayTextBox.TextChanged += (s, e) => UpdatePreview();

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (int.TryParse(RenewalDayTextBox.Text, out int day) && day >= 1 && day <= 31)
        {
            try
            {
                var cycle = _calculator.GenerateCycle(day, _clock.Today);
                var nextRenewal = cycle.NextRenewal;

                PreviewText.Text = $"Current cycle: {cycle.CycleStart.ToString("d", CultureInfo.CurrentCulture)} - {nextRenewal.ToString("d", CultureInfo.CurrentCulture)} ({cycle.Days.Count} days)";
            }
            catch
            {
                PreviewText.Text = "Preview unavailable";
            }
        }
        else
        {
            PreviewText.Text = "Please enter a day between 1 and 31";
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(RenewalDayTextBox.Text, out int day) && day >= 1 && day <= 31)
        {
            RenewalDay = day;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please enter a valid day between 1 and 31", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
