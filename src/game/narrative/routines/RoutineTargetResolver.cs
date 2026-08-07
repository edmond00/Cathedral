using System;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Re-resolves a <see cref="RoutineTargetRef"/> to the live <see cref="Element"/> in a freshly
/// built scene. Returns null when the target no longer exists (e.g. an NPC died, or location
/// layout changed), which marks the owning routine as unreplayable.
/// </summary>
public static class RoutineTargetResolver
{
    /// <summary>
    /// Resolves a routine target against the live scene. <paramref name="pov"/> is the working point of
    /// view for the current step: PoI/Item targets prefer a match in the current area (so the verb's
    /// <c>IsPossible</c>, which is scoped to the PoV, accepts the resolved instance), falling back to a
    /// scene-wide search.
    /// </summary>
    public static Element? Resolve(Scene.Scene scene, PoV pov, RoutineTargetRef target)
    {
        switch (target.Kind)
        {
            case RoutineTargetKind.Area:
                return scene.AllAreas.FirstOrDefault(a => KeyMatches(a.ReferenceLemma, target.Key));

            case RoutineTargetKind.PointOfInterest:
            {
                // Current area first, then scene-wide.
                var local = pov.Where.PointsOfInterest.FirstOrDefault(p => KeyMatches(p.ReferenceLemma, target.Key));
                if (local != null) return local;
                return scene.AllAreas
                    .SelectMany(a => a.PointsOfInterest)
                    .FirstOrDefault(p => KeyMatches(p.ReferenceLemma, target.Key));
            }

            case RoutineTargetKind.Npc:
                return scene.Npcs
                    .FirstOrDefault(n => n.IsAlive && KeyMatches(n.DisplayName, target.Key));

            case RoutineTargetKind.Item:
                // Scoped to the current area — must match the pickup verb's IsPossible scope.
                // Depletion has already removed still-depleted items, so a depleted resource resolves
                // to null → the routine greys out until it regrows.
                return FindItem(pov.Where.PointsOfInterest, target.Key);

            default:
                return null;
        }
    }

    private static ItemElement? FindItem(System.Collections.Generic.IEnumerable<PointOfInterest> pois, string itemId)
    {
        foreach (var poi in pois)
        {
            var item = poi.Items.FirstOrDefault(ie => KeyMatches(ie.Item.ItemId, itemId));
            if (item != null) return item;
        }
        return null;
    }

    private static bool KeyMatches(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
