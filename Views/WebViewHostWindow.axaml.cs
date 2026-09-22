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
        // macOS WKWebView aborts if it is created with a NaN frame. Keep the
        // attach until a finite arrange there. Linux WebKitGTK already worked
        // in-tree on a 1x1 silent host, so leave it attached.
        if (WebViewHostLayout.DeferBrowserAttach(OperatingSystem.IsLinux())
            && Browser.Parent is Panel panel)
            panel.Children.Remove(Browser);
    }

    public NativeWebView WebView => Browser;

    public event EventHandler? ContinueRequested;

    public void SetBannerStatus(string text) => SignInBannerText.Text = text;

    public async Task EnsureReadyAsync()
    {
        Directory.CreateDirectory(WebViewProfilePaths.ProfileDirectory);
        if (!OperatingSystem.IsWindows())
            Directory.CreateDirectory(WebViewProfilePaths.CacheDirectory);

        await WaitForArrangedAsync();
        AttachBrowserIfNeeded();

        if (Browser.AdapterInfo == null)
        {
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
            }
            else
            {
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
        }

        await LinuxWebKitCookiePersistence.EnsureAsync(Browser);
    }

    public void PlaceOffscreen()
    {
        var isLinux = OperatingSystem.IsLinux();
        var size = WebViewHostLayout.SilentHostSize(isLinux);
        var min = WebViewHostLayout.SilentHostMinSize(isLinux);
        SignInBanner.IsVisible = false;
        // AXAML still has MinWidth/MinHeight 400x300. Lower mins before size or
        // Height=1 is clamped to 300 and Refresh shows a 1px-wide vertical line.
        MinWidth = min.MinWidth;
        MinHeight = min.MinHeight;
        Width = size.Width;
        Height = size.Height;
        Opacity = WebViewHostLayout.SilentHostOpacity(isLinux);
        ShowActivated = false;
        CanResize = false;
        CanMaximize = false;
        CanMinimize = false;
        ShowInTaskbar = false;
        if (WebViewHostLayout.UsesCompactSilentHost(isLinux))
        {
            WindowDecorations = Avalonia.Controls.WindowDecorations.None;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        WindowDecorations = Avalonia.Controls.WindowDecorations.Full;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = OffscreenPixel();
    }

    public void ShowForLogin()
    {
        AttachBrowserIfNeeded();
        WindowDecorations = Avalonia.Controls.WindowDecorations.Full;
        Opacity = 1;
        SignInBanner.IsVisible = true;
        Width = WebViewHostLayout.LoginWidth;
        Height = WebViewHostLayout.LoginHeight;
        MinWidth = WebViewHostLayout.MinWidth;
        MinHeight = WebViewHostLayout.MinHeight;
        ShowActivated = true;
        CanResize = true;
        CanMaximize = false;
        CanMinimize = true;
        ShowInTaskbar = true;
        CenterOnWorkArea();
        Show();
        Activate();
    }

    public void HideHost() => Hide();

    private async Task WaitForArrangedAsync()
    {
        UpdateLayout();
        if (WebViewHostLayout.HasFinitePositiveSize(Bounds.Width, Bounds.Height))
            return;

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnLayout(object? sender, EventArgs e)
        {
            if (!WebViewHostLayout.HasFinitePositiveSize(Bounds.Width, Bounds.Height))
                return;
            LayoutUpdated -= OnLayout;
            done.TrySetResult();
        }

        LayoutUpdated += OnLayout;
        try
        {
            OnLayout(this, EventArgs.Empty);
            if (!done.Task.IsCompleted)
                await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            LayoutUpdated -= OnLayout;
        }
    }

    private void AttachBrowserIfNeeded()
    {
        if (Browser.Parent != null)
            return;

        var (width, height) = WebViewHostLayout.BrowserSlotSize(
            RootGrid.Bounds.Width,
            RootGrid.Bounds.Height,
            SignInBanner.Bounds.Height,
            SignInBanner.IsVisible);
        Browser.Measure(new Size(width, height));
        Browser.Arrange(new Rect(0, 0, width, height));
        Grid.SetRow(Browser, 1);
        RootGrid.Children.Add(Browser);
    }

    private PixelPoint OffscreenPixel()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null)
        {
            var fallback = WebViewHostLayout.OffscreenPosition();
            return new PixelPoint(fallback.X, fallback.Y);
        }

        var work = screen.WorkingArea;
        var (x, y) = WebViewHostLayout.OffscreenPosition(work.X, work.Y, work.Height);
        return new PixelPoint(x, y);
    }

    private void CenterOnWorkArea()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null)
            return;

        var work = screen.WorkingArea;
        var width = WebViewHostLayout.IsFinitePositive(Width)
            ? (int)Math.Round(Width)
            : WebViewHostLayout.LoginWidth;
        var height = WebViewHostLayout.IsFinitePositive(Height)
            ? (int)Math.Round(Height)
            : WebViewHostLayout.LoginHeight;
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
