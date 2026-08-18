using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace CursorUsageProgress.Views;

internal static class TextBlockSelection
{
    public static void EnableOnLabels(DependencyObject root, DependencyObject? exclude = null)
    {
        Apply(root, exclude);
    }

    public static TextBlock Message(string text) =>
        new()
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };

    private static void Apply(DependencyObject current, DependencyObject? exclude)
    {
        var count = VisualTreeHelper.GetChildrenCount(current);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(current, i);
            if (exclude is not null && ReferenceEquals(child, exclude))
                continue;
            if (child is ButtonBase)
                continue;
            if (child is CalendarControl or WebView2)
                continue;
            if (child is TextBlock textBlock)
                textBlock.IsTextSelectionEnabled = true;
            Apply(child, exclude);
        }
    }
}
