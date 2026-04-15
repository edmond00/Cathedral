using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// Selects an NPC witness for an illegal action and classifies how they can perceive it.
///
/// Visual witness (same area) selection priority:
///   1. Named NPCs who own the section the crime occurred in (highest authority).
///   2. Named NPCs who are present and can speak (general bystanders).
///
/// Audio witness (adjacent area) selection priority:
///   Same rules, applied across areas one step away in either direction of the AreaGraph.
///
/// Only alive, speaking-capable NPCs are considered.
/// </summary>
public static class WitnessSelector
{
    /// <summary>
    /// Computes the witness context for an action about to be committed at <paramref name="pov"/>.
    /// Returns <see cref="WitnessContext.None"/> when no suitable witness is reachable.
    /// </summary>
    public static WitnessContext ComputeContext(Scene scene, PoV pov)
    {
        // 1. Visual witness: same area
        var visual = SelectFrom(scene, pov.Where, pov.When);
        if (visual != null)
            return new WitnessContext(WitnessType.Visual, visual);

        // 2. Audio witness: adjacent areas (one hop in either direction)
        foreach (var adjacent in GetAdjacentAreas(scene, pov.Where))
        {
            var audio = SelectFrom(scene, adjacent, pov.When);
            if (audio != null)
                return new WitnessContext(WitnessType.Audio, audio);
        }

        return WitnessContext.None;
    }

    /// <summary>
    /// Legacy helper: picks the best visual witness at <paramref name="area"/> or null.
    /// Retained for call sites that only care about same-area witnesses.
    /// </summary>
    public static NpcEntity? Select(Scene scene, Area area, TimePeriod when)
        => SelectFrom(scene, area, when);

    // ── Internals ────────────────────────────────────────────────────────────

    private static NpcEntity? SelectFrom(Scene scene, Area area, TimePeriod when)
    {
        var candidates = scene.GetNpcsAt(area, when)
            .Where(n => n.IsAlive && n.Entity is NpcEntity npc && npc.CanSpeak)
            .Select(n => (NpcEntity)n.Entity)
            .ToList();

        if (candidates.Count == 0) return null;

        // Prefer the NPC who owns this section (highest authority over the space).
        var owner = candidates.FirstOrDefault(n =>
            n.OwnedSectionIds.Any(id =>
                scene.Sections.Any(s => s.Id.ToString() == id && s.Areas.Contains(area))));

        if (owner != null) return owner;

        // Fall back to highest authority level (guard > civilian).
        return candidates.OrderByDescending(n => n.AuthorityLevel).First();
    }

    /// <summary>
    /// Returns all areas one hop away from <paramref name="area"/> in either direction.
    /// </summary>
    private static System.Collections.Generic.IEnumerable<Area> GetAdjacentAreas(Scene scene, Area area)
    {
        // Outgoing edges: areas reachable from the current area
        foreach (var reachable in scene.GetReachableAreas(area))
            yield return reachable;

        // Incoming edges: areas from which the current area is reachable
        foreach (var (fromId, targets) in scene.AreaGraph)
        {
            if (!targets.Contains(area.Id)) continue;
            var fromArea = scene.Sections
                .SelectMany(s => s.Areas)
                .FirstOrDefault(a => a.Id == fromId);
            if (fromArea != null && fromArea.Id != area.Id)
                yield return fromArea;
        }
    }
}
