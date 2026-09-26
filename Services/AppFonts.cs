using Avalonia.Media;

namespace CursorPace.Services;

internal static class AppFonts
{
    // Embedded Inter from WithInterFont(). A bare "Inter" maps to
    // fonts:SystemFonts#Inter and goes through fontconfig.
    public const string InterFamilyName = "fonts:Inter#Inter";

    public static FontManagerOptions ManagerOptions { get; } = new()
    {
        DefaultFamilyName = InterFamilyName,
        FontFallbacks = [new FontFallback { FontFamily = new FontFamily(InterFamilyName) }]
    };
}
