using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.Services;

public sealed class JsonPlanStore : IPlanStore
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorQuotaProgress");

    private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");
    private static readonly string CorruptBackupPath = Path.Combine(AppDataPath, "settings.corrupt.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
            return settings ?? new AppSettings();
        }
        catch (Exception)
        {
            if (File.Exists(SettingsPath))
            {
                File.Copy(SettingsPath, CorruptBackupPath, overwrite: true);
            }
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataPath);

        var json = JsonSerializer.Serialize(settings, Options);
        var tempPath = SettingsPath + ".tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, overwrite: true);
    }
}
