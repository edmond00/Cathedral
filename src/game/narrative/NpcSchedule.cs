using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Maps each <see cref="TimePeriod"/> to the <see cref="Area"/> where an NPC is present,
/// or null when the NPC is absent during that period.
///
/// <para>Areas are held by reference, not by name. A schedule is built inside the same
/// <c>SceneFactory.Build</c> call that constructs the areas and is never serialised, so the
/// reference is always to an area of that same scene instance. Matching by
/// <see cref="Element.DisplayName"/> — which this used to do — silently placed one NPC in every
/// building at once as soon as two buildings each contained a room called "Hall".</para>
///
/// Construction helpers:
///   NpcSchedule.Always(clearing)            — present at the same area all day
///   NpcSchedule.ExceptDuring(clearing, ...) — absent during listed periods
///   NpcSchedule.OnlyDuring(clearing, ...)   — present only during listed periods
///   NpcSchedule.Roaming(dict)               — full custom map
/// </summary>
public class NpcSchedule
{
    // null value = NPC absent during this period
    private readonly Dictionary<TimePeriod, Area?> _areaByPeriod;

    private NpcSchedule(Dictionary<TimePeriod, Area?> map)
    {
        _areaByPeriod = map;
    }

    /// <summary>Returns the area the NPC should be at during <paramref name="period"/>, or null if absent.</summary>
    public Area? GetArea(TimePeriod period)
        => _areaByPeriod.TryGetValue(period, out var area) ? area : null;

    /// <summary>All (period, area) pairs where the NPC is present.</summary>
    public IEnumerable<(TimePeriod Period, Area Area)> ActivePeriods
        => _areaByPeriod
            .Where(kv => kv.Value != null)
            .Select(kv => (kv.Key, kv.Value!));

    // ── Factory helpers ───────────────────────────────────────────────────────

    /// <summary>NPC is always at the same area.</summary>
    public static NpcSchedule Always(Area area)
    {
        var map = new Dictionary<TimePeriod, Area?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
            map[p] = area;
        return new NpcSchedule(map);
    }

    /// <summary>NPC is at <paramref name="area"/> during all periods except those listed.</summary>
    public static NpcSchedule ExceptDuring(Area area, params TimePeriod[] absentPeriods)
    {
        var absent = new HashSet<TimePeriod>(absentPeriods);
        var map = new Dictionary<TimePeriod, Area?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
            map[p] = absent.Contains(p) ? null : area;
        return new NpcSchedule(map);
    }

    /// <summary>NPC is at <paramref name="area"/> only during the listed periods; absent otherwise.</summary>
    public static NpcSchedule OnlyDuring(Area area, params TimePeriod[] presentPeriods)
    {
        var present = new HashSet<TimePeriod>(presentPeriods);
        var map = new Dictionary<TimePeriod, Area?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
            map[p] = present.Contains(p) ? area : null;
        return new NpcSchedule(map);
    }

    /// <summary>
    /// Full custom schedule: the NPC may occupy different areas at different periods.
    /// Periods not present in <paramref name="schedule"/> are treated as absent.
    /// </summary>
    public static NpcSchedule Roaming(Dictionary<TimePeriod, Area?> schedule)
        => new NpcSchedule(new Dictionary<TimePeriod, Area?>(schedule));

    /// <summary>
    /// Replaces the area for one period in place. Used by <c>BuildingSchedule.StaffPublicHall</c>,
    /// which decides who covers an otherwise-empty public hall only after every worker's schedule
    /// has been built.
    /// </summary>
    public void Set(TimePeriod period, Area? area) => _areaByPeriod[period] = area;
}
