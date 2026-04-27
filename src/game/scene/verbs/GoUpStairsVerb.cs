using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Climbs a <see cref="StairPointOfInterest"/> from the bottom area to the top area.
/// Only possible when the player is in the <see cref="StairPointOfInterest.BottomArea"/>.
/// </summary>
public class GoUpStairsVerb : Verb
{
    public override string VerbId         => "go_up_stairs";
    public override string DisplayName    => "Go Up";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not StairPointOfInterest stair) return false;
        return pov.Where.Id == stair.BottomArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"climb up {target.DisplayName.ToLowerInvariant()}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not StairPointOfInterest stair) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(stair.TopArea) };
    }
}
