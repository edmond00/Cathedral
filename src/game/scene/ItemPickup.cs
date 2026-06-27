using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene;

/// <summary>
/// Shared item-pickup logic reused by all pickup verbs (Grab, Gather, Steal). Centralises locating
/// the point of interest that holds an item and performing the pick (remove from world, add to the
/// actor's inventory, and stamp the depletion timestamp). Corpse harvesting (Cut) is intentionally
/// excluded — corpses are transient and not subject to regeneration.
/// </summary>
public static class ItemPickup
{
    /// <summary>
    /// Returns the non-corpse PoI (in the current spot, or the current area) that holds the given item,
    /// or null if none. Mirrors the search used by the pickup verbs' <c>IsPossible</c>.
    /// </summary>
    public static PointOfInterest? FindHoldingPoI(PoV pov, ItemElement item)
    {
        var pois = pov.InSpot != null
            ? pov.InSpot.PointsOfInterest
            : pov.Where.PointsOfInterest;

        return pois
            .Where(poi => poi is not CorpseBodyPartPoI)
            .FirstOrDefault(poi => poi.Items.Any(ie => ie.Id == item.Id));
    }

    /// <summary>
    /// Picks <paramref name="item"/>: removes it from its holding PoI, adds it to the acting member's
    /// inventory, and records the depletion timestamp (the protagonist's current game time). During
    /// routine virtual replay (<see cref="Scene.IsVirtualReplay"/>) nothing is mutated — the call only
    /// has to be reachable for the replay validation to succeed.
    /// </summary>
    public static void Pick(Scene scene, PoV pov, PartyMember actor, ItemElement item)
    {
        // Remove from the holding PoI. Safe even on a disposable virtual-replay scene, and lets a
        // multi-pick routine validate that enough instances exist.
        var poi = FindHoldingPoI(pov, item);
        poi?.Items.Remove(item);

        // Validation-only during routine virtual replay: don't touch real inventory or depletion.
        if (scene.IsVirtualReplay) return;

        actor.AcquireItem(item.Item);

        // Stamp depletion against the global clock so this slot stays empty until it regenerates.
        if (!string.IsNullOrEmpty(item.DepletionKey))
        {
            double now = (actor as Protagonist)?.GameTimeHours ?? 0.0;
            scene.ItemDepletions[item.DepletionKey] = now;
        }
    }
}
