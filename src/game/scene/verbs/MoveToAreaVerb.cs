using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Verb for moving the PoV to a different area.
/// Possible when the target is a reachable <see cref="Area"/> connected via the scene's directed graph.
/// </summary>
public class MoveToAreaVerb : Verb
{
    public override string VerbId         => "move";
    public override string DisplayName    => "Move";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not Area targetArea) return false;
        if (targetArea.Id == pov.Where.Id) return false; // can't move to same area

        return scene.AreaGraph.TryGetValue(pov.Where.Id, out var reachable)
            && reachable.Contains(targetArea.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is Area area && !string.IsNullOrWhiteSpace(area.TransitionDescription))
            return area.TransitionDescription;
        return $"move to {DefiniteTarget(target)}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not Area targetArea) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(targetArea) };
    }

    // ── Routine recording ─────────────────────────────────────────────────────
    // Moving between areas is the first recordable verb. It can later decline for special areas
    // (e.g. one-way/event areas) by inspecting the target here.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is Area;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is Area area
            ? new RoutineTargetRef(RoutineTargetKind.Area, area.ReferenceLemma, area.DisplayName)
            : null;

    // Moving starts a fresh narration phase at the destination area.
    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
