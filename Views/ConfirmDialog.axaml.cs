using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace CursorUsageProgress.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Opened += (_, _) => FocusSafeButton();
    }

    public static Task<bool> ConfirmAsync(Window owner, string title, string body, string primaryText, string secondaryText)
    {
        var dialog = new ConfirmDialog();
        dialog.Title = title;
        dialog.TitleText.Text = title;
        dialog.BodyText.Text = body;
        dialog.PrimaryButton.Content = primaryText;
        dialog.SecondaryButton.Content = secondaryText;
        dialog.SecondaryButton.IsDefault = true;
        dialog.ArrangeButtons(confirm: true);
        return dialog.ShowDialog<bool>(owner);
    }

    public static Task ShowMessageAsync(Window owner, string title, string body)
    {
        var dialog = new ConfirmDialog();
        dialog.Title = title;
        dialog.TitleText.Text = title;
        dialog.BodyText.Text = body;
        dialog.PrimaryButton.Content = "OK";
        dialog.PrimaryButton.IsDefault = true;
        dialog.SecondaryButton.IsVisible = false;
        dialog.ArrangeButtons(confirm: false);
        return dialog.ShowDialog(owner);
    }

    public static Task<bool> ShowMessageWithActionAsync(
        Window owner,
        string title,
        string body,
        string actionText,
        string dismissText = "OK")
    {
        var dialog = new ConfirmDialog();
        dialog.Title = title;
        dialog.TitleText.Text = title;
        dialog.BodyText.Text = body;
        dialog.PrimaryButton.Content = actionText;
        dialog.SecondaryButton.Content = dismissText;
        dialog.SecondaryButton.IsDefault = true;
        dialog.ArrangeActionButtons();
        return dialog.ShowDialog<bool>(owner);
    }

    private void ArrangeActionButtons()
    {
        Grid.SetColumn(PrimaryButton, 0);
        Grid.SetColumn(SecondaryButton, 1);
        PrimaryButton.HorizontalAlignment = HorizontalAlignment.Left;
        SecondaryButton.HorizontalAlignment = HorizontalAlignment.Right;
    }

    private void ArrangeButtons(bool confirm)
    {
        if (!confirm)
        {
            Grid.SetColumn(PrimaryButton, 1);
            PrimaryButton.HorizontalAlignment = HorizontalAlignment.Right;
            return;
        }

        var primaryOnLeft = !OperatingSystem.IsWindows();
        Grid.SetColumn(PrimaryButton, primaryOnLeft ? 1 : 0);
        Grid.SetColumn(SecondaryButton, primaryOnLeft ? 0 : 1);
        PrimaryButton.HorizontalAlignment = primaryOnLeft
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Left;
        SecondaryButton.HorizontalAlignment = primaryOnLeft
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Right;
    }

    private void FocusSafeButton()
    {
        if (SecondaryButton.IsVisible)
            SecondaryButton.Focus();
        else
            PrimaryButton.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Close(SecondaryButton.IsVisible ? false : true);
        e.Handled = true;
    }

    private void OnPrimaryClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnSecondaryClick(object? sender, RoutedEventArgs e) => Close(false);
}
