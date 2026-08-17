using System.IO;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using CursorQuotaProgress.ViewModels;

namespace CursorQuotaProgress.Views;

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

        if (ownerWindow != null)
            CenterOver(ownerWindow);
    }

    private void SetupWindow()
    {
        Title = "Settings";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 560));

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
            SuggestedFileName = $"cursor-quota-progress-{DateTime.Today:yyyy-MM-dd}",
            DefaultFileExtension = ".csv"
        };
        picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" });

        var result = await picker.PickSaveFileAsync();
        if (result == null)
            return;

        await File.WriteAllTextAsync(result.Path, csv);

        await ShowMessageAsync("Export complete", $"Saved to {result.Path}");
    }

    private async Task ShowMessageAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
