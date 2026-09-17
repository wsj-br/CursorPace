using CursorPace.Models;

namespace CursorPace.Services;

public static class UsageSampleAppender
{
    public static bool SameCycleStart(DateTimeOffset left, DateTimeOffset right) =>
        Math.Abs((left.UtcDateTime - right.UtcDateTime).TotalSeconds) < 1;

    public static bool ApplySnapshot(
        UsageSampleDocument document,
        UsageSnapshot snapshot,
        TimeSpan minGap,
        out bool cycleRolledOver)
    {
        cycleRolledOver = document.CycleStartUtc is { } existing
            && !SameCycleStart(existing, snapshot.BillingCycleStartUtc);

        document.CycleStartUtc = snapshot.BillingCycleStartUtc;

        if (!cycleRolledOver && document.Samples.Count > 0)
        {
            var last = document.Samples[^1];
            if (snapshot.FetchedAtUtc - last.TimestampUtc < minGap)
                return false;
        }

        document.Samples.Add(new UsageSample
        {
            TimestampUtc = snapshot.FetchedAtUtc,
            CursorModelsPercent = snapshot.CursorModelsPercent,
            OtherModelsPercent = snapshot.OtherModelsPercent
        });
        return true;
    }
}
