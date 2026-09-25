using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace CursorPace.Services;

// WebKitGTK does not write cookies to the WebsiteDataManager directory unless
// webkit_cookie_manager_set_persistent_storage is called. Avalonia's GTK adapter
// never does that, so the Cursor session lives only in memory and is gone after
// a reboot. WebView2 on Windows persists cookies in the user-data folder on its own.
public static class LinuxWebKitCookiePersistence
{
    private const int WebKitCookiePersistentStorageSqlite = 1;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(2);

    private static readonly string[] WebKitLibraryNames =
    [
        "libwebkit2gtk-4.1.so.0",
        "libwebkit2gtk-4.1.so",
        "libwebkit2gtk-4.0.so.37",
        "libwebkit2gtk-4.0.so",
        "libwpewebkit-2.0.so.1",
        "libwpewebkit-2.0.so",
    ];

    private static readonly object Gate = new();
    private static IntPtr _webKit;
    private static IntPtr _glib;
    private static GetPtr? _getNetworkSession;
    private static GetPtr? _getCookieManagerFromSession;
    private static GetPtr? _getContext;
    private static GetPtr? _getWebsiteDataManager;
    private static GetPtr? _getCookieManagerFromDataManager;
    private static SetPersistentStorage? _setPersistentStorage;
    private static IdleAdd? _idleAdd;
    private static bool _resolved;

    public static string DatabasePath =>
        Path.Combine(WebViewProfilePaths.ProfileDirectory, "cookies.sqlite");

    public static Task EnsureAsync(NativeWebView webView)
    {
        if (!OperatingSystem.IsLinux())
            return Task.CompletedTask;

        var native = TryGetWebViewHandle(webView);
        if (native == IntPtr.Zero)
            return Task.CompletedTask;

        try
        {
            ResolveNative();
            if (_setPersistentStorage == null)
                return Task.CompletedTask;

            Directory.CreateDirectory(WebViewProfilePaths.ProfileDirectory);
            return ApplyOnGLibThreadAsync(native);
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    private static IntPtr TryGetWebViewHandle(NativeWebView webView)
    {
        var handle = webView.TryGetPlatformHandle();
        return handle switch
        {
            IGtkWebViewPlatformHandle gtk => gtk.WebKitWebView,
            ILinuxWpePlatformHandle wpe => wpe.WebKitWebView,
            _ => IntPtr.Zero,
        };
    }

    private static Task ApplyOnGLibThreadAsync(IntPtr webView)
    {
        // Already on the GLib main loop. Scheduling an idle and waiting for it
        // cannot run until this dispatcher turn returns.
        if (Dispatcher.UIThread.CheckAccess() || _idleAdd == null)
        {
            Apply(webView);
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = new IdleState(webView, tcs);
        GSourceFunc handler = OnIdle;
        state.Handler = handler;
        var gcHandle = GCHandle.Alloc(state);
        try
        {
            var sourceId = _idleAdd(handler, GCHandle.ToIntPtr(gcHandle));
            if (sourceId == 0)
            {
                gcHandle.Free();
                Apply(webView);
                return Task.CompletedTask;
            }
        }
        catch
        {
            if (gcHandle.IsAllocated)
                gcHandle.Free();
            Apply(webView);
            return Task.CompletedTask;
        }

        return WaitForIdleAsync(tcs, webView);
    }

    private static async Task WaitForIdleAsync(TaskCompletionSource tcs, IntPtr webView)
    {
        try
        {
            await tcs.Task.WaitAsync(IdleTimeout);
        }
        catch (TimeoutException)
        {
            Apply(webView);
        }
        catch (Exception)
        {
        }
    }

    private static int OnIdle(IntPtr data)
    {
        if (data == IntPtr.Zero)
            return 0;

        var gcHandle = GCHandle.FromIntPtr(data);
        if (gcHandle.Target is not IdleState state)
        {
            if (gcHandle.IsAllocated)
                gcHandle.Free();
            return 0;
        }

        try
        {
            Apply(state.WebView);
            state.Completion.TrySetResult();
        }
        catch (Exception ex)
        {
            state.Completion.TrySetException(ex);
        }
        finally
        {
            if (gcHandle.IsAllocated)
                gcHandle.Free();
        }

        return 0;
    }

    private static void Apply(IntPtr webView)
    {
        if (webView == IntPtr.Zero || _setPersistentStorage == null)
            return;

        var cookies = TryGetCookieManager(webView);
        if (cookies == IntPtr.Zero)
            return;

        var path = Marshal.StringToCoTaskMemUTF8(DatabasePath);
        try
        {
            _setPersistentStorage(cookies, path, WebKitCookiePersistentStorageSqlite);
        }
        finally
        {
            Marshal.FreeCoTaskMem(path);
        }
    }

    private static IntPtr TryGetCookieManager(IntPtr webView)
    {
        // Avalonia's GTK adapter creates the view with a WebsiteDataManager context.
        // Prefer that cookie jar so persistence lands in our profile folder, not the
        // default NetworkSession (which may be ephemeral).
        if (_getContext != null && _getWebsiteDataManager != null && _getCookieManagerFromDataManager != null)
        {
            var context = _getContext(webView);
            if (context != IntPtr.Zero)
            {
                var manager = _getWebsiteDataManager(context);
                if (manager != IntPtr.Zero)
                {
                    var fromManager = _getCookieManagerFromDataManager(manager);
                    if (fromManager != IntPtr.Zero)
                        return fromManager;
                }
            }
        }

        if (_getNetworkSession == null || _getCookieManagerFromSession == null)
            return IntPtr.Zero;

        var session = _getNetworkSession(webView);
        if (session == IntPtr.Zero)
            return IntPtr.Zero;

        return _getCookieManagerFromSession(session);
    }

    private static void ResolveNative()
    {
        lock (Gate)
        {
            if (_resolved)
                return;

            _resolved = true;
            if (!TryLoadFirst(WebKitLibraryNames, out _webKit))
                return;

            _getNetworkSession = GetExport<GetPtr>(_webKit, "webkit_web_view_get_network_session");
            _getCookieManagerFromSession = GetExport<GetPtr>(_webKit, "webkit_network_session_get_cookie_manager");
            _getContext = GetExport<GetPtr>(_webKit, "webkit_web_view_get_context");
            _getWebsiteDataManager = GetExport<GetPtr>(_webKit, "webkit_web_context_get_website_data_manager");
            _getCookieManagerFromDataManager = GetExport<GetPtr>(_webKit, "webkit_website_data_manager_get_cookie_manager");
            _setPersistentStorage = GetExport<SetPersistentStorage>(
                _webKit,
                "webkit_cookie_manager_set_persistent_storage");

            if (NativeLibrary.TryLoad("libglib-2.0.so.0", out _glib)
                || NativeLibrary.TryLoad("libglib-2.0.so", out _glib))
            {
                _idleAdd = GetExport<IdleAdd>(_glib, "g_idle_add");
            }
        }
    }

    private static bool TryLoadFirst(string[] names, out IntPtr handle)
    {
        foreach (var name in names)
        {
            if (NativeLibrary.TryLoad(name, out handle))
                return true;
        }

        handle = IntPtr.Zero;
        return false;
    }

    private static T? GetExport<T>(IntPtr library, string name)
        where T : Delegate
    {
        if (!NativeLibrary.TryGetExport(library, name, out var address) || address == IntPtr.Zero)
            return null;
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetPtr(IntPtr handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetPersistentStorage(IntPtr cookieManager, IntPtr filename, int storage);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint IdleAdd(GSourceFunc function, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GSourceFunc(IntPtr data);

    private sealed class IdleState(IntPtr webView, TaskCompletionSource completion)
    {
        public IntPtr WebView { get; } = webView;
        public TaskCompletionSource Completion { get; } = completion;
        public GSourceFunc? Handler;
    }
}
