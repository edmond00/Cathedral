using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Leaves the current <see cref="Spot"/> and returns to observing the parent area.
/// Only possible when the player is inside a spot.
/// </summary>
public class LeaveSpotVerb : Verb
{
    public override string VerbId         => "leave";
    public override string DisplayName    => "Leave";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (pov.InSpot == null) return false;
        return target is Spot spot && spot.Id == pov.InSpot.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"leave the {target.DisplayName.ToLowerInvariant()}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
        => new[] { new SpotLeaveOutcome() };
}
