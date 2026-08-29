using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class UsageSampleStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;
    private readonly JsonUsageSampleStore _store;

    public UsageSampleStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "cup-samples-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "usage-samples.json");
        _store = new JsonUsageSampleStore(_filePath);
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyDocument()
    {
        var document = _store.Load();

        Assert.Null(document.CycleStartUtc);
        Assert.Empty(document.Samples);
        Assert.False(File.Exists(_filePath));
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsSamplesAndCycleStart()
    {
        var cycleStart = new DateTimeOffset(2026, 8, 2, 21, 19, 47, TimeSpan.Zero);
        var document = new UsageSampleDocument
        {
            CycleStartUtc = cycleStart,
            Samples =
            [
                new UsageSample
                {
                    TimestampUtc = cycleStart.AddHours(2),
                    CursorModelsPercent = 1.5m,
                    OtherModelsPercent = 2.25m
                },
                new UsageSample
                {
                    TimestampUtc = cycleStart.AddDays(1),
                    CursorModelsPercent = 10m,
                    OtherModelsPercent = 12m
                }
            ]
        };

        _store.Save(document);
        var loaded = _store.Load();

        Assert.Equal(cycleStart, loaded.CycleStartUtc);
        Assert.Equal(2, loaded.Samples.Count);
        Assert.Equal(1.5m, loaded.Samples[0].CursorModelsPercent);
        Assert.Equal(12m, loaded.Samples[1].OtherModelsPercent);
        Assert.True(File.Exists(_filePath));
        Assert.False(File.Exists(_filePath + ".tmp"));
    }

    [Fact]
    public void Serialize_MatchesSavedFileContents()
    {
        var document = new UsageSampleDocument
        {
            CycleStartUtc = DateTimeOffset.Parse("2026-08-02T21:19:47Z"),
            Samples =
            [
                new UsageSample
                {
                    TimestampUtc = DateTimeOffset.Parse("2026-08-03T12:00:00Z"),
                    CursorModelsPercent = 4.5m,
                    OtherModelsPercent = 6m
                }
            ]
        };

        _store.Save(document);

        Assert.Equal(File.ReadAllText(_filePath), JsonUsageSampleStore.Serialize(document));
    }

    [Fact]
    public void Save_OrdersSamplesByTimestamp()
    {
        var later = DateTimeOffset.Parse("2026-08-03T12:00:00Z");
        var earlier = DateTimeOffset.Parse("2026-08-03T08:00:00Z");
        _store.Save(new UsageSampleDocument
        {
            Samples =
            [
                new UsageSample { TimestampUtc = later, CursorModelsPercent = 20m, OtherModelsPercent = 20m },
                new UsageSample { TimestampUtc = earlier, CursorModelsPercent = 10m, OtherModelsPercent = 10m }
            ]
        });

        var loaded = _store.Load();
        Assert.Equal(earlier, loaded.Samples[0].TimestampUtc);
        Assert.Equal(later, loaded.Samples[1].TimestampUtc);
    }

    [Fact]
    public void Save_ReplacingCycleStart_PersistsPrunedList()
    {
        var oldStart = DateTimeOffset.Parse("2026-07-02T21:19:47Z");
        var newStart = DateTimeOffset.Parse("2026-08-02T21:19:47Z");
        _store.Save(new UsageSampleDocument
        {
            CycleStartUtc = oldStart,
            Samples =
            [
                new UsageSample { TimestampUtc = oldStart.AddDays(1), CursorModelsPercent = 40m, OtherModelsPercent = 40m }
            ]
        });

        _store.Save(new UsageSampleDocument
        {
            CycleStartUtc = newStart,
            Samples = []
        });

        var loaded = _store.Load();
        Assert.Equal(newStart, loaded.CycleStartUtc);
        Assert.Empty(loaded.Samples);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsEmptyAndWritesBackup()
    {
        File.WriteAllText(_filePath, "{ not json");

        var loaded = _store.Load();

        Assert.Empty(loaded.Samples);
        Assert.True(File.Exists(Path.Combine(_directory, "usage-samples.corrupt.json")));
        Assert.Equal("{ not json", File.ReadAllText(_filePath));
    }

    [Fact]
    public void Load_LockedFile_DoesNotOverwriteExistingSamples()
    {
        _store.Save(new UsageSampleDocument
        {
            CycleStartUtc = DateTimeOffset.Parse("2026-08-02T21:19:47Z"),
            Samples =
            [
                new UsageSample
                {
                    TimestampUtc = DateTimeOffset.Parse("2026-08-03T12:00:00Z"),
                    CursorModelsPercent = 4.5m,
                    OtherModelsPercent = 6m
                }
            ]
        });
        var original = File.ReadAllText(_filePath);

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var loaded = _store.Load();
            Assert.Empty(loaded.Samples);
        }

        Assert.Equal(original, File.ReadAllText(_filePath));
        Assert.False(File.Exists(Path.Combine(_directory, "usage-samples.corrupt.json")));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Temp cleanup is best-effort.
        }
    }
}
