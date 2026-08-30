using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace CursorUsageProgress.Views;

internal static class SelectableTextCopy
{
    public static bool TryHandleCopyKey(Control root, KeyEventArgs e)
    {
        if (e.Key != Key.C || (e.KeyModifiers & KeyModifiers.Control) == 0)
            return false;

        if (TryCopyFromFocused(root))
            return true;

        return TryCopyFromAnySelection(root);
    }

    private static bool TryCopyFromFocused(Control root)
    {
        if (TopLevel.GetTopLevel(root)?.FocusManager?.GetFocusedElement() is not SelectableTextBlock focused)
            return false;

        if (!focused.CanCopy)
            return false;

        focused.Copy();
        return true;
    }

    private static bool TryCopyFromAnySelection(Control root)
    {
        foreach (var block in root.GetVisualDescendants().OfType<SelectableTextBlock>())
        {
            if (!block.CanCopy)
                continue;

            block.Copy();
            return true;
        }

        return false;
    }
}
