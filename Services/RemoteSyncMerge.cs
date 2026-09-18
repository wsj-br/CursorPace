using CursorPace.Models;

namespace CursorPace.Services;

/// <summary>
/// Pure client-side merge for sync-server canonical state.
/// Mirrors the server rules: sample union by instant, newest cycle bounds win,
/// history union by start date.
/// </summary>
public static class RemoteSyncMerge
{
    public static List<UsageSample> UnionSamples(
        IReadOnlyList<UsageSample> local,
        IReadOnlyList<UsageSample> remote)
    {
        var merged = new List<UsageSample>(local.Count + remote.Count);
        var seen = new HashSet<long>();
        foreach (var sample in local)
        {
            if (seen.Add(sample.TimestampUtc.UtcTicks))
                merged.Add(sample);
        }

        foreach (var sample in remote)
        {
            if (seen.Add(sample.TimestampUtc.UtcTicks))
                merged.Add(sample);
        }

        merged.Sort((a, b) => a.TimestampUtc.UtcTicks.CompareTo(b.TimestampUtc.UtcTicks));
        return merged;
    }

    public static DateTimeOffset? LatestCycleStartUtc(DateTimeOffset? local, DateTimeOffset? remote)
    {
        if (local == null)
            return remote;
        if (remote == null)
            return local;
        return remote > local ? remote : local;
    }

    /// <summary>
    /// Newest bounds win: later cycle start, then later renewal, then the local copy.
    /// </summary>
    public static QuotaCycle? NewestCycle(QuotaCycle? local, QuotaCycle? remote)
    {
        if (local == null)
            return remote;
        if (remote == null)
            return local;
        if (remote.CycleStart > local.CycleStart)
            return remote;
        if (remote.CycleStart < local.CycleStart)
            return local;
        return remote.NextRenewal > local.NextRenewal ? remote : local;
    }

    public static List<QuotaCycle> UnionHistory(
        IReadOnlyList<QuotaCycle> local,
        IReadOnlyList<QuotaCycle> remote)
    {
        var merged = new Dictionary<DateTime, QuotaCycle>();
        foreach (var cycle in local)
            AddHistory(merged, cycle);
        foreach (var cycle in remote)
            AddHistory(merged, cycle);

        var result = new List<QuotaCycle>(merged.Values);
        result.Sort((a, b) => a.CycleStart.CompareTo(b.CycleStart));
        return result;
    }

    /// <summary>
    /// Merges cycle state. A superseded local active cycle is preserved in history.
    /// </summary>
    public static (QuotaCycle? Active, List<QuotaCycle> History) MergeCycles(
        QuotaCycle? localActive,
        IReadOnlyList<QuotaCycle> localHistory,
        QuotaCycle? remoteActive,
        IReadOnlyList<QuotaCycle> remoteHistory)
    {
        var active = NewestCycle(localActive, remoteActive);
        var history = UnionHistory(localHistory, remoteHistory);

        if (localActive != null
            && active != null
            && localActive.CycleStart.Date != active.CycleStart.Date)
        {
            var merged = new Dictionary<DateTime, QuotaCycle>();
            foreach (var cycle in history)
                AddHistory(merged, cycle);
            AddHistory(merged, localActive);
            history = new List<QuotaCycle>(merged.Values);
            history.Sort((a, b) => a.CycleStart.CompareTo(b.CycleStart));
        }

        if (active != null)
            history.RemoveAll(cycle => cycle.CycleStart.Date == active.CycleStart.Date);

        return (active, history);
    }

    private static void AddHistory(Dictionary<DateTime, QuotaCycle> merged, QuotaCycle cycle)
    {
        var key = cycle.CycleStart.Date;
        if (!merged.TryGetValue(key, out var existing) || cycle.NextRenewal > existing.NextRenewal)
            merged[key] = cycle;
    }
}
