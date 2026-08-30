using Avalonia.Controls;
using CursorUsageProgress.Models;
using CursorUsageProgress.Views;

namespace CursorUsageProgress.Services;

public sealed class NativeWebViewCursorUsageClient : ICursorUsageClient
{
    private static readonly Uri DashboardUri = new("https://cursor.com/dashboard");

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

            const encoded = JSON.stringify(payload);

            try {
                chrome.webview.postMessage(payload);
            } catch {
            }

            try {
                invokeCSharpAction(encoded);
            } catch {
            }

            // WebKit (Linux/macOS) only marshals primitive/string script results;
            // returning a raw object surfaces as "Unsupported result type".
            return encoded;
        })();
        """;

    private readonly IUiDispatcher _dispatcher;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public NativeWebViewCursorUsageClient(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public bool HasPersistedProfile =>
        Directory.Exists(WebViewProfilePaths.ProfileDirectory)
        && Directory.EnumerateFileSystemEntries(WebViewProfilePaths.ProfileDirectory).Any()
        && !File.Exists(WebViewProfilePaths.SignedOutMarkerPath);

    public async Task<UsageFetchResult> FetchAsync(
        bool allowInteractiveLogin,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        WebViewHostWindow? host = null;
        try
        {
            host = new WebViewHostWindow();
            host.PlaceOffscreen();
            host.Show();
            try
            {
                await host.EnsureReadyAsync();
            }
            catch (TimeoutException)
            {
                return new UsageFetchResult(
                    UsageFetchStatus.Error,
                    null,
                    WebViewUnavailableMessage(),
                    0);
            }

            var webView = host.WebView;
            webView.NewWindowRequested += OnNewWindowRequested;
            host.HideHost();

            await NavigateAsync(webView, DashboardUri, TimeSpan.FromSeconds(30), cancellationToken);

            if (!IsCursorAppHost(webView.Source))
            {
                if (!allowInteractiveLogin)
                    return new UsageFetchResult(UsageFetchStatus.AuthRequired, null, "Sign in to Cursor to sync usage.", 401);

                return await WaitForLoginAsync(host, cancellationToken);
            }

            var fetch = await ExecuteFetchAsync(webView, cancellationToken);
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
        WebViewHostWindow? host = null;
        try
        {
            var folder = WebViewProfilePaths.ProfileDirectory;
            if (!Directory.Exists(folder))
                return;

            var clearedCursorCookiesOnly = false;
            try
            {
                host = new WebViewHostWindow();
                host.PlaceOffscreen();
                host.Show();
                await host.EnsureReadyAsync();
                host.HideHost();

                var cookieManager = host.WebView.TryGetCookieManager();
                if (cookieManager != null)
                {
                    var cookies = await cookieManager.GetCookiesAsync();
                    foreach (var cookie in cookies)
                    {
                        if (IsCursorCookieDomain(cookie.Domain))
                            cookieManager.DeleteCookie(cookie);
                    }

                    clearedCursorCookiesOnly = true;
                }
            }
            catch
            {
                // Cookie manager unavailable or failed; fall back to a full profile wipe below.
            }
            finally
            {
                if (host != null)
                    await CloseHostAsync(host);
            }

            if (clearedCursorCookiesOnly)
            {
                File.WriteAllText(WebViewProfilePaths.SignedOutMarkerPath, string.Empty);
                return;
            }

            // Fallback for backends without a cookie manager: this also clears
            // any Google/GitHub session stored in the profile.
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
                await Task.Delay(250);
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool IsCursorCookieDomain(string? domain)
    {
        if (string.IsNullOrEmpty(domain))
            return false;

        var host = domain.TrimStart('.');
        return host.Equals("cursor.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".cursor.com", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<UsageFetchResult> WaitForLoginAsync(
        WebViewHostWindow host,
        CancellationToken cancellationToken)
    {
        host.ShowForLogin();

        var webView = host.WebView;
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
                if (!IsCursorAppHost(webView.Source))
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
                var fetch = await ExecuteFetchAsync(webView, cancellationToken);
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

        EventHandler<WebViewNavigationCompletedEventArgs>? navigated = null;
        EventHandler? continued = null;
        EventHandler? closed = null;

        navigated = (_, args) =>
        {
            if (!args.IsSuccess || finished.Task.IsCompleted)
                return;
            _dispatcher.Post(() => _ = TryCompleteAsync(fromUser: false));
        };
        continued = (_, _) =>
            _dispatcher.Post(() => _ = TryCompleteAsync(fromUser: true));
        closed = (_, _) =>
        {
            pollCts.Cancel();
            _dispatcher.Post(() => _ = CompleteClosedAsync());
        };

        async Task CompleteClosedAsync()
        {
            try
            {
                if (!finished.Task.IsCompleted && IsCursorAppHost(webView.Source))
                {
                    var fetch = await ExecuteFetchAsync(webView, cancellationToken);
                    if (fetch.Status == 200)
                    {
                        finished.TrySetResult(ToResult(fetch));
                        return;
                    }
                }
            }
            catch
            {
            }

            finished.TrySetResult(
                new UsageFetchResult(UsageFetchStatus.AuthRequired, null, "Sign in was cancelled.", 401));
        }

        webView.NavigationCompleted += navigated;
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
                }
            }
        }
        finally
        {
            pollCts.Cancel();
            webView.NavigationCompleted -= navigated;
            host.ContinueRequested -= continued;
            host.Closed -= closed;
        }
    }

    private static async Task NavigateAsync(
        NativeWebView webView,
        Uri url,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? sender, WebViewNavigationCompletedEventArgs e) =>
            done.TrySetResult(e.IsSuccess);

        webView.NavigationCompleted += Handler;
        try
        {
            webView.Navigate(url);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            await done.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            webView.NavigationCompleted -= Handler;
        }
    }

    private static async Task<ScriptFetchResult> ExecuteFetchAsync(
        NativeWebView webView,
        CancellationToken cancellationToken)
    {
        if (!IsCursorAppHost(webView.Source))
            return new ScriptFetchResult { Status = 401, Body = "Not on cursor.com." };

        var finished = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnMessage(object? sender, WebMessageReceivedEventArgs args)
        {
            var json = args.Body;
            if (!string.IsNullOrEmpty(json) && WebView2ScriptResultParser.TryParse(json, out _, out _))
                finished.TrySetResult(json);
        }

        webView.WebMessageReceived += OnMessage;
        try
        {
            Exception? invokeError = null;
            string? encoded = null;
            try
            {
                encoded = await webView.InvokeScript(FetchScript);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Script may still have delivered the payload via invokeCSharpAction.
                invokeError = ex;
            }

            if (finished.Task.IsCompletedSuccessfully)
                return ParseFetchResult(await finished.Task);

            if (encoded != null
                && WebView2ScriptResultParser.TryParse(encoded, out var status, out var body))
            {
                return new ScriptFetchResult { Status = status, Body = body };
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                return ParseFetchResult(await finished.Task.WaitAsync(timeoutCts.Token));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (invokeError != null)
                    throw new InvalidOperationException(invokeError.Message, invokeError);

                throw new InvalidOperationException("The usage request timed out.");
            }
        }
        finally
        {
            webView.WebMessageReceived -= OnMessage;
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

        // A 200 response proves a live Cursor session exists, so any earlier
        // Sign out marker (cursor.com cookies cleared, other cookies kept) no
        // longer applies.
        try
        {
            File.Delete(WebViewProfilePaths.SignedOutMarkerPath);
        }
        catch (IOException)
        {
        }

        return new UsageFetchResult(UsageFetchStatus.Ok, snapshot, null, fetch.Status);
    }

    private static async Task CloseHostAsync(WebViewHostWindow host)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Closed += (_, _) => closed.TrySetResult();
        host.Close();
        try
        {
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
        }
    }

    private void OnNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (sender is NativeWebView webView && e.Request != null)
            _dispatcher.Post(() => webView.Navigate(e.Request));
    }

    private static string WebViewUnavailableMessage()
    {
        if (OperatingSystem.IsLinux())
        {
            return "Could not start the embedded browser. Install WebKitGTK 4.1 "
                + "(and WPE WebKit if your distribution requires it).";
        }

        return "Could not start the embedded browser used to sign in to Cursor.";
    }

    private static bool IsCursorAppHost(Uri? url)
    {
        if (url == null)
            return false;

        return url.Host.Equals("cursor.com", StringComparison.OrdinalIgnoreCase)
            || url.Host.Equals("www.cursor.com", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ScriptFetchResult
    {
        public int Status { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
