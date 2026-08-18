using System.IO;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace CursorUsageProgress.Views;

public sealed partial class WebView2HostWindow : Window
{
    public WebView2HostWindow()
    {
        InitializeComponent();
        Title = "Sign in to Cursor";
        ExtendsContentIntoTitleBar = false;
        RootGrid.Loaded += (_, _) => TextBlockSelection.EnableOnLabels(RootGrid);
    }

    public Microsoft.UI.Xaml.Controls.WebView2 WebView => Browser;

    public event EventHandler? ContinueRequested;

    public void SetBannerStatus(string text)
    {
        SignInBannerText.Text = text;
    }

    public async Task EnsureReadyAsync(string userDataFolder)
    {
        Directory.CreateDirectory(userDataFolder);
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
        try
        {
            await Browser.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "WebView2 failed to start (" + ex.Message + "). " +
                "Install the Microsoft Edge WebView2 Runtime if it is missing: https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                ex);
        }
    }

    public void PlaceOffscreen()
    {
        SignInBanner.Visibility = Visibility.Collapsed;
        AppWindow.Move(new PointInt32(-32000, -32000));
        AppWindow.Resize(new SizeInt32(800, 600));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }

    public void ShowForLogin()
    {
        SignInBanner.Visibility = Visibility.Visible;
        AppWindow.Resize(new SizeInt32(900, 700));
        CenterOnWorkArea();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = false;
            presenter.IsResizable = true;
        }

        AppWindow.Show();
        Activate();
    }

    private void CenterOnWorkArea()
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var work = display.WorkArea;
        var size = AppWindow.Size;
        var x = work.X + Math.Max(0, (work.Width - size.Width) / 2);
        var y = work.Y + Math.Max(0, (work.Height - size.Height) / 2);
        AppWindow.Move(new PointInt32(x, y));
    }

    public void HideHost()
    {
        AppWindow.Hide();
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        ContinueRequested?.Invoke(this, EventArgs.Empty);
    }
}
