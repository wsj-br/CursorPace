using System.Text;

namespace CursorUsageProgress.Services;

public sealed class MacStartupRegistration : IStartupRegistration
{
    private const string Label = "com.cursorusageprogress.app";

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
    }

    public void Unregister()
    {
        if (File.Exists(PlistPath))
            File.Delete(PlistPath);
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
