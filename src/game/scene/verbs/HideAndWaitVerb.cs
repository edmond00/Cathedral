using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Gets out of sight and stays there until somebody comes or goes.
///
/// <para>Where <see cref="SitAndWaitVerb"/> spends exactly one period, this spends as many as it
/// takes — walking the day forward one period at a time and stopping the moment the location's
/// roster changes. If nothing happens it gives up after a full day less one period, so the character
/// never emerges at the same hour they went in with nothing to show for it.</para>
///
/// <para>The reward is the notice, not the time: what you get for hiding is knowing who arrived and
/// who left, which is the only way to learn a person's comings and goings without following them.</para>
/// </summary>
public class HideAndWaitVerb : Verb
{
    public override string VerbId         => "hide_and_wait";
    public override string DisplayName    => "Hide and Wait";
    public override int    BaseDifficulty => 2;

    /// <summary>What a success teaches: being somewhere without being seen there.</summary>
    public override string? GrantedModusMentisId(Element? target) => "stealth";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => target is HidingPointOfInterest && pov.Where.PointsOfInterest.Contains(target);

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"hide in {DefiniteTarget(target)} and watch";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var (period, notice) = WaitForChange(scene, pov.When);

        var reports = new List<OutcomeReport> { new TimeShiftOutcome(period) };
        if (notice != null) reports.Add(notice);
        return reports;
    }

    /// <summary>
    /// Walks the day forward from <paramref name="from"/> until the set of NPCs present anywhere in
    /// the location differs from the set present now, and reports what changed.
    ///
    /// <para>Presence is compared at the level of the whole location, not the current area: somebody
    /// crossing from one room to another has not come or gone, and a hidden watcher would not count
    /// it. Capped at one period short of a full day, so the worst case is still a change of hour.</para>
    /// </summary>
    private static (TimePeriod Period, OutcomeReport? Notice) WaitForChange(Scene scene, TimePeriod from)
    {
        var before = scene.PresentAt(from).Select(n => n.Id).ToHashSet();

        for (int step = 1; step < TimePeriodExtensions.PeriodsPerDay; step++)
        {
            var period = from.Advance(step);
            var now    = scene.PresentAt(period);
            var nowIds = now.Select(n => n.Id).ToHashSet();

            var arrived = now.Where(n => !before.Contains(n.Id)).ToList();
            var left    = scene.PresentAt(from).Where(n => !nowIds.Contains(n.Id)).ToList();

            if (arrived.Count == 0 && left.Count == 0) continue;

            return (period, new NoticeOutcome(DescribeChange(arrived, left), VerbaliseChange(arrived, left)));
        }

        // Nobody came and nobody went all day. Still worth the hours — the hour has moved on — but say
        // so plainly rather than inventing an arrival.
        return (from.Advance(TimePeriodExtensions.PeriodsPerDay - 1),
                new NoticeOutcome("Nobody came and nobody left", "watched a whole day go by without a soul moving"));
    }

    private static string DescribeChange(List<SceneNpc> arrived, List<SceneNpc> left)
    {
        var parts = new List<string>();
        if (arrived.Count > 0) parts.Add($"arrived: {Names(arrived)}");
        if (left.Count > 0)    parts.Add($"left: {Names(left)}");
        return string.Join("; ", parts);
    }

    private static string VerbaliseChange(List<SceneNpc> arrived, List<SceneNpc> left)
    {
        var parts = new List<string>();
        if (arrived.Count > 0) parts.Add($"saw {Names(arrived)} arrive");
        if (left.Count > 0)    parts.Add($"saw {Names(left)} go");
        return string.Join(" and ", parts);
    }

    private static string Names(List<SceneNpc> npcs)
    {
        var names = npcs.Select(n => n.Entity.DisplayName).ToList();
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];
    }

    // ── Routine recording ─────────────────────────────────────────────────────
    // Not recordable: how long the wait lasts depends on who happens to be moving that day, so a
    // replayed chain built on "and then it was evening" would be wrong the next time round.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
