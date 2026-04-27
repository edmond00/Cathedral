using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Descends a <see cref="StairPointOfInterest"/> from the top area to the bottom area.
/// Only possible when the player is in the <see cref="StairPointOfInterest.TopArea"/>.
/// </summary>
public class GoDownStairsVerb : Verb
{
    public override string VerbId         => "go_down_stairs";
    public override string DisplayName    => "Go Down";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not StairPointOfInterest stair) return false;
        return pov.Where.Id == stair.TopArea.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"descend {target.DisplayName.ToLowerInvariant()}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not StairPointOfInterest stair) return System.Array.Empty<OutcomeReport>();
        return new[] { new AreaMoveOutcome(stair.BottomArea) };
    }
}
