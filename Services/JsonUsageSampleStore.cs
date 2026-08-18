using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public sealed class JsonUsageSampleStore : IUsageSampleStore
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorUsageProgress",
        "usage-samples.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _filePath;
    private readonly string _corruptBackupPath;

    public JsonUsageSampleStore()
        : this(DefaultPath)
    {
    }

    public JsonUsageSampleStore(string filePath)
    {
        _filePath = filePath;
        _corruptBackupPath = Path.Combine(
            Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory,
            Path.GetFileNameWithoutExtension(filePath) + ".corrupt.json");
    }

    public UsageSampleDocument Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new UsageSampleDocument();

            var json = File.ReadAllText(_filePath);
            var stored = JsonSerializer.Deserialize<UsageSampleDocument>(json, Options);
            if (stored == null)
                return new UsageSampleDocument();

            stored.Samples ??= new List<UsageSample>();
            stored.Samples = stored.Samples
                .OrderBy(s => s.TimestampUtc)
                .ToList();
            return stored;
        }
        catch (Exception)
        {
            try
            {
                if (File.Exists(_filePath))
                    File.Copy(_filePath, _corruptBackupPath, overwrite: true);
            }
            catch
            {
                // Ignore backup failures and return an empty document.
            }

            return new UsageSampleDocument();
        }
    }

    public void Save(UsageSampleDocument document)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var payload = new UsageSampleDocument
        {
            Version = document.Version <= 0 ? 1 : document.Version,
            CycleStartUtc = document.CycleStartUtc,
            Samples = document.Samples
                .OrderBy(s => s.TimestampUtc)
                .ToList()
        };

        var json = JsonSerializer.Serialize(payload, Options);
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
