using CursorPace.Models;

namespace CursorPace.Services;

public static class CycleHistory
{
    public static QuotaCycle CopyBounds(QuotaCycle cycle) =>
        new()
        {
            RenewalDay = cycle.CycleStart.Day,
            CycleStart = cycle.CycleStart,
            NextRenewal = cycle.NextRenewal
        };

    public static bool ArchiveIfNewStartDate(AppSettings settings, DateTime newStartLocal)
    {
        var previous = settings.ActiveCycle;
        if (previous == null || previous.CycleStart.Date == newStartLocal.Date)
            return false;

        settings.CycleHistory.RemoveAll(cycle => cycle.CycleStart.Date == previous.CycleStart.Date);
        settings.CycleHistory.Add(CopyBounds(previous));
        return true;
    }
}
