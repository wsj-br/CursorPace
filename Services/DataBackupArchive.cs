using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace CursorUsageProgress.Services;

public static class DataBackupArchive
{
    public const int FormatVersion = 1;
    public const string ProductName = "CursorUsageProgress";
    public const string ManifestEntryName = "manifest.json";
    public const string SettingsEntryName = "settings.json";
    public const string SamplesEntryName = "usage-samples.json";

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Write(
        Stream destination,
        string settingsJson,
        string samplesJson,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(samplesJson);

        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        WriteEntry(zip, ManifestEntryName, JsonSerializer.Serialize(new Manifest
        {
            FormatVersion = FormatVersion,
            Product = ProductName,
            CreatedUtc = createdUtc
        }, ManifestOptions));
        WriteEntry(zip, SettingsEntryName, settingsJson);
        WriteEntry(zip, SamplesEntryName, samplesJson);
    }

    public static DataBackupReadResult Read(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        try
        {
            using var zip = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
            var settings = ReadEntry(zip, SettingsEntryName);
            if (string.IsNullOrWhiteSpace(settings))
                return DataBackupReadResult.Fail("The backup file does not contain settings.json.");

            var manifestJson = ReadEntry(zip, ManifestEntryName);
            if (!string.IsNullOrWhiteSpace(manifestJson))
            {
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, ManifestOptions);
                if (manifest != null && manifest.FormatVersion > FormatVersion)
                    return DataBackupReadResult.Fail("This backup was created by a newer app version.");
                if (manifest != null
                    && !string.IsNullOrWhiteSpace(manifest.Product)
                    && !string.Equals(manifest.Product, ProductName, StringComparison.Ordinal))
                {
                    return DataBackupReadResult.Fail("This file is not a Cursor Usage Progress backup.");
                }
            }

            var samples = ReadEntry(zip, SamplesEntryName);
            if (string.IsNullOrWhiteSpace(samples))
                samples = """{"version":1,"samples":[]}""";

            return DataBackupReadResult.Ok(settings, samples);
        }
        catch (InvalidDataException)
        {
            return DataBackupReadResult.Fail("This file is not a valid backup archive.");
        }
        catch (Exception)
        {
            return DataBackupReadResult.Fail("Could not read the backup file.");
        }
    }

    private static void WriteEntry(ZipArchive zip, string name, string text)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.SmallestSize);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(text);
    }

    private static string? ReadEntry(ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name);
        if (entry == null)
            return null;
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class Manifest
    {
        public int FormatVersion { get; set; }
        public string Product { get; set; } = "";
        public DateTimeOffset CreatedUtc { get; set; }
    }
}

public sealed class DataBackupReadResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string SettingsJson { get; private init; } = "";
    public string SamplesJson { get; private init; } = "";

    public static DataBackupReadResult Ok(string settingsJson, string samplesJson) =>
        new()
        {
            Success = true,
            SettingsJson = settingsJson,
            SamplesJson = samplesJson
        };

    public static DataBackupReadResult Fail(string message) =>
        new()
        {
            Success = false,
            ErrorMessage = message
        };
}
