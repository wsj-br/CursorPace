using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using CursorPace.Models;
using CursorPace.Services;
using CursorPace.ViewModels;

namespace CursorPace.Views;

public partial class SettingsView : UserControl
{
    private MainViewModel? _viewModel;
    private bool _applyingSettingsTab;
    private bool _settingsTabReady;

    private bool _applyingLanguage;
    private bool _localeRefreshPosted;

    public SettingsView()
    {
        InitializeComponent();
        RestoreButton.Content = Tp("backup", "Restore");
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
        LocaleChanged += OnLocaleChanged;
    }

    private Window? HostWindow => TopLevel.GetTopLevel(this) as Window;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        HookViewModel(DataContext as MainViewModel);
        _settingsTabReady = true;
        ApplySettingsTab();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _settingsTabReady = false;
        HookViewModel(null);
    }

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
        ApplySettingsTab();
        SyncIntervalBox();
        SyncThemeBox();
        SyncSampleDetailBox();
        SyncLanguageBox();
        if (RestoreButton != null)
            RestoreButton.Content = Tp("backup", "Restore");
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SyncIntervalHours))
            SyncIntervalBox();
        else if (e.PropertyName == nameof(MainViewModel.ThemeMode))
            SyncThemeBox();
        else if (e.PropertyName == nameof(MainViewModel.RawSampleMaxDays))
            SyncSampleDetailBox();
        else if (e.PropertyName is nameof(MainViewModel.SettingsTab) or nameof(MainViewModel.IsSettingsView))
            ApplySettingsTab();
    }

    private void ApplySettingsTab()
    {
        if (_viewModel == null || SettingsTabs == null)
            return;

        var index = SettingsTabIds.ToIndex(_viewModel.SettingsTab);
        if (SettingsTabs.SelectedIndex == index)
            return;

        _applyingSettingsTab = true;
        try
        {
            SettingsTabs.SelectedIndex = index;
        }
        finally
        {
            _applyingSettingsTab = false;
        }
    }

    private void OnSettingsTabChanged(object? sender, SelectionChangedEventArgs e)
    {
        // ComboBox selection changes bubble. Refreshing a combo from that event
        // replaces its items while the selection update is still on the stack.
        if (!ReferenceEquals(e.Source, SettingsTabs))
            return;

        SyncIntervalBox();
        SyncThemeBox();
        SyncSampleDetailBox();
        SyncLanguageBox();

        if (!_settingsTabReady || _applyingSettingsTab || _viewModel == null)
            return;

        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not TabItem)
            return;

        if (SettingsTabs.SelectedIndex < 0)
            return;

        _viewModel.SettingsTab = SettingsTabIds.FromIndex(SettingsTabs.SelectedIndex);
    }

    private void SyncIntervalBox()
    {
        if (_viewModel == null || IntervalBox == null)
            return;

        IntervalBox.ItemsSource = _viewModel.SyncIntervalOptions;
        IntervalBox.SelectedItem = _viewModel.SyncIntervalHours;
    }

    private void SyncThemeBox()
    {
        if (_viewModel == null || ThemeBox == null)
            return;

        var options = _viewModel.ThemeModeOptions;
        ThemeBox.ItemsSource = options;
        ThemeBox.SelectedItem = options.First(option => option.Mode == _viewModel.ThemeMode);
    }

    private void SyncSampleDetailBox()
    {
        if (_viewModel == null || SampleDetailBox == null)
            return;

        var options = _viewModel.SampleDetailOptions;
        SampleDetailBox.ItemsSource = options;
        SampleDetailBox.SelectedItem = options.First(option => option.Days == _viewModel.RawSampleMaxDays);
    }

    private void SyncLanguageBox()
    {
        if (_viewModel == null || LanguageBox == null)
            return;

        var options = _viewModel.LanguageOptions;
        _applyingLanguage = true;
        try
        {
            LanguageBox.ItemsSource = options;
            LanguageBox.SelectedItem = options.First(option => option.Code == _viewModel.UiLocale);
        }
        finally
        {
            _applyingLanguage = false;
        }
    }

    private void OnLocaleChanged()
    {
        if (_localeRefreshPosted)
            return;

        _localeRefreshPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _localeRefreshPosted = false;
            SyncThemeBox();
            SyncSampleDetailBox();
            SyncLanguageBox();
            UpdateTokenRevealCaption();
            if (RestoreButton != null)
                RestoreButton.Content = Tp("backup", "Restore");
        });
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        if (!_viewModel.TryBuildCycleCsv(out var csv))
        {
            await ShowMessageAsync(T("Export failed"), T("There is no cycle data to export."));
            return;
        }

        await SaveTextFileAsync(
            T("Export CSV"),
            _viewModel.SuggestedCycleFileName,
            "csv",
            T("CSV"),
            csv);
    }

    private async void OnExportUsageClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        if (!_viewModel.TryBuildUsageSamplesCsv(out var csv))
        {
            await ShowMessageAsync(T("Export failed"), T("There is no usage data to export."));
            return;
        }

        await SaveTextFileAsync(
            T("Export CSV"),
            _viewModel.SuggestedUsageSamplesFileName,
            "csv",
            T("CSV"),
            csv);
    }

    private async void OnBackupClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        try
        {
            var file = await PickSaveFileAsync(
                T("Backup data"),
                _viewModel.SuggestedBackupFileName,
                "zip",
                T("Backup"),
                "*.zip");
            if (file == null)
                return;

            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await using var stream = await file.OpenWriteAsync();
                if (!_viewModel.TryWriteBackup(stream, out var error))
                {
                    await ShowMessageAsync(T("Backup failed"), UiText.Translate(error ?? "Could not write the backup file."));
                    return;
                }

                await ShowSavedFileAsync(T("Backup complete"), T("Saved the backup file."));
                return;
            }

            await using (var stream = File.Create(path))
            {
                if (!_viewModel.TryWriteBackup(stream, out var error))
                {
                    await ShowMessageAsync(T("Backup failed"), UiText.Translate(error ?? "Could not write the backup file."));
                    return;
                }
            }

            await ShowSavedFileAsync(T("Backup complete"), T("Saved to {0}", path), path);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                T("Backup failed"),
                UiText.Translate(string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not write the backup file."
                    : ex.Message));
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
                Title = T("Restore backup"),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(T("Backup")) { Patterns = ["*.zip"] }
                ]
            });
            if (files.Count == 0)
                return;

            var confirmed = await ConfirmDialog.ConfirmAsync(
                window,
                T("Restore backup"),
                T("This replaces the current settings and collected usage samples with the backup. Your Cursor sign-in session in this app is not changed."),
                Tp("backup", "Restore"),
                T("Cancel"));
            if (!confirmed)
                return;

            var file = files[0];
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await using var stream = await file.OpenReadAsync();
                if (!_viewModel.TryRestoreBackup(stream, out var error))
                {
                    await ShowMessageAsync(T("Restore failed"), UiText.Translate(error ?? "Could not restore the backup file."));
                    return;
                }

                await ShowMessageAsync(T("Restore complete"), T("Restored settings and usage samples."));
                return;
            }

            await using (var stream = File.OpenRead(path))
            {
                if (!_viewModel.TryRestoreBackup(stream, out var error))
                {
                    await ShowMessageAsync(T("Restore failed"), UiText.Translate(error ?? "Could not restore the backup file."));
                    return;
                }
            }

            await ShowMessageAsync(T("Restore complete"), T("Restored settings and usage samples."));
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                T("Restore failed"),
                UiText.Translate(string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not restore the backup file."
                    : ex.Message));
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
            await ShowSavedFileAsync(T("Export complete"), T("Saved the {0} file.", fileTypeName));
            return;
        }

        await File.WriteAllTextAsync(path, contents);
        await ShowSavedFileAsync(T("Export complete"), T("Saved to {0}", path), path);
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
            T("Open Folder"));
        if (openFolder && !FolderOpener.TryOpenContainingFolder(localPath, out var error))
        {
            await ConfirmDialog.ShowMessageAsync(
                window,
                T("Open folder failed"),
                UiText.Translate(error ?? "Could not open the destination folder."));
        }
    }

    private void OnIntervalChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && IntervalBox.SelectedItem is int hours)
            _viewModel.SyncIntervalHours = hours;
    }

    private void OnToggleRemoteSyncApiTokenClick(object? sender, RoutedEventArgs e)
    {
        var reveal = RemoteSyncApiTokenBox.PasswordChar == '•';
        RemoteSyncApiTokenBox.PasswordChar = reveal ? '\0' : '•';
        ShowRemoteSyncApiTokenIcon.IsVisible = !reveal;
        HideRemoteSyncApiTokenIcon.IsVisible = reveal;
        UpdateTokenRevealCaption();
    }

    private void UpdateTokenRevealCaption()
    {
        if (RemoteSyncApiTokenBox == null || RemoteSyncApiTokenRevealButton == null)
            return;

        var reveal = RemoteSyncApiTokenBox.PasswordChar == '\0';
        RemoteSyncApiTokenRevealButton.SetValue(
            ToolTip.TipProperty,
            reveal ? T("Hide token") : T("Show token"));
        AutomationProperties.SetName(
            RemoteSyncApiTokenRevealButton,
            reveal ? T("Hide API token") : T("Show API token"));
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && ThemeBox.SelectedItem is ThemeModeChoice choice)
            _viewModel.ThemeMode = choice.Mode;
    }

    private void OnSampleDetailChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && SampleDetailBox.SelectedItem is SampleDetailOption option)
            _viewModel.RawSampleMaxDays = option.Days;
    }

    private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_applyingLanguage || _viewModel == null || LanguageBox.SelectedItem is not LanguageChoice choice)
            return;
        _viewModel.UiLocale = choice.Code;
    }

    private async void OnDisconnectClick(object? sender, RoutedEventArgs e)
    {
        var window = HostWindow;
        if (window == null || _viewModel == null)
            return;

        var confirmed = await ConfirmDialog.ConfirmAsync(
            window,
            T("Sign out of Cursor"),
            T("This signs out of Cursor in this app. Any Google or GitHub session stored in the app's private browser profile is kept when possible, so signing in again may not ask for that password. Your regular browser is not affected. Collected usage samples stay on disk."),
            T("Sign out"),
            T("Cancel"));
        if (confirmed && _viewModel.DisconnectCommand.CanExecute(null))
            _viewModel.DisconnectCommand.Execute(null);
    }
}
