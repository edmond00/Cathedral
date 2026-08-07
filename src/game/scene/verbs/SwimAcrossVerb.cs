using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Swims a <see cref="WaterCrossingPointOfInterest"/> to reach the area on the far side.
///
/// <para>Refused outright when the actor is overloaded. Every other verb treats an overloaded pack as
/// a travel problem, but going into deep water with more than you can carry is the one case where it
/// is an immediate one — and being told so is more useful than a mysterious roll you cannot win.</para>
/// </summary>
public class SwimAcrossVerb : Verb
{
    public override string VerbId         => "swim_across";
    public override string DisplayName    => "Swim Across";
    public override int    BaseDifficulty => 5;

    /// <summary>What a success teaches: staying up and moving in open water.</summary>
    public override string? GrantedModusMentisId(Element? target) => "natation";

    public override int DifficultyFor(Element? target)
        => target is WaterCrossingPointOfInterest water ? water.Difficulty : BaseDifficulty;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not WaterCrossingPointOfInterest water) return false;
        if (!water.Touches(pov.Where)) return false;

        // An overloaded swimmer sinks. Checked here rather than left to the dice so the action simply
        // is not offered, the way a locked door is not offered as "open".
        return actor == null || !actor.IsOverloaded;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"swim across {DefiniteTarget(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not WaterCrossingPointOfInterest water) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(water.Other(pov.Where)) };
    }

    public override IReadOnlyList<Wound?> FailurePenalties(Element? target)
        => target is WaterCrossingPointOfInterest water ? water.FailurePenalties() : NoPenalty;

    // ── Routine recording ─────────────────────────────────────────────────────

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is WaterCrossingPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
