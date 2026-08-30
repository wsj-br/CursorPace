namespace CursorPace.Services;

public static class StartupRegistration
{
    public static IStartupRegistration Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsStartupRegistration();
        if (OperatingSystem.IsMacOS())
            return new MacStartupRegistration();
        if (OperatingSystem.IsLinux())
            return new LinuxStartupRegistration();
        return new NoOpStartupRegistration();
    }
}

internal sealed class NoOpStartupRegistration : IStartupRegistration
{
    public bool IsRegistered => false;
    public void Register(bool startInTray) { }
    public void Unregister() { }
}
