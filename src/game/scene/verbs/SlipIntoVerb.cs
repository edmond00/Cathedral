using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Forces a body through a gap that is not a door — a chimney, a broken window, a delivery hatch —
/// to get into somewhere that has not been opened to you.
///
/// <para>Illegal, like <c>unlock_door</c>: this is a way past a lock, and the witness and moral rules
/// should treat it as one. A chimney is one-way — you can drop down it and you cannot climb back up —
/// so <see cref="SlipIntoPointOfInterest.TwoWay"/> gates the return.</para>
/// </summary>
public class SlipIntoVerb : Verb
{
    public override string VerbId         => "slip_into";
    public override string DisplayName    => "Slip Into";
    public override int    BaseDifficulty => 5;

    /// <summary>
    /// Getting in where you are not admitted — but a gap is only a way past a lock if there is
    /// something private behind it. A breach in a barn wall leading to a public yard is a shortcut,
    /// not a burglary. Read from the gap's own two endpoints, so climbing back out of a private room
    /// is judged the same way as dropping into it.
    /// </summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor)
        => PrivacyModel.ReachesPrivateArea(scene, target);

    /// <summary>What a success teaches: getting through a way in that was not meant as one.</summary>
    public override string? GrantedModusMentisId(Element? target) => "sneak_art";

    public override int DifficultyFor(Element? target)
        => target is SlipIntoPointOfInterest slip ? slip.Difficulty : BaseDifficulty;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not SlipIntoPointOfInterest slip) return false;
        if (pov.Where.Id == slip.OutsideArea.Id) return true;

        // Coming back out is only possible through a gap that works both ways. You do not climb
        // back up a chimney.
        return slip.TwoWay && pov.Where.Id == slip.InsideArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is SlipIntoPointOfInterest slip && pov.Where.Id == slip.InsideArea.Id)
            return $"slip back out through {DefiniteTarget(target)}";
        return $"slip in through {DefiniteTarget(target)}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SlipIntoPointOfInterest slip) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(slip.Other(pov.Where)) };
    }

    public override IReadOnlyList<Wound?> FailurePenalties(Element? target)
        => target is SlipIntoPointOfInterest slip ? slip.FailurePenalties() : NoPenalty;

    // ── Routine recording ─────────────────────────────────────────────────────
    // Recordable: a way in the character has already found once is exactly the sort of thing a
    // routine is for, and unlike a forced door it leaves nothing behind that a rebuild resets.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is SlipIntoPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
