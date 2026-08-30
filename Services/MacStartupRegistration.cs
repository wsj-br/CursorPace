using System.Diagnostics;
using System.Text;

namespace CursorPace.Services;

public sealed class MacStartupRegistration : IStartupRegistration
{
    private const string Label = "com.cursorpace.app";

    private static string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library",
        "LaunchAgents",
        Label + ".plist");

    public bool IsRegistered => File.Exists(PlistPath);

    public void Register(bool startInTray)
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine executable path");

        var args = new StringBuilder();
        args.Append("    <string>").Append(EscapeXml(exePath)).AppendLine("</string>");
        if (startInTray)
            args.AppendLine("    <string>--background</string>");

        var plist = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
              <key>Label</key>
              <string>{Label}</string>
              <key>RunAtLoad</key>
              <true/>
              <key>ProgramArguments</key>
              <array>
            {args.ToString().TrimEnd()}
              </array>
            </dict>
            </plist>
            """;

        var directory = Path.GetDirectoryName(PlistPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(PlistPath, plist);
        ReloadLaunchAgent();
    }

    public void Unregister()
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

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
