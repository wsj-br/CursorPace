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

    // A duplicate autostart (`--background`) must exit silently. `--show` still
    // asks the running instance to come forward.
    public static bool ActivateExistingInstance(IEnumerable<string> commandLineArgs)
    {
        ArgumentNullException.ThrowIfNull(commandLineArgs);
        if (commandLineArgs.Contains(ShowArgument, StringComparer.Ordinal))
            return true;

        return !commandLineArgs.Contains(BackgroundArgument, StringComparer.Ordinal);
    }
}
