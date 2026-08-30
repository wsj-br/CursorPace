using System.Globalization;
using System.Reflection;

namespace CursorPace.Services;

public sealed class AppInfo
{
    public const string LicenseName = "MIT License";
    public const string RepositoryUrl = "https://github.com/wsj-br/CursorPace";
    public const string BuildDateMetadataKey = "BuildDateUtc";

    public static Uri RepositoryUri { get; } = new(RepositoryUrl, UriKind.Absolute);

    public static AppInfo Current { get; } = Read(typeof(AppInfo).Assembly);

    public AppInfo(string version, string copyright, DateOnly? buildDateUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(copyright);
        Version = version;
        Copyright = copyright;
        BuildDateUtc = buildDateUtc;
    }

    public string Version { get; }

    public string Copyright { get; }

    public DateOnly? BuildDateUtc { get; }

    public static AppInfo Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = NormalizeVersion(informational, assembly.GetName().Version);
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        if (string.IsNullOrWhiteSpace(copyright))
            copyright = "Copyright © 2026 Waldemar Scudeller Jr.";

        DateOnly? buildDate = null;
        var raw = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == BuildDateMetadataKey)?.Value;
        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            buildDate = parsed;

        return new AppInfo(version, copyright, buildDate);
    }

    public static string NormalizeVersion(string? informationalVersion, Version? assemblyVersion)
    {
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informationalVersion[..plus] : informationalVersion;
        }

        if (assemblyVersion == null)
            return "0.0.0";

        return assemblyVersion.Revision > 0
            ? assemblyVersion.ToString()
            : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }

    public string FormatBuildDate(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return BuildDateUtc is { } date
            ? date.ToString("dd-MMM-yyyy", culture)
            : "—";
    }
}
