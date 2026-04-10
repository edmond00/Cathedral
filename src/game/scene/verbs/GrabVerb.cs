using System;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Verb for picking up an item from a point of interest.
/// Possible when the target is an <see cref="ItemElement"/> located in the current area's points of interest.
/// </summary>
public class GrabVerb : Verb
{
    public override string VerbId => "grab";
    public override string DisplayName => "Grab";

    public override bool IsPossible(Scene scene, PoV pov, Element target)
    {
        if (target is not ItemElement) return false;

        // Check that the item is in one of the current area's points of interest
        return pov.Where.PointsOfInterest.Any(poi => poi.Items.Any(ie => ie.Id == target.Id));
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var name = target.DisplayName.ToLowerInvariant();
        var article = "aeiou".Contains(name[0]) ? "an" : "a";
        return $"grab {article} {name}";
    }

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not ItemElement itemElement)
            throw new InvalidOperationException("GrabVerb target must be an ItemElement");

        // Remove from point of interest
        foreach (var poi in pov.Where.PointsOfInterest)
        {
            if (poi.Items.Remove(itemElement))
            {
                Console.WriteLine($"GrabVerb: Removed {itemElement.DisplayName} from {poi.DisplayName}");
                break;
            }
        }

        // Add to protagonist inventory
        actor.Inventory.Add(itemElement.Item);

        // Record state change
        scene.StateChanges.Capture(itemElement);

        Console.WriteLine($"GrabVerb: {actor.DisplayName} acquired {itemElement.DisplayName}");
    }
}
