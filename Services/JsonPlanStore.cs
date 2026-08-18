using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public sealed class JsonPlanStore : IPlanStore
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorUsageProgress");

    private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");
    private static readonly string CorruptBackupPath = Path.Combine(AppDataPath, "settings.corrupt.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var blank = new AppSettings();
                Save(blank);
                return blank;
            }

            var json = File.ReadAllText(SettingsPath);
            var stored = JsonSerializer.Deserialize<StoredSettings>(json, Options);
            return stored == null ? new AppSettings() : ToAppSettings(stored);
        }
        catch (Exception)
        {
            if (File.Exists(SettingsPath))
                File.Copy(SettingsPath, CorruptBackupPath, overwrite: true);

            var blank = new AppSettings();
            try { Save(blank); } catch { }
            return blank;
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataPath);

        var json = JsonSerializer.Serialize(ToStoredSettings(settings), Options);
        var tempPath = SettingsPath + ".tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, overwrite: true);
    }

    private static AppSettings ToAppSettings(StoredSettings stored)
    {
        QuotaCycle? cycle = null;
        if (stored.ActiveCycle != null)
        {
            cycle = new QuotaCycle
            {
                RenewalDay = stored.ActiveCycle.RenewalDay,
                CycleStart = stored.ActiveCycle.CycleStart,
                NextRenewal = stored.ActiveCycle.NextRenewal,
                Edits = MigrateEdits(stored.ActiveCycle)
            };
        }

        return new AppSettings
        {
            Version = stored.Version,
            RenewalDay = stored.RenewalDay,
            ActiveCycle = cycle,
            RunAtStartup = stored.RunAtStartup
        };
    }

    private static List<QuotaDayEdit> MigrateEdits(StoredCycle stored)
    {
        if (stored.Edits is { Count: > 0 })
            return stored.Edits.Where(e => e.HasAnyValue).ToList();

        if (stored.Days == null)
            return new List<QuotaDayEdit>();

        var edits = new List<QuotaDayEdit>();
        foreach (var day in stored.Days)
        {
            if (!day.CursorModelsIsManual && !day.OtherModelsIsManual)
                continue;

            edits.Add(new QuotaDayEdit
            {
                DayNumber = day.DayNumber,
                CursorModelsPercent = day.CursorModelsIsManual ? day.CursorModelsPercent : null,
                OtherModelsPercent = day.OtherModelsIsManual ? day.OtherModelsPercent : null
            });
        }

        return edits;
    }

    private static StoredSettings ToStoredSettings(AppSettings settings)
    {
        StoredCycle? cycle = null;
        if (settings.ActiveCycle != null)
        {
            cycle = new StoredCycle
            {
                RenewalDay = settings.ActiveCycle.RenewalDay,
                CycleStart = settings.ActiveCycle.CycleStart,
                NextRenewal = settings.ActiveCycle.NextRenewal,
                Edits = settings.ActiveCycle.Edits.Where(e => e.HasAnyValue).ToList()
            };
        }

        return new StoredSettings
        {
            Version = settings.Version,
            RenewalDay = settings.RenewalDay,
            ActiveCycle = cycle,
            RunAtStartup = settings.RunAtStartup
        };
    }

    private sealed class StoredSettings
    {
        public int Version { get; set; } = 1;
        public int? RenewalDay { get; set; }
        public StoredCycle? ActiveCycle { get; set; }
        public bool RunAtStartup { get; set; }
    }

    private sealed class StoredCycle
    {
        public int RenewalDay { get; set; }
        public DateTime CycleStart { get; set; }
        public DateTime NextRenewal { get; set; }
        public List<QuotaDayEdit>? Edits { get; set; }
        public List<QuotaDayEntry>? Days { get; set; }
    }
}
