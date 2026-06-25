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
    public static Element? Resolve(Scene.Scene scene, RoutineTargetRef target)
    {
        switch (target.Kind)
        {
            case RoutineTargetKind.Area:
                return scene.AllAreas.FirstOrDefault(a => KeyMatches(a.ReferenceLemma, target.Key));

            case RoutineTargetKind.PointOfInterest:
                foreach (var area in scene.AllAreas)
                {
                    var direct = area.PointsOfInterest.FirstOrDefault(p => KeyMatches(p.ReferenceLemma, target.Key));
                    if (direct != null) return direct;
                    foreach (var spot in area.Spots)
                    {
                        var inSpot = spot.PointsOfInterest.FirstOrDefault(p => KeyMatches(p.ReferenceLemma, target.Key));
                        if (inSpot != null) return inSpot;
                    }
                }
                return null;

            case RoutineTargetKind.Spot:
                return scene.AllAreas
                    .SelectMany(a => a.Spots)
                    .FirstOrDefault(s => KeyMatches(s.ReferenceLemma, target.Key));

            case RoutineTargetKind.Npc:
                return scene.Npcs
                    .FirstOrDefault(n => n.IsAlive && KeyMatches(n.DisplayName, target.Key));

            default:
                return null;
        }
    }

    private static bool KeyMatches(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
