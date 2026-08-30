using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Climbs a <see cref="CliffPointOfInterest"/> from the bottom area to the top area.
/// Only possible when the player is in <see cref="CliffPointOfInterest.BottomArea"/>.
/// Difficulty is 6 (8 if icy).
/// </summary>
public class ClimbUpVerb : Verb
{
    /// <summary>A height reached in order to look from it.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Target is CliffPointOfInterest { TopArea: CragArea or SummitArea or RidgeArea or CrownArea }) yield return Mm<VantageModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId      => "climb_up";
    public override string DisplayName => "Climb Up";
    public override int    BaseDifficulty => 6;

    /// <summary>Claws or arms — see <see cref="Verb.GrantedModusMentisIds"/> for why the beast lesson comes first.</summary>
    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => new[] { "clambering", "scaling" };

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not CliffPointOfInterest cliff) return false;
        return pov.Where.Id == cliff.BottomArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"climb up {DefiniteTarget(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not CliffPointOfInterest cliff) return System.Array.Empty<Outcome>();
        return new[] { new AreaMoveOutcome(cliff.TopArea) };
    }

    // A failed climb is a fall: usually a scare, sometimes a fracture. Extra nulls keep injury the minority.
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null,
        new AnkleFractureLeftWound(),
        new KneeFractureRightWound(),
        new TibiaFractureLeftWound(),
        new BrokenArmRightWound(),
    };

    // ── Routine recording ─────────────────────────────────────────────────────
    // Replay skips the climb skill check — a "practised" route is the point of a routine.
    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is CliffPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
