using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using CursorPace.Services;

namespace CursorPace.Views;

public partial class WebViewHostWindow : Window
{
    public WebViewHostWindow()
    {
        InitializeComponent();
        Title = "Sign in to Cursor";
        Browser.EnvironmentRequested += OnEnvironmentRequested;
    }

    public NativeWebView WebView => Browser;

    public event EventHandler? ContinueRequested;

    public void SetBannerStatus(string text) => SignInBannerText.Text = text;

    public Task EnsureReadyAsync()
    {
        Directory.CreateDirectory(WebViewProfilePaths.ProfileDirectory);
        if (!OperatingSystem.IsWindows())
            Directory.CreateDirectory(WebViewProfilePaths.CacheDirectory);

        if (Browser.AdapterInfo != null)
            return Task.CompletedTask;

        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnCreated(object? sender, EventArgs e)
        {
            Browser.AdapterCreated -= OnCreated;
            ready.TrySetResult();
        }

        Browser.AdapterCreated += OnCreated;
        if (Browser.AdapterInfo != null)
        {
            Browser.AdapterCreated -= OnCreated;
            return Task.CompletedTask;
        }

        return ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    public void PlaceOffscreen()
    {
        SignInBanner.IsVisible = false;
        Width = 1;
        Height = 1;
        Opacity = 0;
        CanResize = false;
        CanMaximize = false;
        CanMinimize = false;
        ShowInTaskbar = false;
    }

    public void ShowForLogin()
    {
        Opacity = 1;
        SignInBanner.IsVisible = true;
        Width = 900;
        Height = 700;
        CanResize = true;
        CanMaximize = false;
        CanMinimize = true;
        ShowInTaskbar = true;
        CenterOnWorkArea();
        Show();
        Activate();
    }

    public void HideHost() => Hide();

    private void CenterOnWorkArea()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null)
            return;

        var work = screen.WorkingArea;
        var width = (int)Math.Round(Width);
        var height = (int)Math.Round(Height);
        var x = work.X + Math.Max(0, (work.Width - width) / 2);
        var y = work.Y + Math.Max(0, (work.Height - height) / 2);
        Position = new PixelPoint(x, y);
    }

    private static void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        Directory.CreateDirectory(WebViewProfilePaths.ProfileDirectory);
        if (!OperatingSystem.IsWindows())
            Directory.CreateDirectory(WebViewProfilePaths.CacheDirectory);

        switch (e)
        {
            case WindowsWebView2EnvironmentRequestedEventArgs windows:
                windows.UserDataFolder = WebViewProfilePaths.ProfileDirectory;
                break;
            case AppleWKWebViewEnvironmentRequestedEventArgs apple:
                apple.NonPersistentDataStore = false;
                apple.DataStoreIdentifier = new Guid("a7c4e91b-2d58-4f0a-9c31-6b8e0d5f4a12");
                break;
            case LinuxWpeWebViewEnvironmentRequestedEventArgs wpe:
                wpe.DataDirectory = WebViewProfilePaths.ProfileDirectory;
                wpe.CacheDirectory = WebViewProfilePaths.CacheDirectory;
                break;
            case GtkWebViewEnvironmentRequestedEventArgs gtk:
                gtk.EphemeralDataManager = false;
                gtk.BaseDataDirectory = WebViewProfilePaths.ProfileDirectory;
                gtk.BaseCacheDirectory = WebViewProfilePaths.CacheDirectory;
                break;
        }
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e) =>
        ContinueRequested?.Invoke(this, EventArgs.Empty);
}
