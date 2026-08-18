using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Descends a <see cref="StairPointOfInterest"/> from the top area to the bottom area.
/// Only possible when the player is in the <see cref="StairPointOfInterest.TopArea"/>.
/// </summary>
public class GoDownStairsVerb : Verb
{
    public override string VerbId         => "go_down_stairs";
    public override string DisplayName    => "Go Down";
    public override int    BaseDifficulty => 1;

    /// <summary>As going up: a stair asks nothing of the hands.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>What a success teaches: stairs are footing, and going down is the harder half.</summary>
    public override string? GrantedModusMentisId(Element? target) => "surefoot";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not StairPointOfInterest stair) return false;
        return pov.Where.Id == stair.TopArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"descend {DefiniteTarget(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not StairPointOfInterest stair) return System.Array.Empty<Outcome>();
        return new[] { new AreaMoveOutcome(stair.BottomArea) };
    }

    // ── Routine recording ─────────────────────────────────────────────────────
    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is StairPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
