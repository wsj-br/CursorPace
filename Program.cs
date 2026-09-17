using System.Diagnostics;
using Avalonia;
using CursorPace.Services;

namespace CursorPace;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            TryLog("UnhandledException", e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            TryLog("UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        var singleInstance = SingleInstance.Create();
        if (!singleInstance.TryAcquire())
        {
            if (LaunchMode.ActivateExistingInstance(args))
                singleInstance.SignalExisting();
            singleInstance.Dispose();
            return;
        }

        SingleInstance.Current = singleInstance;
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            SingleInstance.Current = null;
            singleInstance.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { WmClass = "CursorPace" })
            .WithInterFont()
            .LogToTrace();

    private static void TryLog(string kind, object? exception)
    {
        try
        {
            var text = $"{DateTimeOffset.Now:O} {kind}: {exception}";
            Trace.WriteLine(text);
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CursorPace");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "crash.log"), text + Environment.NewLine);
        }
        catch
        {
        }
    }
}
