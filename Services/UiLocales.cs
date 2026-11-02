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
        return TryApplyManifest(path);
    }

    internal static bool TryApplyManifest(string? path)
    {
        if (path != null)
        {
            try
            {
                var locales = Parse(File.ReadAllText(path));
                if (locales == null)
                    return false;

                Apply(locales);
                return true;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        var embedded = ReadEmbeddedManifest();
        if (embedded != null)
        {
            var locales = Parse(embedded);
            if (locales != null)
            {
                Apply(locales);
                return true;
            }
        }

        Apply(new[] { SourceLanguage() });
        return false;
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
    // A published install has no repo parent and reads the embedded manifest instead.
    internal static string? FindManifest(string? startDirectory)
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

    private static string? ReadEmbeddedManifest()
    {
        var assembly = typeof(UiLocales).Assembly;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith("ui-languages.json", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
                return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
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
