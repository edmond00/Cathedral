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
    /// Returns the PoI (in the current spot, or the current area) that holds the given item, or null if
    /// none. Corpse PoIs are excluded unless <paramref name="includeCorpse"/> is set (Cut harvesting).
    /// Mirrors the search used by the pickup verbs' <c>IsPossible</c>.
    /// </summary>
    public static PointOfInterest? FindHoldingPoI(PoV pov, ItemElement item, bool includeCorpse = false)
    {
        var pois = pov.InSpot != null
            ? pov.InSpot.PointsOfInterest
            : pov.Where.PointsOfInterest;

        return pois
            .Where(poi => includeCorpse || poi is not CorpseBodyPartPoI)
            .FirstOrDefault(poi => poi.Items.Any(ie => ie.Id == item.Id));
    }

    /// <summary>
    /// Picks <paramref name="item"/>: places it in the acting member's inventory, removes it from its
    /// holding PoI, and records the depletion timestamp (the protagonist's current game time). If the
    /// inventory is full the item is left in the world untouched (pickup is normally pre-gated by the
    /// coded inventory-capacity rule). During routine virtual replay (<see cref="Scene.IsVirtualReplay"/>)
    /// nothing real is mutated — the item is only removed from the disposable scene so multi-pick
    /// routines can validate.
    /// </summary>
    public static void Pick(Scene scene, PoV pov, PartyMember actor, ItemElement item, bool includeCorpse = false)
    {
        var poi = FindHoldingPoI(pov, item, includeCorpse);

        // Validation-only during virtual replay: advance the disposable scene, touch nothing real.
        if (scene.IsVirtualReplay)
        {
            poi?.Items.Remove(item);
            return;
        }

        // Acquire first; if there is no room, drop the item (leave it in the world) rather than lose it.
        if (!actor.AcquireItem(item.Item)) return;

        poi?.Items.Remove(item);

        // Stamp depletion against the global clock so this slot stays empty until it regenerates.
        if (!string.IsNullOrEmpty(item.DepletionKey))
        {
            scene.ItemDepletions[item.DepletionKey] = GameClock.Days;
        }
    }
}
