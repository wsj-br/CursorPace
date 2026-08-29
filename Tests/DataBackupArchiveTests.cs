using System.IO.Compression;
using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class DataBackupArchiveTests
{
    [Fact]
    public void WriteThenRead_RoundTripsSettingsAndSamples()
    {
        var settings = new AppSettings
        {
            SyncIntervalHours = 6,
            RunAtStartup = true,
            ActiveCycle = new QuotaCycle
            {
                RenewalDay = 1,
                CycleStart = new DateTime(2026, 8, 1, 8, 0, 0),
                NextRenewal = new DateTime(2026, 9, 1, 8, 0, 0)
            }
        };
        var samples = new UsageSampleDocument
        {
            CycleStartUtc = DateTimeOffset.Parse("2026-08-01T07:00:00Z"),
            Samples =
            [
                new UsageSample
                {
                    TimestampUtc = DateTimeOffset.Parse("2026-08-03T12:00:00Z"),
                    CursorModelsPercent = 12m,
                    OtherModelsPercent = 8m
                }
            ]
        };

        using var stream = new MemoryStream();
        DataBackupArchive.Write(
            stream,
            JsonPlanStore.Serialize(settings),
            JsonUsageSampleStore.Serialize(samples),
            DateTimeOffset.Parse("2026-08-18T10:40:00Z"));

        stream.Position = 0;
        var read = DataBackupArchive.Read(stream);

        Assert.True(read.Success);
        Assert.True(JsonPlanStore.TryDeserialize(read.SettingsJson, out var loadedSettings));
        Assert.True(JsonUsageSampleStore.TryDeserialize(read.SamplesJson, out var loadedSamples));
        Assert.Equal(6, loadedSettings.SyncIntervalHours);
        Assert.True(loadedSettings.RunAtStartup);
        Assert.Equal(settings.ActiveCycle.CycleStart, loadedSettings.ActiveCycle!.CycleStart);
        Assert.Equal(12m, loadedSamples.Samples[0].CursorModelsPercent);
        Assert.Equal(8m, loadedSamples.Samples[0].OtherModelsPercent);
    }

    [Fact]
    public void Read_MissingSettings_Fails()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(DataBackupArchive.SamplesEntryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("""{"version":1,"samples":[]}""");
        }

        stream.Position = 0;
        var read = DataBackupArchive.Read(stream);

        Assert.False(read.Success);
        Assert.Contains("settings.json", read.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_MissingSamples_UsesEmptyDocument()
    {
        var settingsJson = JsonPlanStore.Serialize(new AppSettings { AutoSyncEnabled = false });
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(DataBackupArchive.SettingsEntryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(settingsJson);
        }

        stream.Position = 0;
        var read = DataBackupArchive.Read(stream);

        Assert.True(read.Success);
        Assert.True(JsonUsageSampleStore.TryDeserialize(read.SamplesJson, out var samples));
        Assert.Empty(samples.Samples);
    }

    [Fact]
    public void Read_NotAZip_Fails()
    {
        using var stream = new MemoryStream("{ not a zip"u8.ToArray());
        var read = DataBackupArchive.Read(stream);

        Assert.False(read.Success);
        Assert.False(string.IsNullOrWhiteSpace(read.ErrorMessage));
    }

    [Fact]
    public void DataBackupService_RestoreReplacesStoreContents()
    {
        var plan = new MemoryPlanStore
        {
            Settings = new AppSettings { SyncIntervalHours = 2, StartInNotificationTray = false }
        };
        var samples = new MemorySampleStore
        {
            Document = new UsageSampleDocument
            {
                Samples =
                [
                    new UsageSample
                    {
                        TimestampUtc = DateTimeOffset.Parse("2026-08-03T12:00:00Z"),
                        CursorModelsPercent = 5m,
                        OtherModelsPercent = 7m
                    }
                ]
            }
        };
        var service = new DataBackupService(plan, samples);
        using var archive = new MemoryStream();
        service.WriteBackup(archive, DateTimeOffset.Parse("2026-08-18T12:00:00Z"));

        plan.Settings = new AppSettings();
        samples.Document = new UsageSampleDocument();

        archive.Position = 0;
        var result = service.RestoreBackup(archive);

        Assert.True(result.Success);
        Assert.Equal(2, plan.Settings.SyncIntervalHours);
        Assert.False(plan.Settings.StartInNotificationTray);
        Assert.Equal(5m, samples.Document.Samples[0].CursorModelsPercent);
    }

    [Fact]
    public void DataBackupService_RestoreSaveFailure_ReturnsErrorAndKeepsSettings()
    {
        var sourcePlan = new MemoryPlanStore
        {
            Settings = new AppSettings { SyncIntervalHours = 12 }
        };
        var sourceSamples = new MemorySampleStore();
        using var archive = new MemoryStream();
        new DataBackupService(sourcePlan, sourceSamples)
            .WriteBackup(archive, DateTimeOffset.Parse("2026-08-18T12:00:00Z"));

        var destPlan = new MemoryPlanStore
        {
            Settings = new AppSettings { SyncIntervalHours = 2 }
        };
        var destSamples = new ThrowingSampleStore();
        archive.Position = 0;
        var result = new DataBackupService(destPlan, destSamples).RestoreBackup(archive);

        Assert.False(result.Success);
        Assert.Equal(2, destPlan.Settings.SyncIntervalHours);
        Assert.Contains("disk full", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MemoryPlanStore : IPlanStore
    {
        public AppSettings Settings { get; set; } = new();
        public AppSettings Load() => Settings;
        public void Save(AppSettings settings) => Settings = settings;
    }

    private sealed class MemorySampleStore : IUsageSampleStore
    {
        public UsageSampleDocument Document { get; set; } = new();
        public UsageSampleDocument Load() => Document;
        public void Save(UsageSampleDocument document) => Document = document;
    }

    private sealed class ThrowingSampleStore : IUsageSampleStore
    {
        public UsageSampleDocument Load() => new();
        public void Save(UsageSampleDocument document) =>
            throw new IOException("disk full");
    }
}
