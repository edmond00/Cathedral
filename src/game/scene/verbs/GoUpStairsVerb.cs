using System;
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

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not StairPointOfInterest stair)
            throw new InvalidOperationException("GoUpStairsVerb target must be a StairPointOfInterest");

        pov.Where = stair.TopArea;
        pov.Focus = null;
        Console.WriteLine($"GoUpStairsVerb: Climbed to {stair.TopArea.DisplayName}");
    }
}
