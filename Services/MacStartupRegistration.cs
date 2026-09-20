using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CursorPace.Services;

public sealed class MacStartupRegistration : IStartupRegistration
{
    internal const string Label = "com.cursorpace.app";
    internal const string BundleIdentifier = Label;
    internal const string OpenExecutable = "/usr/bin/open";
    private const long LoginItemEnabled = 1;
    private const long LoginItemRequiresApproval = 2;
    private const int RtldLazy = 1;
    private static bool _serviceManagementResolved;
    private static bool _serviceManagementAvailable;

    private static string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library",
        "LaunchAgents",
        Label + ".plist");

    public bool IsRegistered
    {
        get
        {
            var processPath = Environment.ProcessPath;
            var bundlePath = processPath == null
                ? null
                : ResolveExistingAppBundlePath(processPath);
            if (bundlePath != null
                && TryGetMainAppStatus(out var status)
                && IsNativeLoginItemRegistered(status))
            {
                return true;
            }

            return File.Exists(PlistPath);
        }
    }

    public void Register(bool startInTray)
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine executable path");
        var bundlePath = ResolveExistingAppBundlePath(exePath);

        // SMAppService.mainApp registers the app itself under Open at Login,
        // including the bundle name and icon. It requires a signed app bundle
        // on macOS 13 and later, so keep the attributed Launch Agent fallback
        // for unsigned builds and older macOS versions.
        if (bundlePath != null)
        {
            // Remove the old legacy job first. The native service and the
            // fallback use the same identifier, and launchd can reject the
            // native registration while the legacy job is still loaded.
            RemoveLaunchAgent();
            if (TryRegisterMainApp())
                return;
        }

        // launchd must not exec Contents/MacOS/CursorPace. That path is a
        // background item (Allow in the Background): the process is not a GUI
        // app, the tray icon never appears, and Accessory cannot hide the Dock.
        // open(1) starts the .app through LaunchServices instead.
        var arguments = BuildLaunchProgramArguments(bundlePath, exePath, startInTray);
        WriteLaunchAgent(arguments);
        ReloadLaunchAgent();
    }

    public void Unregister()
    {
        TryUnregisterMainApp();
        RemoveLaunchAgent();
    }

    internal static bool IsNativeLoginItemRegistered(long status) =>
        status is LoginItemEnabled or LoginItemRequiresApproval;

    public static string? ResolveAppBundlePath(string? processPath)
    {
        if (string.IsNullOrWhiteSpace(processPath))
            return null;

        var macosDir = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(macosDir)
            || !string.Equals(Path.GetFileName(macosDir), "MacOS", StringComparison.OrdinalIgnoreCase))
            return null;

        var contentsDir = Path.GetDirectoryName(macosDir);
        if (string.IsNullOrEmpty(contentsDir)
            || !string.Equals(Path.GetFileName(contentsDir), "Contents", StringComparison.OrdinalIgnoreCase))
            return null;

        var bundle = Path.GetDirectoryName(contentsDir);
        if (string.IsNullOrEmpty(bundle)
            || !Path.GetFileName(bundle).EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            return null;

        return bundle;
    }

    public static IReadOnlyList<string> BuildLaunchProgramArguments(
        string? appBundlePath,
        string executablePath,
        bool startInTray)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        if (!string.IsNullOrWhiteSpace(appBundlePath))
        {
            var args = new List<string> { OpenExecutable };
            if (startInTray)
                args.Add("-g");
            args.Add("-a");
            args.Add(appBundlePath);
            if (startInTray)
            {
                args.Add("--args");
                args.Add("--background");
            }

            return args;
        }

        return startInTray
            ? [executablePath, "--background"]
            : [executablePath];
    }

    public static string BuildLaunchAgentPlist(
        string label,
        IReadOnlyList<string> programArguments,
        string? associatedBundleIdentifier = BundleIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(programArguments);
        if (programArguments.Count == 0)
            throw new ArgumentException("ProgramArguments must not be empty.", nameof(programArguments));

        var args = new StringBuilder();
        foreach (var argument in programArguments)
            args.Append("    <string>").Append(EscapeXml(argument)).AppendLine("</string>");

        var associatedBundle = string.IsNullOrWhiteSpace(associatedBundleIdentifier)
            ? string.Empty
            : $"""
              <key>AssociatedBundleIdentifiers</key>
              <array>
                <string>{EscapeXml(associatedBundleIdentifier)}</string>
              </array>
            """;

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
              <key>Label</key>
              <string>{EscapeXml(label)}</string>
            {associatedBundle}
              <key>RunAtLoad</key>
              <true/>
              <key>LimitLoadToSessionType</key>
              <string>Aqua</string>
              <key>ProgramArguments</key>
              <array>
            {args.ToString().TrimEnd()}
              </array>
            </dict>
            </plist>
            """;
    }

    private static string? ResolveExistingAppBundlePath(string processPath)
    {
        var bundlePath = ResolveAppBundlePath(processPath);
        return bundlePath != null && Directory.Exists(bundlePath) ? bundlePath : null;
    }

    private void WriteLaunchAgent(IReadOnlyList<string> programArguments)
    {
        var directory = Path.GetDirectoryName(PlistPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(PlistPath, BuildLaunchAgentPlist(Label, programArguments, BundleIdentifier));
    }

    private void RemoveLaunchAgent()
    {
        BootoutLaunchAgent();
        if (File.Exists(PlistPath))
            File.Delete(PlistPath);
    }

    private static void ReloadLaunchAgent()
    {
        BootoutLaunchAgent();
        TryLaunchCtl("bootstrap", GuiDomain(), PlistPath);
    }

    private static void BootoutLaunchAgent() =>
        TryLaunchCtl("bootout", GuiDomain(), PlistPath);

    private static string GuiDomain()
    {
        var uid = TryReadUserId();
        return string.IsNullOrWhiteSpace(uid) ? "gui/501" : "gui/" + uid;
    }

    private static string? TryReadUserId()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "id",
                ArgumentList = { "-u" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null)
                return Environment.GetEnvironmentVariable("UID");

            process.WaitForExit(3000);
            return process.StandardOutput.ReadToEnd().Trim();
        }
        catch
        {
            return Environment.GetEnvironmentVariable("UID");
        }
    }

    private static void TryLaunchCtl(params string[] arguments)
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = "launchctl",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
                start.ArgumentList.Add(argument);

            using var process = Process.Start(start);
            process?.WaitForExit(5000);
        }
        catch
        {
        }
    }

    private static bool TryRegisterMainApp()
    {
        if (!TryGetMainAppService(out var service))
            return false;

        var status = GetMainAppStatus(service);
        if (IsNativeLoginItemRegistered(status))
            return true;

        if (InvokeErrorReturningSelector(service, "registerAndReturnError:"))
            return true;

        return IsNativeLoginItemRegistered(GetMainAppStatus(service));
    }

    private static void TryUnregisterMainApp()
    {
        if (!TryGetMainAppService(out var service))
            return;

        if (!IsNativeLoginItemRegistered(GetMainAppStatus(service)))
            return;

        InvokeErrorReturningSelector(service, "unregisterAndReturnError:");
    }

    private static bool TryGetMainAppStatus(out long status)
    {
        status = 0;
        if (!TryGetMainAppService(out var service))
            return false;

        status = GetMainAppStatus(service);
        return true;
    }

    private static long GetMainAppStatus(IntPtr service) =>
        IntPtr_objc_msgSend(service, sel_registerName("status")).ToInt64();

    private static bool InvokeErrorReturningSelector(IntPtr service, string selectorName)
    {
        var errorStorage = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(errorStorage, IntPtr.Zero);
        try
        {
            return Bool_objc_msgSend(
                service,
                sel_registerName(selectorName),
                errorStorage) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(errorStorage);
        }
    }

    private static bool TryGetMainAppService(out IntPtr service)
    {
        service = IntPtr.Zero;
        if (!OperatingSystem.IsMacOS()
            || !OperatingSystem.IsMacOSVersionAtLeast(13)
            || !EnsureServiceManagementLoaded())
        {
            return false;
        }

        var serviceClass = objc_getClass("SMAppService");
        if (serviceClass == IntPtr.Zero)
            return false;

        service = IntPtr_objc_msgSend(
            serviceClass,
            sel_registerName("mainAppService"));
        return service != IntPtr.Zero;
    }

    private static bool EnsureServiceManagementLoaded()
    {
        if (_serviceManagementResolved)
            return _serviceManagementAvailable;

        _serviceManagementResolved = true;
        try
        {
            var handle = dlopen(
                "/System/Library/Frameworks/ServiceManagement.framework/ServiceManagement",
                RtldLazy);
            _serviceManagementAvailable = handle != IntPtr.Zero
                && objc_getClass("SMAppService") != IntPtr.Zero;
        }
        catch
        {
            _serviceManagementAvailable = false;
        }

        return _serviceManagementAvailable;
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private const string Objc = "/usr/lib/libobjc.A.dylib";
    private const string LibSystem = "/usr/lib/libSystem.B.dylib";

    [DllImport(LibSystem)]
    private static extern IntPtr dlopen(string path, int mode);

    [DllImport(Objc)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(Objc)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    private static extern byte Bool_objc_msgSend(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg);
}
