using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CursorUsageProgress.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
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
        return dialog.ShowDialog(owner);
    }

    private void OnPrimaryClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnSecondaryClick(object? sender, RoutedEventArgs e) => Close(false);
}
