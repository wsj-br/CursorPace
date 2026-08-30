namespace CursorUsageProgress.Models;

public enum UiThemeMode
{
    System,
    Light,
    Dark
}

public static class UiTheme
{
    public static readonly UiThemeMode[] AllowedModes =
    [
        UiThemeMode.System,
        UiThemeMode.Light,
        UiThemeMode.Dark
    ];

    public static UiThemeMode Clamp(UiThemeMode mode) => mode switch
    {
        UiThemeMode.System => UiThemeMode.System,
        UiThemeMode.Light => UiThemeMode.Light,
        UiThemeMode.Dark => UiThemeMode.Dark,
        _ => UiThemeMode.System
    };
}
