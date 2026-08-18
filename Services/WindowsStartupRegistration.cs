using Microsoft.Win32;

namespace CursorUsageProgress.Services;

public sealed class WindowsStartupRegistration : IStartupRegistration
{
    private const string AppName = "CursorUsageProgress";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(AppName) != null;
        }
    }

    public void Register()
    {
        var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path");
        var value = $"\"{exePath}\" --background";

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Cannot open Run registry key");

        key.SetValue(AppName, value);
    }

    public void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
