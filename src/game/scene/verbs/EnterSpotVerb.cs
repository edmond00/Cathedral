using System;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Moves the PoV into a <see cref="Spot"/> within the current area.
/// Only possible when the player is not already inside a spot.
/// </summary>
public class EnterSpotVerb : Verb
{
    public override string VerbId         => "enter_spot";
    public override string DisplayName    => "Examine";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not Spot spot) return false;
        if (pov.InSpot != null) return false;  // already inside a spot
        return pov.Where.Spots.Contains(spot);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"examine the {target.DisplayName.ToLowerInvariant()}";

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not Spot spot)
            throw new InvalidOperationException("EnterSpotVerb target must be a Spot");

        pov.InSpot = spot;
        pov.Focus  = null;
        Console.WriteLine($"EnterSpotVerb: Entered spot '{spot.DisplayName}'");
    }
}
