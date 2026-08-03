using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Descends a <see cref="CliffPointOfInterest"/> from the top area to the bottom area.
/// Only possible when the player is in <see cref="CliffPointOfInterest.TopArea"/>.
/// Difficulty is 6 (8 if icy).
/// </summary>
public class ClimbDownVerb : Verb
{
    public override string VerbId      => "climb_down";
    public override string DisplayName => "Climb Down";
    public override int    BaseDifficulty => 6;

    /// <summary>What a success teaches: hand and foot on rock.</summary>
    public override string? GrantedModusMentisId(Element? target) => "clambering";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not CliffPointOfInterest cliff) return false;
        return pov.Where.Id == cliff.TopArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"climb down {DefiniteTarget(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not CliffPointOfInterest cliff) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(cliff.BottomArea) };
    }

    // A failed descent is a fall: usually a scare, sometimes a fracture. Extra nulls keep injury the minority.
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null,
        new AnkleFractureRightWound(),
        new KneeFractureLeftWound(),
        new TibiaFractureRightWound(),
        new BrokenFootLeftWound(),
    };

    // ── Routine recording ─────────────────────────────────────────────────────
    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is CliffPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
