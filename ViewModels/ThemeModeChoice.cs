using CursorPace.Models;

namespace CursorPace.ViewModels;

public sealed class ThemeModeChoice
{
    public ThemeModeChoice(UiThemeMode mode) => Mode = mode;

    public UiThemeMode Mode { get; }

    public override string ToString() => Mode switch
    {
        UiThemeMode.System => T("System"),
        UiThemeMode.Light => T("Light"),
        UiThemeMode.Dark => T("Dark"),
        _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null)
    };

    public override bool Equals(object? obj) => obj is ThemeModeChoice other && other.Mode == Mode;

    public override int GetHashCode() => (int)Mode;
}
