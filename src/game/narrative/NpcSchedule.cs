using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Maps each <see cref="TimePeriod"/> to the node ID where an NPC is present,
/// or null when the NPC is absent during that period.
///
/// Construction helpers:
///   NpcSchedule.Always("clearing")            — present at the same node all day
///   NpcSchedule.ExceptDuring("clearing", ...) — absent during listed periods
///   NpcSchedule.OnlyDuring("clearing", ...)   — present only during listed periods
///   NpcSchedule.Roaming(dict)                 — full custom map
/// </summary>
public class NpcSchedule
{
    // null value = NPC absent during this period
    private readonly Dictionary<TimePeriod, string?> _nodeIdByPeriod;

    private NpcSchedule(Dictionary<TimePeriod, string?> map)
    {
        _nodeIdByPeriod = map;
    }

    /// <summary>Returns the node ID the NPC should be at during <paramref name="period"/>, or null if absent.</summary>
    public string? GetNodeId(TimePeriod period)
        => _nodeIdByPeriod.TryGetValue(period, out var id) ? id : null;

    /// <summary>All (period, nodeId) pairs where the NPC is present.</summary>
    public IEnumerable<(TimePeriod Period, string NodeId)> ActivePeriods
        => _nodeIdByPeriod
            .Where(kv => kv.Value != null)
            .Select(kv => (kv.Key, kv.Value!));

    // ── Factory helpers ───────────────────────────────────────────────────────

    /// <summary>NPC is always at the same node.</summary>
    public static NpcSchedule Always(string nodeId)
    {
        var map = new Dictionary<TimePeriod, string?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
            map[p] = nodeId;
        return new NpcSchedule(map);
    }

    /// <summary>NPC is at <paramref name="nodeId"/> during all periods except those listed.</summary>
    public static NpcSchedule ExceptDuring(string nodeId, params TimePeriod[] absentPeriods)
    {
        var absent = new HashSet<TimePeriod>(absentPeriods);
        var map = new Dictionary<TimePeriod, string?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
            map[p] = absent.Contains(p) ? null : nodeId;
        return new NpcSchedule(map);
    }

    /// <summary>NPC is at <paramref name="nodeId"/> only during the listed periods; absent otherwise.</summary>
    public static NpcSchedule OnlyDuring(string nodeId, params TimePeriod[] presentPeriods)
    {
        var present = new HashSet<TimePeriod>(presentPeriods);
        var map = new Dictionary<TimePeriod, string?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
            map[p] = present.Contains(p) ? nodeId : null;
        return new NpcSchedule(map);
    }

    /// <summary>
    /// Full custom schedule: the NPC may occupy different nodes at different periods.
    /// Periods not present in <paramref name="schedule"/> are treated as absent.
    /// </summary>
    public static NpcSchedule Roaming(Dictionary<TimePeriod, string?> schedule)
        => new NpcSchedule(new Dictionary<TimePeriod, string?>(schedule));
}
