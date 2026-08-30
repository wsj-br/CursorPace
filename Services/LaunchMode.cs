namespace CursorPace.Services;

public static class LaunchMode
{
    public const string BackgroundArgument = "--background";

    public static bool HideMainWindow(bool startInNotificationTray, IEnumerable<string> commandLineArgs)
    {
        ArgumentNullException.ThrowIfNull(commandLineArgs);
        return startInNotificationTray
            || commandLineArgs.Contains(BackgroundArgument, StringComparer.Ordinal);
    }
}
