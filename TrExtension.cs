using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace CursorPace;

public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string text)
    {
        Text = text;
    }

    public string Text { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var translated = I18n.T(Text);
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target
            && target.TargetObject is AvaloniaObject avalonia
            && target.TargetProperty is AvaloniaProperty property)
        {
            void OnLocaleChanged() => avalonia.SetValue(property, I18n.T(Text));
            I18n.LocaleChanged += OnLocaleChanged;
            if (avalonia is Control control)
                control.DetachedFromVisualTree += (_, _) => I18n.LocaleChanged -= OnLocaleChanged;
        }

        return translated;
    }

    public static void ApplyFlowDirection(Control control)
    {
        control.FlowDirection = I18n.IsRightToLeft(I18n.CurrentLocale)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }
}
