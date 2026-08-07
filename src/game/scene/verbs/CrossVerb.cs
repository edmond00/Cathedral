using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Forces a way across a <see cref="CrossingPointOfInterest"/> — brambles, mud, a fallen trunk, a
/// scree slope, a boundary hedge — to reach the area on the other side.
///
/// <para>Unlike a path, a crossing is symmetric but not free: difficulty and failure penalties come
/// from the obstacle rather than from the verb, so one verb covers a mud puddle you will nearly
/// always manage and a thicket that will usually turn you back bleeding.</para>
/// </summary>
public class CrossVerb : Verb
{
    public override string VerbId         => "cross";
    public override string DisplayName    => "Cross";
    public override int    BaseDifficulty => 4;

    /// <summary>What a success teaches: depends on what the obstacle asked of you — see the crossing.</summary>
    public override string? GrantedModusMentisId(Element? target)
        => target is CrossingPointOfInterest crossing ? crossing.ModusMentisId : "surefoot";

    public override int DifficultyFor(Element? target)
        => target is CrossingPointOfInterest crossing ? crossing.Difficulty : BaseDifficulty;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => target is CrossingPointOfInterest crossing && crossing.Touches(pov.Where);

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"push through {DefiniteTarget(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not CrossingPointOfInterest crossing) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(crossing.Other(pov.Where)) };
    }

    public override IReadOnlyList<Wound?> FailurePenalties(Element? target)
        => target is CrossingPointOfInterest crossing ? crossing.FailurePenalties() : NoPenalty;

    // ── Routine recording ─────────────────────────────────────────────────────
    // A route the character has already forced once is exactly what a routine is for.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is CrossingPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
