using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Climbs a <see cref="CliffPointOfInterest"/> from the bottom area to the top area.
/// Only possible when the player is in <see cref="CliffPointOfInterest.BottomArea"/>.
/// Difficulty is 6 (8 if icy).
/// </summary>
public class ClimbUpVerb : Verb
{
    public override string VerbId      => "climb_up";
    public override string DisplayName => "Climb Up";
    public override int    BaseDifficulty => 6;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not CliffPointOfInterest cliff) return false;
        return pov.Where.Id == cliff.BottomArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"climb up {target.DisplayName.ToLowerInvariant()}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not CliffPointOfInterest cliff) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(cliff.TopArea) };
    }
}
