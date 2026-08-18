using System.IO;
using CursorUsageProgress.Models;
using CursorUsageProgress.Views;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace CursorUsageProgress.Services;

public sealed class WebView2CursorUsageClient : ICursorUsageClient
{
    public static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorUsageProgress",
        "WebView2");

    private const string DashboardUrl = "https://cursor.com/dashboard";

    private const string FetchScript = """
        (async () => {
            const payload = await (async () => {
                try {
                    const response = await fetch(
                        'https://cursor.com/api/usage-summary',
                        {
                            method: 'GET',
                            credentials: 'include',
                            headers: {
                                'Accept': 'application/json'
                            }
                        }
                    );

                    return {
                        status: response.status,
                        body: await response.text()
                    };
                } catch (error) {
                    return {
                        status: 0,
                        body: String(error)
                    };
                }
            })();

            try {
                chrome.webview.postMessage(payload);
            } catch {
                // ExecuteScriptAsync result is the fallback when host messaging is unavailable.
            }

            return payload;
        })();
        """;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool HasPersistedProfile =>
        Directory.Exists(UserDataFolder)
        && Directory.EnumerateFileSystemEntries(UserDataFolder).Any();

    public async Task<UsageFetchResult> FetchAsync(
        bool allowInteractiveLogin,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        WebView2HostWindow? host = null;
        try
        {
            host = new WebView2HostWindow();
            host.PlaceOffscreen();
            host.Activate();
            await host.EnsureReadyAsync(UserDataFolder);

            var core = host.WebView.CoreWebView2
                ?? throw new InvalidOperationException("WebView2 did not initialize.");

            core.NewWindowRequested += OnNewWindowRequested;
            core.Settings.IsWebMessageEnabled = true;
            host.HideHost();

            await NavigateAsync(core, DashboardUrl, TimeSpan.FromSeconds(30), cancellationToken);

            if (!IsCursorAppHost(core.Source))
            {
                if (!allowInteractiveLogin)
                    return new UsageFetchResult(UsageFetchStatus.AuthRequired, null, "Sign in to Cursor to sync usage.", 401);

                return await WaitForLoginAsync(host, cancellationToken);
            }

            var fetch = await ExecuteFetchAsync(core, cancellationToken);
            if (fetch.Status == 200)
                return ToResult(fetch);

            if (fetch.Status is 401 or 403)
            {
                if (!allowInteractiveLogin)
                    return new UsageFetchResult(UsageFetchStatus.AuthRequired, null, "Sign in to Cursor to sync usage.", fetch.Status);

                return await WaitForLoginAsync(host, cancellationToken);
            }

            if (fetch.Status == 429)
            {
                return new UsageFetchResult(
                    UsageFetchStatus.RateLimited,
                    null,
                    "Cursor rate-limited the request. Will retry later.",
                    fetch.Status);
            }

            return new UsageFetchResult(UsageFetchStatus.Error, null, fetch.Body, fetch.Status);
        }
        catch (OperationCanceledException)
        {
            return new UsageFetchResult(UsageFetchStatus.Error, null, "The usage request was cancelled.", 0);
        }
        catch (Exception ex)
        {
            return new UsageFetchResult(UsageFetchStatus.Error, null, ex.Message, 0);
        }
        finally
        {
            if (host != null)
                await CloseHostAsync(host);
            _gate.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!Directory.Exists(UserDataFolder))
                return;

            try
            {
                Directory.Delete(UserDataFolder, recursive: true);
            }
            catch (IOException)
            {
                await Task.Delay(250);
                if (Directory.Exists(UserDataFolder))
                    Directory.Delete(UserDataFolder, recursive: true);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<UsageFetchResult> WaitForLoginAsync(
        WebView2HostWindow host,
        CancellationToken cancellationToken)
    {
        host.ShowForLogin();

        var core = host.WebView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 did not initialize.");

        var finished = new TaskCompletionSource<UsageFetchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var checking = 0;

        async Task TryCompleteAsync(bool fromUser)
        {
            if (finished.Task.IsCompleted)
                return;
            if (Interlocked.CompareExchange(ref checking, 1, 0) != 0)
                return;

            try
            {
                if (!IsCursorAppHost(core.Source))
                {
                    if (fromUser)
                    {
                        host.SetBannerStatus(
                            "Finish signing in until you see your Cursor account, then click Continue.");
                    }

                    return;
                }

                if (fromUser)
                    host.SetBannerStatus("Checking Cursor session…");

                await Task.Delay(fromUser ? 200 : 400, cancellationToken);
                var fetch = await ExecuteFetchAsync(core, cancellationToken);
                if (fetch.Status == 200)
                {
                    finished.TrySetResult(ToResult(fetch));
                    return;
                }

                if (fromUser)
                {
                    host.SetBannerStatus(
                        "Cursor has not accepted the session yet. Stay on cursor.com after signing in, then click Continue.");
                }
            }
            catch (OperationCanceledException)
            {
                // Caller is shutting down.
            }
            catch (Exception ex)
            {
                if (fromUser)
                    host.SetBannerStatus(ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref checking, 0);
            }
        }

        TypedEventHandler<CoreWebView2, CoreWebView2NavigationCompletedEventArgs>? navigated = null;
        TypedEventHandler<CoreWebView2, object>? historyChanged = null;
        TypedEventHandler<object, WindowEventArgs>? closed = null;
        EventHandler? continued = null;

        navigated = async (_, args) =>
        {
            if (!args.IsSuccess || finished.Task.IsCompleted)
                return;
            await TryCompleteAsync(fromUser: false);
        };
        historyChanged = async (_, _) =>
        {
            if (finished.Task.IsCompleted)
                return;
            await TryCompleteAsync(fromUser: false);
        };
        continued = async (_, _) => await TryCompleteAsync(fromUser: true);
        closed = async (_, _) =>
        {
            pollCts.Cancel();
            try
            {
                if (!finished.Task.IsCompleted && IsCursorAppHost(core.Source))
                {
                    var fetch = await ExecuteFetchAsync(core, cancellationToken);
                    if (fetch.Status == 200)
                    {
                        finished.TrySetResult(ToResult(fetch));
                        return;
                    }
                }
            }
            catch
            {
                // The control may already be tearing down.
            }

            finished.TrySetResult(
                new UsageFetchResult(UsageFetchStatus.AuthRequired, null, "Sign in was cancelled.", 401));
        };

        core.NavigationCompleted += navigated;
        core.HistoryChanged += historyChanged;
        host.ContinueRequested += continued;
        host.Closed += closed;

        try
        {
            _ = TryCompleteAsync(fromUser: false);
            _ = PollForSessionAsync();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return await finished.Task.WaitAsync(linked.Token);

            async Task PollForSessionAsync()
            {
                try
                {
                    while (!pollCts.IsCancellationRequested && !finished.Task.IsCompleted)
                    {
                        await Task.Delay(1500, pollCts.Token);
                        await TryCompleteAsync(fromUser: false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Login window closed or fetch completed.
                }
            }
        }
        finally
        {
            pollCts.Cancel();
            core.NavigationCompleted -= navigated;
            core.HistoryChanged -= historyChanged;
            host.ContinueRequested -= continued;
            host.Closed -= closed;
        }
    }

    private static async Task NavigateAsync(
        CoreWebView2 core,
        string url,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var done = new TaskCompletionSource<bool>();
        void Handler(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e) =>
            done.TrySetResult(e.IsSuccess);

        core.NavigationCompleted += Handler;
        try
        {
            core.Navigate(url);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            await done.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            core.NavigationCompleted -= Handler;
        }
    }

    private static async Task<ScriptFetchResult> ExecuteFetchAsync(
        CoreWebView2 core,
        CancellationToken cancellationToken)
    {
        if (!IsCursorAppHost(core.Source))
            return new ScriptFetchResult { Status = 401, Body = "Not on cursor.com." };

        var finished = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnMessage(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var json = args.WebMessageAsJson;
            if (WebView2ScriptResultParser.TryParse(json, out _, out _))
                finished.TrySetResult(json);
        }

        core.WebMessageReceived += OnMessage;
        try
        {
            var encoded = await core.ExecuteScriptAsync(FetchScript);
            if (finished.Task.IsCompletedSuccessfully)
                return ParseFetchResult(await finished.Task);

            if (WebView2ScriptResultParser.TryParse(encoded, out var status, out var body))
                return new ScriptFetchResult { Status = status, Body = body };

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                return ParseFetchResult(await finished.Task.WaitAsync(timeoutCts.Token));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("The usage request timed out.");
            }
        }
        finally
        {
            core.WebMessageReceived -= OnMessage;
        }
    }

    private static ScriptFetchResult ParseFetchResult(string encoded)
    {
        if (!WebView2ScriptResultParser.TryParse(encoded, out var status, out var body))
            throw new InvalidOperationException("Unable to parse the usage request result.");

        return new ScriptFetchResult { Status = status, Body = body };
    }

    private static UsageFetchResult ToResult(ScriptFetchResult fetch)
    {
        if (!UsageSummaryParser.TryParse(fetch.Body, DateTimeOffset.UtcNow, out var snapshot) || snapshot == null)
        {
            return new UsageFetchResult(
                UsageFetchStatus.Error,
                null,
                "The Cursor usage response could not be parsed.",
                fetch.Status);
        }

        return new UsageFetchResult(UsageFetchStatus.Ok, snapshot, null, fetch.Status);
    }

    private static async Task CloseHostAsync(WebView2HostWindow host)
    {
        var closed = new TaskCompletionSource();
        host.Closed += (_, _) => closed.TrySetResult();
        host.Close();
        try
        {
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
            // Profile unlock is best-effort after Close.
        }
    }

    private static void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (!string.IsNullOrEmpty(e.Uri))
            sender.Navigate(e.Uri);
    }

    private static bool IsCursorAppHost(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("cursor.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("www.cursor.com", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ScriptFetchResult
    {
        public int Status { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
