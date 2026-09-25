using System.Globalization;
using System.Reflection;

namespace CursorPace.Services;

public sealed class AppInfo
{
    public const string LicenseName = "MIT License";
    public const string RepositoryUrl = "https://github.com/wsj-br/CursorPace";
    public const string SyncServerRepositoryUrl = "https://github.com/wsj-br/CursorPace-SyncServer";
    public const string BuildDateMetadataKey = "BuildDateUtc";

    public static Uri RepositoryUri { get; } = new(RepositoryUrl, UriKind.Absolute);

    public static Uri SyncServerRepositoryUri { get; } = new(SyncServerRepositoryUrl, UriKind.Absolute);

    public static AppInfo Current { get; } = Read(typeof(AppInfo).Assembly);

    public AppInfo(string version, string copyright, DateTime? buildDateUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(copyright);
        Version = version;
        Copyright = copyright;
        BuildDateUtc = buildDateUtc;
    }

    public string Version { get; }

    public string Copyright { get; }

    public DateTime? BuildDateUtc { get; }

    public static AppInfo Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = NormalizeVersion(informational, assembly.GetName().Version);
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        if (string.IsNullOrWhiteSpace(copyright))
            copyright = "Copyright © 2026 Waldemar Scudeller Jr.";

        DateTime? buildDate = null;
        var raw = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == BuildDateMetadataKey)?.Value;
        if (DateTime.TryParseExact(
                raw,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            buildDate = parsed;
        else if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            buildDate = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

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
        if (BuildDateUtc is not { } date)
            return "—";

        var utc = date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : date.ToUniversalTime();
        return $"{utc.ToString("dd-MMM-yyyy", culture)} {utc.ToString("HH:mm:ss", CultureInfo.InvariantCulture)} UTC";
    }
}
