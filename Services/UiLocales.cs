using System.Text.Json;

namespace CursorPace.Services;

public static class UiLocales
{
    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Normalize(string? code)
    {
        var canonical = CanonicalOrNull(code);
        return canonical ?? SourceLocale;
    }

    public static string? CanonicalOrNull(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmed = code.Trim().Replace('_', '-');
        foreach (var locale in SupportedLocales)
        {
            if (string.Equals(locale.Code, trimmed, StringComparison.OrdinalIgnoreCase))
                return locale.Code;
        }

        return null;
    }

    public static bool TryApplyFromDisk()
    {
        var path = FindManifest(AppContext.BaseDirectory)
            ?? FindManifest(Directory.GetCurrentDirectory());
        if (path == null)
        {
            Apply(new[] { SourceLanguage() });
            return false;
        }

        LocaleInfo[]? locales;
        try
        {
            locales = Parse(File.ReadAllText(path));
        }
        catch (IOException)
        {
            locales = null;
        }
        catch (UnauthorizedAccessException)
        {
            locales = null;
        }

        if (locales == null)
            return false;

        Apply(locales);
        return true;
    }

    private static void Apply(LocaleInfo[] locales)
    {
        Configure(new I18nOptions
        {
            SourceLocale = SourceLocale,
            Locales = locales
        });
    }

    private static LocaleInfo SourceLanguage()
    {
        foreach (var locale in SupportedLocales)
        {
            if (string.Equals(locale.Code, SourceLocale, StringComparison.OrdinalIgnoreCase))
                return locale;
        }

        return new LocaleInfo(SourceLocale, "English", "English");
    }

    // bin/Debug/net10.0 is three levels down from the repo, and the test output is four.
    private static string? FindManifest(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            return null;

        DirectoryInfo? dir;
        try
        {
            dir = new DirectoryInfo(startDirectory);
        }
        catch (ArgumentException)
        {
            return null;
        }

        for (var depth = 0; dir != null && depth < 8; depth++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "locales", "ui-languages.json");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static LocaleInfo[]? Parse(string json)
    {
        ManifestEntry[]? entries;
        try
        {
            entries = JsonSerializer.Deserialize<ManifestEntry[]>(json, ManifestOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (entries == null || entries.Length == 0)
            return null;

        var locales = new List<LocaleInfo>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Code))
                continue;

            var code = entry.Code.Trim().Replace('_', '-');
            if (locales.Exists(locale => string.Equals(locale.Code, code, StringComparison.OrdinalIgnoreCase)))
                continue;

            var native = string.IsNullOrWhiteSpace(entry.Label) ? code : entry.Label.Trim();
            var english = string.IsNullOrWhiteSpace(entry.EnglishName) ? native : entry.EnglishName.Trim();
            locales.Add(new LocaleInfo(code, native, english));
        }

        return locales.Count == 0 ? null : locales.ToArray();
    }

    private sealed class ManifestEntry
    {
        public string? Code { get; set; }
        public string? Label { get; set; }
        public string? EnglishName { get; set; }
    }
}
