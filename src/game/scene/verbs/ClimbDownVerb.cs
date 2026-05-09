using System.Collections.Generic;
using Cathedral.Game.Narrative;
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

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not CliffPointOfInterest cliff) return false;
        return pov.Where.Id == cliff.TopArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"climb down {target.DisplayName.ToLowerInvariant()}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not CliffPointOfInterest cliff) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(cliff.BottomArea) };
    }
}
