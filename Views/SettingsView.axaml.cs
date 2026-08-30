using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CursorPace.Models;
using CursorPace.Services;
using CursorPace.ViewModels;

namespace CursorPace.Views;

public partial class SettingsView : UserControl
{
    private MainViewModel? _viewModel;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private Window? HostWindow => TopLevel.GetTopLevel(this) as Window;

    private void OnLoaded(object? sender, RoutedEventArgs e) =>
        HookViewModel(DataContext as MainViewModel);

    private void OnUnloaded(object? sender, RoutedEventArgs e) => HookViewModel(null);

    private void OnDataContextChanged(object? sender, EventArgs e) =>
        HookViewModel(DataContext as MainViewModel);

    private void HookViewModel(MainViewModel? viewModel)
    {
        if (_viewModel == viewModel)
            return;

        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = viewModel;
        if (_viewModel == null)
            return;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        SyncIntervalBox();
        SyncThemeBox();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SyncIntervalHours))
            SyncIntervalBox();
        else if (e.PropertyName == nameof(MainViewModel.ThemeMode))
            SyncThemeBox();
    }

    private void SyncIntervalBox()
    {
        if (_viewModel == null)
            return;

        IntervalBox.ItemsSource = _viewModel.SyncIntervalOptions;
        IntervalBox.SelectedItem = _viewModel.SyncIntervalHours;
    }

    private void SyncThemeBox()
    {
        if (_viewModel == null)
            return;

        ThemeBox.ItemsSource = _viewModel.ThemeModeOptions;
        ThemeBox.SelectedItem = _viewModel.ThemeMode;
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        if (!_viewModel.TryBuildCycleCsv(out var csv))
        {
            await ShowMessageAsync("Export failed", "There is no cycle data to export.");
            return;
        }

        await SaveTextFileAsync(
            "Export CSV",
            _viewModel.SuggestedCycleFileName,
            "csv",
            "CSV",
            csv);
    }

    private async void OnExportUsageClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        if (!_viewModel.TryBuildUsageSamplesCsv(out var csv))
        {
            await ShowMessageAsync("Export failed", "There is no usage data to export.");
            return;
        }

        await SaveTextFileAsync(
            "Export CSV",
            _viewModel.SuggestedUsageSamplesFileName,
            "csv",
            "CSV",
            csv);
    }

    private async void OnBackupClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        try
        {
        var file = await PickSaveFileAsync(
            "Backup data",
            _viewModel.SuggestedBackupFileName,
            "zip",
            "Backup",
            "*.zip");
        if (file == null)
            return;

        var path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            await using var stream = await file.OpenWriteAsync();
            if (!_viewModel.TryWriteBackup(stream, out var error))
            {
                await ShowMessageAsync("Backup failed", error ?? "Could not write the backup file.");
                return;
            }

            await ShowSavedFileAsync("Backup complete", "Saved the backup file.");
            return;
        }

        await using (var stream = File.Create(path))
        {
            if (!_viewModel.TryWriteBackup(stream, out var error))
            {
                await ShowMessageAsync("Backup failed", error ?? "Could not write the backup file.");
                return;
            }
        }

        await ShowSavedFileAsync("Backup complete", $"Saved to {path}", path);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Backup failed",
                string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not write the backup file."
                    : ex.Message);
        }
    }

    private async void OnRestoreClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        try
        {
        var window = HostWindow;
        if (window == null)
            return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Restore backup",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Backup") { Patterns = ["*.zip"] }
            ]
        });
        if (files.Count == 0)
            return;

        var confirmed = await ConfirmDialog.ConfirmAsync(
            window,
            "Restore backup",
            "This replaces the current settings and collected usage samples with the backup. Your Cursor sign-in session in this app is not changed.",
            "Restore",
            "Cancel");
        if (!confirmed)
            return;

        var file = files[0];
        var path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            await using var stream = await file.OpenReadAsync();
            if (!_viewModel.TryRestoreBackup(stream, out var error))
            {
                await ShowMessageAsync("Restore failed", error ?? "Could not restore the backup file.");
                return;
            }

            await ShowMessageAsync("Restore complete", "Restored settings and usage samples.");
            return;
        }

        await using (var stream = File.OpenRead(path))
        {
            if (!_viewModel.TryRestoreBackup(stream, out var error))
            {
                await ShowMessageAsync("Restore failed", error ?? "Could not restore the backup file.");
                return;
            }
        }

        await ShowMessageAsync("Restore complete", "Restored settings and usage samples.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Restore failed",
                string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not restore the backup file."
                    : ex.Message);
        }
    }

    private async Task SaveTextFileAsync(
        string title,
        string suggestedName,
        string extension,
        string fileTypeName,
        string contents)
    {
        var file = await PickSaveFileAsync(title, suggestedName, extension, fileTypeName, $"*.{extension}");
        if (file == null)
            return;

        var path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(contents);
            await ShowSavedFileAsync("Export complete", $"Saved the {fileTypeName} file.");
            return;
        }

        await File.WriteAllTextAsync(path, contents);
        await ShowSavedFileAsync("Export complete", $"Saved to {path}", path);
    }

    private async Task<IStorageFile?> PickSaveFileAsync(
        string title,
        string suggestedName,
        string extension,
        string fileTypeName,
        string pattern)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return null;

        return await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices =
            [
                new FilePickerFileType(fileTypeName) { Patterns = [pattern] }
            ]
        });
    }

    private async Task ShowMessageAsync(string title, string body)
    {
        var window = HostWindow;
        if (window == null)
            return;
        await ConfirmDialog.ShowMessageAsync(window, title, body);
    }

    private async Task ShowSavedFileAsync(string title, string body, string? localPath = null)
    {
        var window = HostWindow;
        if (window == null)
            return;

        if (string.IsNullOrEmpty(localPath))
        {
            await ConfirmDialog.ShowMessageAsync(window, title, body);
            return;
        }

        var openFolder = await ConfirmDialog.ShowMessageWithActionAsync(
            window,
            title,
            body,
            "Open Folder");
        if (openFolder && !FolderOpener.TryOpenContainingFolder(localPath, out var error))
        {
            await ConfirmDialog.ShowMessageAsync(
                window,
                "Open folder failed",
                error ?? "Could not open the destination folder.");
        }
    }

    private void OnIntervalChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && IntervalBox.SelectedItem is int hours)
            _viewModel.SyncIntervalHours = hours;
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && ThemeBox.SelectedItem is UiThemeMode mode)
            _viewModel.ThemeMode = mode;
    }

    private async void OnDisconnectClick(object? sender, RoutedEventArgs e)
    {
        var window = HostWindow;
        if (window == null || _viewModel == null)
            return;

        var confirmed = await ConfirmDialog.ConfirmAsync(
            window,
            "Sign out of Cursor",
            "This signs out of Cursor in this app. Any Google or GitHub session stored in the app's private browser profile is kept when possible, so signing in again may not ask for that password. Your regular browser is not affected. Collected usage samples stay on disk.",
            "Sign out",
            "Cancel");
        if (confirmed && _viewModel.DisconnectCommand.CanExecute(null))
            _viewModel.DisconnectCommand.Execute(null);
    }
}
