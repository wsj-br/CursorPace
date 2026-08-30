namespace CursorPace.Services;

public static class LaunchMode
{
    public const string BackgroundArgument = "--background";
    public const string ShowArgument = "--show";

    public static bool HideMainWindow(bool startInNotificationTray, IEnumerable<string> commandLineArgs)
    {
        ArgumentNullException.ThrowIfNull(commandLineArgs);
        if (commandLineArgs.Contains(ShowArgument, StringComparer.Ordinal))
            return false;

        return startInNotificationTray
            || commandLineArgs.Contains(BackgroundArgument, StringComparer.Ordinal);
    }
}
