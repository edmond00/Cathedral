using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Steals an item from a <see cref="Area.IsPrivate"/> area or spot.
/// Functionally identical to <see cref="GrabVerb"/> but marks the action as illegal,
/// which triggers witness detection and the "caught red-handed" dialogue on failure.
/// </summary>
public class StealVerb : Verb
{
    public override string VerbId         => "steal";
    public override string DisplayName    => "Steal";
    public override int    BaseDifficulty => 3;

    /// <summary>Stealing is always an illegal action.</summary>
    public override bool IsLegal => false;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not ItemElement itemEl) return false;

        if (pov.InSpot != null)
        {
            // Inside a spot of a private area only.
            if (!pov.Where.IsPrivate) return false;
            return pov.InSpot.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
        else
        {
            // In a private area directly.
            if (!pov.Where.IsPrivate) return false;
            return pov.Where.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var name    = target.DisplayName.ToLowerInvariant();
        var article = "aeiou".Contains(name[0]) ? "an" : "a";
        return $"steal {article} {name}";
    }

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not ItemElement itemElement)
            throw new InvalidOperationException("StealVerb target must be an ItemElement");

        var searchPoIs = pov.InSpot != null
            ? pov.InSpot.PointsOfInterest.Where(p => p is not CorpseBodyPartPoI)
            : pov.Where.PointsOfInterest.Where(p => p is not CorpseBodyPartPoI);

        foreach (var poi in searchPoIs)
        {
            if (poi.Items.Remove(itemElement))
            {
                Console.WriteLine($"StealVerb: Removed {itemElement.DisplayName} from {poi.DisplayName}");
                break;
            }
        }

        actor.Inventory.Add(itemElement.Item);
        scene.StateChanges.Capture(itemElement);
        Console.WriteLine($"StealVerb: {actor.DisplayName} stole {itemElement.DisplayName}");
    }
}
