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
        if (stored.ActiveCycle != null
            && stored.ActiveCycle.NextRenewal > stored.ActiveCycle.CycleStart)
        {
            cycle = new QuotaCycle
            {
                RenewalDay = stored.ActiveCycle.RenewalDay != 0
                    ? stored.ActiveCycle.RenewalDay
                    : stored.ActiveCycle.CycleStart.Day,
                CycleStart = stored.ActiveCycle.CycleStart,
                NextRenewal = stored.ActiveCycle.NextRenewal
            };
        }

        return new AppSettings
        {
            Version = 2,
            ActiveCycle = cycle,
            RunAtStartup = stored.RunAtStartup,
            StartInNotificationTray = stored.StartInNotificationTray,
            AutoSyncEnabled = stored.AutoSyncEnabled,
            SyncIntervalHours = SyncInterval.Clamp(stored.SyncIntervalHours),
            ShowChartView = stored.ShowChartView,
            CursorAccountConnected = stored.CursorAccountConnected,
            LastUsageSyncUtc = stored.LastUsageSyncUtc,
            WindowX = stored.WindowX,
            WindowY = stored.WindowY
        };
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
                NextRenewal = settings.ActiveCycle.NextRenewal
            };
        }

        return new StoredSettings
        {
            Version = 2,
            ActiveCycle = cycle,
            RunAtStartup = settings.RunAtStartup,
            StartInNotificationTray = settings.StartInNotificationTray,
            AutoSyncEnabled = settings.AutoSyncEnabled,
            SyncIntervalHours = SyncInterval.Clamp(settings.SyncIntervalHours),
            ShowChartView = settings.ShowChartView,
            CursorAccountConnected = settings.CursorAccountConnected,
            LastUsageSyncUtc = settings.LastUsageSyncUtc,
            WindowX = settings.WindowX,
            WindowY = settings.WindowY
        };
    }

    private sealed class StoredSettings
    {
        public int Version { get; set; } = 2;
        public StoredCycle? ActiveCycle { get; set; }
        public bool RunAtStartup { get; set; }
        public bool StartInNotificationTray { get; set; } = true;
        public bool AutoSyncEnabled { get; set; } = true;
        public int SyncIntervalHours { get; set; } = 1;
        public bool ShowChartView { get; set; }
        public bool CursorAccountConnected { get; set; }
        public DateTimeOffset? LastUsageSyncUtc { get; set; }
        public int? WindowX { get; set; }
        public int? WindowY { get; set; }
    }

    private sealed class StoredCycle
    {
        public int RenewalDay { get; set; }
        public DateTime CycleStart { get; set; }
        public DateTime NextRenewal { get; set; }
    }
}
