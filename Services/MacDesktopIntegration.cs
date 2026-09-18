using System.Runtime.InteropServices;

namespace CursorPace.Services;

public static class MacDesktopIntegration
{
    public const string IconFileName = "cursor_pace.png";

    public static void EnsureDockIcon()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        try
        {
            var iconPath = ResolveBundledIconPath(AppContext.BaseDirectory);
            if (iconPath is null)
                return;

            ApplyDockIcon(iconPath);
        }
        catch
        {
        }
    }

    internal static string? ResolveBundledIconPath(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            return null;

        var candidate = Path.Combine(baseDirectory, "Assets", IconFileName);
        return File.Exists(candidate) ? candidate : null;
    }

    private static void ApplyDockIcon(string iconPath)
    {
        var nsImageClass = objc_getClass("NSImage");
        var nsStringClass = objc_getClass("NSString");
        var nsAppClass = objc_getClass("NSApplication");
        if (nsImageClass == IntPtr.Zero || nsStringClass == IntPtr.Zero || nsAppClass == IntPtr.Zero)
            return;

        var alloc = sel_registerName("alloc");
        var initWithContentsOfFile = sel_registerName("initWithContentsOfFile:");
        var stringWithUTF8String = sel_registerName("stringWithUTF8String:");
        var sharedApplication = sel_registerName("sharedApplication");
        var setApplicationIconImage = sel_registerName("setApplicationIconImage:");
        var release = sel_registerName("release");

        var utf8 = Marshal.StringToCoTaskMemUTF8(iconPath);
        try
        {
            var nsPath = IntPtr_objc_msgSend(nsStringClass, stringWithUTF8String, utf8);
            if (nsPath == IntPtr.Zero)
                return;

            var image = IntPtr_objc_msgSend(
                IntPtr_objc_msgSend(nsImageClass, alloc),
                initWithContentsOfFile,
                nsPath);
            if (image == IntPtr.Zero)
                return;

            var nsApp = IntPtr_objc_msgSend(nsAppClass, sharedApplication);
            if (nsApp != IntPtr.Zero)
                Void_objc_msgSend(nsApp, setApplicationIconImage, image);

            IntPtr_objc_msgSend(image, release);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    private const string Objc = "/usr/lib/libobjc.A.dylib";

    [DllImport(Objc)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(Objc)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    private static extern void Void_objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);
}
