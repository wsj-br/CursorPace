using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Views;

public sealed class RenewalDayDialog : ContentDialog
{
    private readonly ICycleCalculator _calculator;
    private readonly IClock _clock;

    private readonly NumberBox _dayBox;
    private readonly TextBlock _previewText;

    public int? RenewalDay { get; private set; }

    public RenewalDayDialog(ICycleCalculator calculator, IClock clock, int defaultDay = 15)
    {
        _calculator = calculator;
        _clock = clock;

        Title = "Set renewal day";
        PrimaryButtonText = "OK";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;

        _dayBox = new NumberBox
        {
            Minimum = 1,
            Maximum = 31,
            Value = defaultDay,
            SmallChange = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Header = "Renewal day (1–31)",
        };

        _previewText = new TextBlock
        {
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
            IsTextSelectionEnabled = true
        };

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = "Select the day of the month your Cursor quota renews.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            IsTextSelectionEnabled = true
        });
        panel.Children.Add(_dayBox);
        panel.Children.Add(_previewText);

        Content = panel;

        _dayBox.ValueChanged += (_, _) => UpdatePreview();
        UpdatePreview();

        PrimaryButtonClick += OnOkClick;
    }

    private void UpdatePreview()
    {
        var day = (int)_dayBox.Value;
        if (day < 1 || day > 31)
        {
            _previewText.Text = string.Empty;
            return;
        }

        try
        {
            var cycle = _calculator.GenerateCycle(day, _clock.Today);
            _previewText.Text =
                $"Current cycle: {cycle.CycleStart.ToString("d", CultureInfo.CurrentCulture)}" +
                $" – {cycle.NextRenewal.ToString("d", CultureInfo.CurrentCulture)}" +
                $" ({cycle.Days.Count} days)";
        }
        catch
        {
            _previewText.Text = "Preview unavailable";
        }
    }

    private void OnOkClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var day = (int)_dayBox.Value;
        if (day >= 1 && day <= 31)
        {
            RenewalDay = day;
        }
        else
        {
            args.Cancel = true;
        }
    }
}
