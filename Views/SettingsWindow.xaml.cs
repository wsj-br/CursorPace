using System.IO;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;

    public SettingsWindow(MainViewModel viewModel, AppWindow? ownerWindow = null)
    {
        InitializeComponent();

        _viewModel = viewModel;
        RootGrid.DataContext = viewModel;

        SetupWindow();
        SetupTheme();
        RootGrid.Loaded += (_, _) => TextBlockSelection.EnableOnLabels(RootGrid, AppTitleBar);

        IntervalBox.ItemsSource = viewModel.SyncIntervalOptions;
        IntervalBox.SelectedItem = viewModel.SyncIntervalHours;

        if (ownerWindow != null)
            CenterOver(ownerWindow);
    }

    private void SetupWindow()
    {
        Title = "Settings";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 620));

        // Mica backdrop (Windows 11), Acrylic fallback on Windows 10
        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
        else
            SystemBackdrop = new DesktopAcrylicBackdrop();
    }

    private void CenterOver(AppWindow ownerWindow)
    {
        var size = AppWindow.Size;
        var ownerPos = ownerWindow.Position;
        var ownerSize = ownerWindow.Size;
        var x = ownerPos.X + (ownerSize.Width - size.Width) / 2;
        var y = ownerPos.Y + (ownerSize.Height - size.Height) / 2;
        AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }

    private void SetupTheme()
    {
        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        ApplySystemTheme(uiSettings);
        uiSettings.ColorValuesChanged += (s, _) =>
            DispatcherQueue.TryEnqueue(() => ApplySystemTheme(s));
    }

    private void ApplySystemTheme(Windows.UI.ViewManagement.UISettings settings)
    {
        var bg = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var isDark = bg.R < 128;
        RootGrid.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    private async void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryBuildCycleCsv(out var csv))
        {
            await ShowMessageAsync("Export failed", "There is no cycle data to export.");
            return;
        }

        var picker = new FileSavePicker(AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"cursor-usage-progress-{DateTime.Today:yyyy-MM-dd}",
            DefaultFileExtension = ".csv"
        };
        picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" });

        var result = await picker.PickSaveFileAsync();
        if (result == null)
            return;

        await File.WriteAllTextAsync(result.Path, csv);

        await ShowMessageAsync("Export complete", $"Saved to {result.Path}");
    }

    private async void OnExportUsageClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryBuildUsageSamplesCsv(out var csv))
        {
            await ShowMessageAsync("Export failed", "There is no usage data to export.");
            return;
        }

        var picker = new FileSavePicker(AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"usage-samples-{DateTime.Today:yyyy-MM-dd}",
            DefaultFileExtension = ".csv"
        };
        picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" });

        var result = await picker.PickSaveFileAsync();
        if (result == null)
            return;

        await File.WriteAllTextAsync(result.Path, csv);

        await ShowMessageAsync("Export complete", $"Saved to {result.Path}");
    }

    private void OnIntervalChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IntervalBox.SelectedItem is int hours)
            _viewModel.SyncIntervalHours = hours;
    }

    private async void OnDisconnectClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Sign out of Cursor",
            Content = TextBlockSelection.Message(
                "This signs out of Cursor in this app, including any Google or GitHub session stored in the app's private browser profile. Your regular browser is not affected. Collected usage samples stay on disk."),
            PrimaryButtonText = "Sign out",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = RootGrid.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary
            && _viewModel.DisconnectCommand.CanExecute(null))
        {
            _viewModel.DisconnectCommand.Execute(null);
        }
    }

    private async Task ShowMessageAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = TextBlockSelection.Message(content),
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
