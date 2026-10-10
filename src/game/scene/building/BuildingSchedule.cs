using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Builds the day schedules of the people who live in buildings.
///
/// <para>Two invariants live here rather than in each scene factory, because each factory used to
/// hand-write them and each got them slightly wrong:</para>
/// <list type="bullet">
/// <item><b>Everyone sleeps somewhere.</b> Night is always the worker's own bed. The field's workers
/// used to have <c>[Night] = null</c> — "they return to the village" — which meant they simply ceased
/// to exist after dark, in a village that had no room for them either.</item>
/// <item><b>A public hall is staffed all day.</b> A workshop whose master happens to be out at noon
/// should still have someone to trade with, or the building is shut in all but name.</item>
/// </list>
/// </summary>
public static class BuildingSchedule
{
    /// <summary>The five periods that are not Night, in order.</summary>
    public static readonly TimePeriod[] DayPeriods =
    {
        TimePeriod.Dawn, TimePeriod.Morning, TimePeriod.Noon,
        TimePeriod.Afternoon, TimePeriod.Evening,
    };

    /// <summary>
    /// A worker's day: Night in their own bed, and the five day periods split between
    /// <paramref name="workplace"/> and somewhere plausible from <paramref name="elsewhere"/>.
    /// <paramref name="awayPeriods"/> says how many of the five are spent away — 0 pins them to work
    /// all day, 1 is the reeve who steps out once.
    /// </summary>
    public static NpcSchedule ForWorker(
        Area bed, Area workplace, IReadOnlyList<Area> elsewhere, Random rng, int awayPeriods = 2)
    {
        if (bed == null)       throw new ArgumentNullException(nameof(bed));
        if (workplace == null) throw new ArgumentNullException(nameof(workplace));

        var map = new Dictionary<TimePeriod, Area?> { [TimePeriod.Night] = bed };

        // Dawn is always at work — someone lights the fire — so the away periods are drawn from the
        // remaining four. That also keeps a hall occupied at the start of every day for free.
        var candidates = DayPeriods.Skip(1).ToList();
        var away = new HashSet<TimePeriod>();
        int wanted = elsewhere.Count == 0 ? 0 : Math.Clamp(awayPeriods, 0, candidates.Count);
        while (away.Count < wanted)
            away.Add(candidates[rng.Next(candidates.Count)]);

        foreach (var period in DayPeriods)
            map[period] = away.Contains(period) ? elsewhere[rng.Next(elsewhere.Count)] : workplace;

        return NpcSchedule.Roaming(map);
    }

    /// <summary>
    /// Someone who does not work a public hall — a hand, a herder — whose day is spent across
    /// <paramref name="workplaces"/> and whose night is their own bed.
    /// </summary>
    public static NpcSchedule ForHand(Area bed, IReadOnlyList<Area> workplaces, Random rng)
    {
        if (bed == null) throw new ArgumentNullException(nameof(bed));
        if (workplaces.Count == 0) return NpcSchedule.Always(bed);

        var map = new Dictionary<TimePeriod, Area?> { [TimePeriod.Night] = bed };
        foreach (var period in DayPeriods)
            map[period] = workplaces[rng.Next(workplaces.Count)];
        return NpcSchedule.Roaming(map);
    }

    /// <summary>
    /// The day of someone who lives inside a great building and need never leave it: Night in their
    /// own bed; Dawn at prayer or washing (<paramref name="dawn"/>, when given, else work); Noon and
    /// Evening at table (<paramref name="table"/>); Morning and Afternoon at their work, one of
    /// <paramref name="work"/>. Deterministic for a given <paramref name="rng"/> state.
    /// </summary>
    public static NpcSchedule ForResident(Area bed, IReadOnlyList<Area> work, Area? table, Area? dawn, Random rng)
    {
        if (bed == null) throw new ArgumentNullException(nameof(bed));
        if (work.Count == 0) return NpcSchedule.Always(bed);
        Area Work() => work[rng.Next(work.Count)];
        return NpcSchedule.Roaming(new Dictionary<TimePeriod, Area?>
        {
            [TimePeriod.Night]     = bed,
            [TimePeriod.Dawn]      = dawn ?? Work(),
            [TimePeriod.Morning]   = Work(),
            [TimePeriod.Noon]      = table ?? Work(),
            [TimePeriod.Afternoon] = Work(),
            [TimePeriod.Evening]   = table ?? Work(),
        });
    }

    /// <summary>
    /// Fills any day period where <paramref name="hall"/> would stand empty, by moving one of
    /// <paramref name="cover"/> back to it.
    ///
    /// <para><paramref name="cover"/> is <b>only</b> the staff who may be moved — the master is
    /// deliberately not in it. A master is required to be out of their own workshop for part of the
    /// day, so pulling them back to mind an empty counter would quietly undo the rule that makes an
    /// apprentice necessary in the first place. Whoever is passed here is what the hall falls back
    /// on; if the list cannot fill a period, the hall is simply empty then, and
    /// <c>--building-audit</c> reports it.</para>
    ///
    /// <para>Cover rotates through the list rather than always falling on the first name, so a
    /// workshop with two apprentices splits the duty instead of chaining one of them to the shop for
    /// every hour the master is out.</para>
    ///
    /// <para><paramref name="locks"/>, when given, holds assignments no staffing pass may undo: whoever
    /// is locked at a period is not pulled away then, and every assignment this pass makes is locked in
    /// turn. A farm's hands mind both the longhouse and the barn — staff the barn first, then the hall
    /// with the same locks, and covering the one can never empty the other or take a trader off the
    /// one visit to their store they are held to (<see cref="EnsureVisit"/>). Not limited to halls:
    /// a store is staffed the same way.</para>
    ///
    /// Mutates the passed schedules in place.
    /// </summary>
    public static void StaffPublicHall(Area hall, IReadOnlyList<NpcSchedule> cover, StaffingLocks? locks = null)
    {
        if (hall == null || cover.Count == 0) return;

        int next = 0;
        foreach (var period in DayPeriods)
        {
            // Already manned: lock whoever mans it, so a later pass cannot take them away.
            var there = cover.FirstOrDefault(s => s.GetArea(period)?.Id == hall.Id);
            if (there != null) { locks?.Add((there, period)); continue; }

            for (int k = 0; k < cover.Count; k++)
            {
                var s = cover[(next + k) % cover.Count];
                if (locks != null && locks.Contains((s, period))) continue;
                s.Set(period, hall);
                locks?.Add((s, period));
                next += k + 1;
                break;
            }
        }
    }

    /// <summary>
    /// Makes sure <paramref name="schedule"/> spends at least one day period in <paramref name="area"/>,
    /// moving one period other than Dawn there when it never does. What holds a trader to their
    /// premises: a seller whose day never brings them to their own store is someone nobody can buy
    /// from. Call it before any staffing pass, since it may move someone off a place they covered —
    /// and pass the staffing's <paramref name="locks"/>, so the visit is locked and survives it.
    /// </summary>
    public static void EnsureVisit(NpcSchedule schedule, Area area, Random rng, StaffingLocks? locks = null)
    {
        var at = DayPeriods.Where(p => schedule.GetArea(p)?.Id == area.Id).ToList();
        if (at.Count == 0)
        {
            var candidates = DayPeriods.Skip(1).ToList();
            var period = candidates[rng.Next(candidates.Count)];
            schedule.Set(period, area);
            at.Add(period);
        }
        locks?.Add((schedule, at[0]));
    }

    /// <summary>
    /// The day periods <paramref name="schedule"/> spends away from <paramref name="workplace"/>.
    /// Used by the audit to hold masters to the rule that they must be out of their own workshop for
    /// at least one period.
    /// </summary>
    public static int AwayPeriodCount(NpcSchedule schedule, Area workplace)
        => DayPeriods.Count(p => schedule.GetArea(p)?.Id != workplace.Id);
}

/// <summary>
/// Assignments a staffing pass may not undo — see <see cref="BuildingSchedule.StaffPublicHall"/>.
/// One set per crew, shared by every pass over it.
/// </summary>
public sealed class StaffingLocks : HashSet<(NpcSchedule Schedule, TimePeriod Period)>
{
    public StaffingLocks() : base(new Comparer()) { }

    // By reference for the schedule: two schedules with the same day are still two people.
    private sealed class Comparer : IEqualityComparer<(NpcSchedule Schedule, TimePeriod Period)>
    {
        public bool Equals((NpcSchedule Schedule, TimePeriod Period) x, (NpcSchedule Schedule, TimePeriod Period) y)
            => ReferenceEquals(x.Schedule, y.Schedule) && x.Period == y.Period;
        public int GetHashCode((NpcSchedule Schedule, TimePeriod Period) o)
            => HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o.Schedule), o.Period);
    }
}
