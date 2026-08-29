using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public sealed class DataBackupService : IDataBackupService
{
    private readonly IPlanStore _planStore;
    private readonly IUsageSampleStore _sampleStore;

    public DataBackupService(IPlanStore planStore, IUsageSampleStore sampleStore)
    {
        _planStore = planStore;
        _sampleStore = sampleStore;
    }

    public void WriteBackup(Stream destination, DateTimeOffset createdUtc)
    {
        var settingsJson = JsonPlanStore.Serialize(_planStore.Load());
        var samplesJson = JsonUsageSampleStore.Serialize(_sampleStore.Load());
        DataBackupArchive.Write(destination, settingsJson, samplesJson, createdUtc);
    }

    public DataRestoreResult RestoreBackup(Stream source)
    {
        var read = DataBackupArchive.Read(source);
        if (!read.Success)
            return DataRestoreResult.Fail(read.ErrorMessage ?? "Could not read the backup file.");

        if (!JsonPlanStore.TryDeserialize(read.SettingsJson, out var settings))
            return DataRestoreResult.Fail("The backup settings.json is not valid.");

        if (!JsonUsageSampleStore.TryDeserialize(read.SamplesJson, out var samples))
            return DataRestoreResult.Fail("The backup usage-samples.json is not valid.");

        AppSettings? previousSettings = null;
        UsageSampleDocument? previousSamples = null;
        try
        {
            previousSettings = _planStore.Load();
            previousSamples = _sampleStore.Load();
            _planStore.Save(settings);
            try
            {
                _sampleStore.Save(samples);
            }
            catch (Exception ex)
            {
                try { _planStore.Save(previousSettings); } catch { }
                return DataRestoreResult.Fail(
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "Could not restore usage samples."
                        : ex.Message);
            }

            return DataRestoreResult.Ok();
        }
        catch (Exception ex)
        {
            if (previousSettings != null)
            {
                try { _planStore.Save(previousSettings); } catch { }
            }

            if (previousSamples != null)
            {
                try { _sampleStore.Save(previousSamples); } catch { }
            }

            return DataRestoreResult.Fail(
                string.IsNullOrWhiteSpace(ex.Message)
                    ? "Could not restore the backup file."
                    : ex.Message);
        }
    }
}
