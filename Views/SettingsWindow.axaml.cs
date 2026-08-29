using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel = null!;

    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        RootGrid.Loaded += (_, _) => TextBlockSelection.EnableOnLabels(RootGrid, AppTitleBar);

        IntervalBox.ItemsSource = viewModel.SyncIntervalOptions;
        IntervalBox.SelectedItem = viewModel.SyncIntervalHours;
        if (OperatingSystem.IsMacOS())
            AppTitleBar.Padding = new Avalonia.Thickness(78, 0, 12, 0);
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryBuildCycleCsv(out var csv))
        {
            await ConfirmDialog.ShowMessageAsync(this, "Export failed", "There is no cycle data to export.");
            return;
        }

        await SaveCsvAsync($"cursor-usage-progress-{DateTime.Today:yyyy-MM-dd}", csv);
    }

    private async void OnExportUsageClick(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryBuildUsageSamplesCsv(out var csv))
        {
            await ConfirmDialog.ShowMessageAsync(this, "Export failed", "There is no usage data to export.");
            return;
        }

        await SaveCsvAsync($"usage-samples-{DateTime.Today:yyyy-MM-dd}", csv);
    }

    private async Task SaveCsvAsync(string suggestedName, string csv)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export CSV",
            SuggestedFileName = suggestedName,
            DefaultExtension = "csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV") { Patterns = ["*.csv"] }
            ]
        });
        if (file == null)
            return;

        var path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(csv);
            await ConfirmDialog.ShowMessageAsync(this, "Export complete", "Saved the CSV file.");
            return;
        }

        await File.WriteAllTextAsync(path, csv);
        await ConfirmDialog.ShowMessageAsync(this, "Export complete", $"Saved to {path}");
    }

    private void OnIntervalChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IntervalBox.SelectedItem is int hours)
            _viewModel.SyncIntervalHours = hours;
    }

    private async void OnDisconnectClick(object? sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmDialog.ConfirmAsync(
            this,
            "Sign out of Cursor",
            "This signs out of Cursor in this app, including any Google or GitHub session stored in the app's private browser profile. Your regular browser is not affected. Collected usage samples stay on disk.",
            "Sign out",
            "Cancel");
        if (confirmed && _viewModel.DisconnectCommand.CanExecute(null))
            _viewModel.DisconnectCommand.Execute(null);
    }
}
