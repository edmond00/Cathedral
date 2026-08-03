using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared behaviour of the two directions of a <see cref="ScalePointOfInterest"/>. Difficulty and
/// failure penalties come from the thing being climbed rather than from the verb, so a ladder and a
/// bare wall are the same action at very different odds.
/// </summary>
public abstract class ScaleVerbBase : Verb
{
    public override int BaseDifficulty => 4;

    /// <summary>What a success teaches: getting a body up something using hands and feet.</summary>
    public override string? GrantedModusMentisId(Element? target) => "clambering";

    public override int DifficultyFor(Element? target)
        => target is ScalePointOfInterest scale ? scale.Difficulty : BaseDifficulty;

    public override IReadOnlyList<Wound?> FailurePenalties(Element? target)
        => target is ScalePointOfInterest scale ? scale.FailurePenalties() : NoPenalty;

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is ScalePointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>Climbs a <see cref="ScalePointOfInterest"/> from its foot to the place above.</summary>
public class ScaleUpVerb : ScaleVerbBase
{
    public override string VerbId      => "scale_up";
    public override string DisplayName => "Scale";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
        => target is ScalePointOfInterest scale && pov.Where.Id == scale.BottomArea.Id;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"scale {DefiniteTarget(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ScalePointOfInterest scale) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(scale.TopArea) };
    }
}

/// <summary>
/// Climbs back down a <see cref="ScalePointOfInterest"/>. Kept as its own verb rather than folded
/// into one symmetric "scale" because the two directions read differently and, on a well shaft,
/// are wanted in opposite orders.
/// </summary>
public class ScaleDownVerb : ScaleVerbBase
{
    public override string VerbId      => "scale_down";
    public override string DisplayName => "Climb Back Down";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
        => target is ScalePointOfInterest scale && pov.Where.Id == scale.TopArea.Id;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"climb back down {DefiniteTarget(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ScalePointOfInterest scale) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(scale.BottomArea) };
    }
}
