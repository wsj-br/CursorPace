namespace CursorPace.Models;

public static class SyncSchedule
{
    public static readonly TimeSpan StartupSkipWindow = TimeSpan.FromMinutes(20);

    public static bool ShouldRefreshOnStart(
        bool autoSyncEnabled,
        bool accountConnected,
        DateTimeOffset now,
        DateTimeOffset? lastUpdateUtc,
        int intervalHours)
    {
        if (!autoSyncEnabled || !accountConnected)
            return false;

        if (lastUpdateUtc is null)
            return true;

        var age = now - lastUpdateUtc.Value;
        if (age < StartupSkipWindow)
            return false;

        intervalHours = SyncInterval.Clamp(intervalHours);
        if (age >= TimeSpan.FromHours(intervalHours))
            return true;

        var nowLocal = now.DateTime;
        var lastLocal = lastUpdateUtc.Value.ToOffset(now.Offset).DateTime;
        return lastLocal < CurrentAlignedLocal(nowLocal, intervalHours);
    }

    public static DateTime CurrentAlignedLocal(DateTime now, int intervalHours)
    {
        intervalHours = SyncInterval.Clamp(intervalHours);
        var alignedHour = now.Hour - (now.Hour % intervalHours);
        return new DateTime(now.Year, now.Month, now.Day, alignedHour, 0, 0, now.Kind);
    }

    public static DateTime NextAlignedLocal(DateTime now, int intervalHours)
    {
        intervalHours = SyncInterval.Clamp(intervalHours);
        return CurrentAlignedLocal(now, intervalHours).AddHours(intervalHours);
    }
}
