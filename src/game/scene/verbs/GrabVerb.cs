using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Picks up an item from a <see cref="PointOfInterest"/> in the current area or current spot.
/// Does not apply to <see cref="CorpseBodyPartPoI"/> items — use <see cref="CutVerb"/> for those.
/// </summary>
public class GrabVerb : Verb
{
    public override string VerbId      => "grab";
    public override string DisplayName => "Grab";

    public override bool IsPossible(Scene scene, PoV pov, Element target)
    {
        if (target is not ItemElement itemEl) return false;

        if (pov.InSpot != null)
        {
            // Inside a spot: item must be in a non-corpse PoI of the current spot
            return pov.InSpot.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
        else
        {
            // In an area: item must be in a non-corpse PoI of the current area
            return pov.Where.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var name    = target.DisplayName.ToLowerInvariant();
        var article = "aeiou".Contains(name[0]) ? "an" : "a";
        return $"grab {article} {name}";
    }

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not ItemElement itemElement)
            throw new InvalidOperationException("GrabVerb target must be an ItemElement");

        // Remove from PoI (area or spot)
        var searchPoIs = pov.InSpot != null
            ? pov.InSpot.PointsOfInterest.Where(p => p is not CorpseBodyPartPoI)
            : pov.Where.PointsOfInterest.Where(p => p is not CorpseBodyPartPoI);

        foreach (var poi in searchPoIs)
        {
            if (poi.Items.Remove(itemElement))
            {
                Console.WriteLine($"GrabVerb: Removed {itemElement.DisplayName} from {poi.DisplayName}");
                break;
            }
        }

        actor.Inventory.Add(itemElement.Item);
        scene.StateChanges.Capture(itemElement);
        Console.WriteLine($"GrabVerb: {actor.DisplayName} acquired {itemElement.DisplayName}");
    }
}
