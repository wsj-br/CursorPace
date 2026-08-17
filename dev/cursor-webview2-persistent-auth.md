# Cursor Usage Reader with WebView2

This guide describes a native Windows C# implementation that:

1. Installs and initializes WebView2.
2. Opens Cursor authentication in an embedded browser.
3. Stores the authenticated WebView2 profile persistently.
4. Fetches Cursor usage data on later launches without requiring login again.
5. Re-authenticates only when the Cursor session expires or is revoked.

The approach uses a dedicated WebView2 profile. It does not read or decrypt the user's Google Chrome cookies, and it does not require Python, Playwright, Selenium, or a Cursor Team API key.

> The endpoint `https://cursor.com/api/usage-summary` is a Cursor dashboard endpoint rather than a documented public personal-plan API. Its response schema and availability may change.

## 1. Requirements

- Windows 10 or later.
- .NET 8 or later recommended.
- WPF or WinForms application.
- Microsoft Edge WebView2 Runtime installed on the machine.
- A Cursor individual account, such as Pro.

WebView2 stores cookies and other browser state in its user-data folder. Reusing the same folder allows the authenticated session to persist between application launches.

## 2. Add the WebView2 package

For a WPF or WinForms project:

```powershell
dotnet add package Microsoft.Web.WebView2
```

The application installer should ensure that the WebView2 Runtime is present. Many current Windows installations already include it, but a production installer should check for the Evergreen Runtime and install or bootstrap it when necessary.

## 3. WPF control

Add the WebView2 control to the main window.

```xml
<Window
    x:Class="CursorProgress.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
    Title="Cursor Progress"
    Width="1100"
    Height="800">

    <Grid>
        <wv2:WebView2 x:Name="Browser" />
    </Grid>
</Window>
```

For a background-only implementation, the control can be placed in a hidden or minimized window. It must still be initialized on a UI thread.

## 4. Persistent profile location

Create a stable, application-specific WebView2 user-data folder:

```csharp
private static string GetWebViewUserDataFolder()
{
    return Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "YourCompany",
        "CursorProgress",
        "WebView2");
}
```

Do not use a temporary folder or generate a random folder on every launch:

```csharp
// Do not do this for persistent authentication.
var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
```

The same user-data-folder path must be used every time the application starts. If the path changes, WebView2 creates a new profile and the user must authenticate again.

## 5. Initialize WebView2

```csharp
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace CursorProgress;

public partial class MainWindow : Window
{
    private CoreWebView2Environment? _environment;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await InitializeWebViewAsync();

            var result = await FetchUsageSummaryAsync();

            if (result.Status is 200)
            {
                DisplayUsage(result.Body);
            }
            else if (result.Status is 401 or 403)
            {
                ShowCursorLogin();
            }
            else
            {
                ShowRequestError(result.Status, result.Body);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Cursor initialization error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task InitializeWebViewAsync()
    {
        var userDataFolder = GetWebViewUserDataFolder();

        Directory.CreateDirectory(userDataFolder);

        _environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);

        await Browser.EnsureCoreWebView2Async(_environment);

        Browser.CoreWebView2.NavigationCompleted +=
            Browser_NavigationCompleted;
    }

    private static string GetWebViewUserDataFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "YourCompany",
            "CursorProgress",
            "WebView2");
    }
}
```

## 6. Authenticate only when necessary

On the first launch, navigate to the Cursor dashboard:

```csharp
private void ShowCursorLogin()
{
    Browser.Visibility = Visibility.Visible;
    Browser.CoreWebView2.Navigate("https://cursor.com/dashboard");
}
```

The user completes authentication inside WebView2. Cursor stores its session cookies in the persistent WebView2 profile.

On later launches, the application initializes WebView2 using the same profile and attempts the usage request first. If the session is still valid, no login screen is needed.

## 7. Fetch usage from inside WebView2

Do not extract the cookie into C# unless there is a specific reason to do so. Execute an authenticated same-origin `fetch` inside the WebView2 page instead. WebView2 attaches the stored Cursor cookies automatically.

```csharp
private sealed record FetchResult(
    int Status,
    string Body);

private async Task<FetchResult> FetchUsageSummaryAsync()
{
    const string script = """
        (async () => {
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

                return JSON.stringify({
                    status: response.status,
                    body: await response.text()
                });
            } catch (error) {
                return JSON.stringify({
                    status: 0,
                    body: String(error)
                });
            }
        })();
        """;

    var encodedResult = await Browser.CoreWebView2
        .ExecuteScriptAsync(script);

    var resultJson = JsonSerializer.Deserialize<string>(encodedResult)
        ?? throw new InvalidOperationException(
            "WebView2 returned an empty response.");

    return JsonSerializer.Deserialize<FetchResult>(resultJson)
        ?? throw new InvalidOperationException(
            "Unable to parse the usage request result.");
}
```

The `credentials: 'include'` option is important. It tells the browser to include cookies associated with the Cursor session.

## 8. Detect authentication status

Treat the response status as the authority. Do not try to predict the exact cookie expiration date.

```csharp
private async Task<bool> IsCursorAuthenticatedAsync()
{
    var result = await FetchUsageSummaryAsync();
    return result.Status == 200;
}
```

A useful status policy is:

```csharp
private async Task LoadUsageAsync()
{
    var result = await FetchUsageSummaryAsync();

    switch (result.Status)
    {
        case 200:
            DisplayUsage(result.Body);
            break;

        case 401:
        case 403:
            ShowCursorLogin();
            break;

        case 429:
            ShowRequestError(
                result.Status,
                "Cursor rate-limited the request. Try again later.");
            break;

        default:
            ShowRequestError(result.Status, result.Body);
            break;
    }
}
```

## 9. Retry after authentication

When the user finishes signing in, the page will navigate through the authentication flow and eventually return to Cursor. Retry the usage request after the dashboard navigation completes.

```csharp
private async void Browser_NavigationCompleted(
    object? sender,
    CoreWebView2NavigationCompletedEventArgs e)
{
    if (!e.IsSuccess)
        return;

    var url = Browser.Source?.ToString() ?? string.Empty;

    if (!url.Contains("cursor.com", StringComparison.OrdinalIgnoreCase))
        return;

    if (!url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase))
        return;

    try
    {
        await Task.Delay(500);
        await LoadUsageAsync();
    }
    catch (Exception ex)
    {
        ShowRequestError(0, ex.Message);
    }
}
```

The short delay gives the authentication flow time to finish setting cookies before the application performs the request.

## 10. Display and parse the response

Initially, log or display the complete response so that you can inspect the schema for your Cursor account:

```csharp
private void DisplayUsage(string body)
{
    try
    {
        using var document = JsonDocument.Parse(body);

        var formatted = JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        UsageText.Text = formatted;
    }
    catch (JsonException)
    {
        UsageText.Text = body;
    }
}
```

Example WPF UI element:

```xml
<TextBox
    x:Name="UsageText"
    IsReadOnly="True"
    TextWrapping="Wrap"
    VerticalScrollBarVisibility="Auto" />
```

Do not assume the exact JSON path until you inspect the response. Cursor may change fields between account types and dashboard versions. Fields seen in some personal-plan responses include values similar to:

```text
individualUsage
plan
autoPercentUsed
apiPercentUsed
totalPercentUsed
```

These are dashboard fields, not a stable public API contract.

## 11. Refresh usage without re-authentication

Add a refresh method that reuses the same WebView2 profile:

```csharp
private async void RefreshButton_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        await LoadUsageAsync();
    }
    catch (Exception ex)
    {
        ShowRequestError(0, ex.Message);
    }
}
```

The application can also refresh periodically:

```csharp
private readonly DispatcherTimer _refreshTimer = new()
{
    Interval = TimeSpan.FromMinutes(5)
};

private void StartRefreshTimer()
{
    _refreshTimer.Tick += async (_, _) =>
    {
        try
        {
            await LoadUsageAsync();
        }
        catch
        {
            // Log the exception in production.
        }
    };

    _refreshTimer.Start();
}
```

Avoid aggressive polling. A five- or ten-minute interval is more appropriate for a dashboard indicator. Also provide a manual refresh button.

## 12. Complete minimal flow

```csharp
private async void MainWindow_Loaded(
    object sender,
    RoutedEventArgs e)
{
    await InitializeWebViewAsync();

    var result = await FetchUsageSummaryAsync();

    if (result.Status == 200)
    {
        DisplayUsage(result.Body);
        StartRefreshTimer();
        return;
    }

    if (result.Status is 401 or 403)
    {
        ShowCursorLogin();
        return;
    }

    ShowRequestError(result.Status, result.Body);
}
```

The behavior is:

```text
First run
  → WebView2 profile is empty
  → usage request returns 401/403
  → dashboard opens
  → user signs in
  → usage is loaded

Later run with valid session
  → same WebView2 profile is opened
  → stored cookies are reused
  → usage request returns 200
  → no authentication UI is shown

After session expiration
  → usage request returns 401/403
  → dashboard opens
  → user authenticates again
  → profile receives the new session
```

## 13. Optional: retrieve cookies from WebView2

WebView2 exposes a cookie manager:

```csharp
private async Task<IReadOnlyList<CoreWebView2Cookie>>
    GetCursorCookiesAsync()
{
    return await Browser.CoreWebView2.CookieManager
        .GetCookiesAsync("https://cursor.com/");
}
```

You can inspect metadata without printing values:

```csharp
var cookies = await GetCursorCookiesAsync();

foreach (var cookie in cookies)
{
    Console.WriteLine(
        $"{cookie.Name} " +
        $"domain={cookie.Domain} " +
        $"secure={cookie.IsSecure} " +
        $"httpOnly={cookie.IsHttpOnly}");
}
```

For this application, prefer executing `fetch()` inside WebView2 instead of copying cookie values into C# or an `HttpClient` instance. This reduces exposure of the session token.

## 14. Logout and disconnect

Provide a disconnect action that clears the dedicated WebView2 profile:

```csharp
private async Task DisconnectCursorAsync()
{
    if (Browser.CoreWebView2 is null)
        return;

    await Browser.CoreWebView2.Profile.ClearBrowsingDataAsync(
        CoreWebView2BrowsingDataKinds.AllSite);

    Browser.CoreWebView2.Navigate("about:blank");
}
```

After this operation, the user will need to authenticate again. Do not clear the profile on ordinary application exit.

## 15. Multi-account support

If the application supports multiple Cursor accounts, assign each account a separate persistent profile. Do not use the email address directly as a path component; derive a stable identifier or hash.

```csharp
private static string GetProfileFolder(string profileId)
{
    return Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "YourCompany",
        "CursorProgress",
        "Profiles",
        profileId);
}
```

The profile folder must remain stable for that account. Switching to another folder means switching to another WebView2 session.

## 16. Security considerations

- Use a dedicated WebView2 profile rather than the user's normal Chrome profile.
- Never read or decrypt Chrome's cookie database.
- Never log cookie values, session tokens, or copied cURL commands containing cookies.
- Do not send cookies to an external service.
- Do not store raw cookies in application settings or a database.
- Keep the profile under the user's local application-data directory.
- Avoid using a network share for the WebView2 user-data folder.
- Provide a visible Disconnect or Sign out action.
- Treat the Cursor endpoint as undocumented and subject to change.
- Handle `401`, `403`, `429`, network failures, and JSON-schema changes.
- Do not use the Cursor Team Admin API for an individual Pro account.

## 17. Why this avoids repeated authentication

The application does not save a manually extracted cookie. Instead, WebView2 owns a persistent browser profile:

```text
WebView2 profile
  ├── cookies
  ├── local storage
  ├── session-related browser state
  └── other site data
```

On each launch, the application creates WebView2 with the same profile folder. WebView2 restores the profile, and the browser automatically sends valid Cursor cookies with the request. Authentication is only required after Cursor or its identity provider invalidates the session.

## 18. Limitations

This approach does not turn the personal Cursor dashboard endpoint into an official supported API. It depends on the endpoint currently used by Cursor's web dashboard:

```text
GET https://cursor.com/api/usage-summary
```

Cursor may change:

- The endpoint path.
- Required headers.
- Authentication behavior.
- The response schema.
- The meaning of usage fields.
- Whether the endpoint remains available to personal accounts.

Build the application so that an endpoint or schema failure produces a clear message and can be updated independently.

## Summary

Use a persistent WebView2 profile and make the usage request from inside that profile:

```text
Create stable WebView2 user-data folder
  → authenticate once in embedded Cursor page
  → reuse the same folder on later launches
  → execute authenticated fetch('/api/usage-summary')
  → show login only after 401/403
```

This is a native Windows C# solution with no Python, Playwright, Selenium, Chrome-cookie decryption, or Team API subscription requirement.
