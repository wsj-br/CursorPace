using System.Runtime.InteropServices;

namespace CursorPace.Services;

internal static class LinuxFontSetup
{
    public const string ConfigFileName = "fonts.conf";

    public static void Apply()
    {
        if (!OperatingSystem.IsLinux())
            return;

        SetNativeEnvironment("FC_FONTATIONS", "0");

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CursorPace");
        Directory.CreateDirectory(folder);
        ApplyTo(folder, Environment.GetEnvironmentVariable("FONTCONFIG_FILE"));
    }

    internal static void ApplyTo(string directory, string? existingFontConfigFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var path = Path.Combine(directory, ConfigFileName);
        if (string.Equals(existingFontConfigFile, path, StringComparison.Ordinal))
        {
            SetNativeEnvironment("FONTCONFIG_FILE", path);
            return;
        }

        var include = ResolveIncludePath(existingFontConfigFile);
        File.WriteAllText(path, BuildConfig(include));
        SetNativeEnvironment("FONTCONFIG_FILE", path);
    }

    internal static void SetNativeEnvironment(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);
        if (OperatingSystem.IsLinux())
            SetEnv(name, value, 1);
    }

    [DllImport("libc", EntryPoint = "setenv")]
    private static extern int SetEnv(string name, string value, int overwrite);

    internal static string BuildConfig(string includePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includePath);
        var include = EscapeXml(includePath);
        return $"""
            <?xml version="1.0"?>
            <!DOCTYPE fontconfig SYSTEM "urn:fontconfig:fonts.dtd">
            <fontconfig>
              <include ignore_missing="yes">{include}</include>
              <selectfont>
                <rejectfont>
                  <glob>/usr/share/fonts/woff/**</glob>
                </rejectfont>
                <rejectfont>
                  <glob>**/*.woff</glob>
                </rejectfont>
                <rejectfont>
                  <glob>**/*.woff2</glob>
                </rejectfont>
              </selectfont>
            </fontconfig>

            """;
    }

    internal static string ResolveIncludePath(string? existingFontConfigFile)
    {
        if (string.IsNullOrWhiteSpace(existingFontConfigFile))
            return "/etc/fonts/fonts.conf";
        if (existingFontConfigFile.IndexOfAny(['<', '>', '&']) >= 0)
            return "/etc/fonts/fonts.conf";
        return existingFontConfigFile;
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
