using Avalonia.Controls;
using Avalonia.Media;

namespace CursorUsageProgress.Views;

internal static class TextBlockSelection
{
    public static SelectableTextBlock Message(string text) =>
        new()
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };
}
