using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// Selects the nearest enemy NPC and classifies the level of threat it poses.
///
/// Visual threat (same area): an alive enemy is right there — any action risks an opportunity attack.
/// Audio threat (adjacent area): an alive enemy one hop away — a failed noisy action may draw them.
///
/// Selection priority (same as WitnessSelector): section owner > highest AuthorityLevel.
/// </summary>
public static class ThreatSelector
{
    /// <summary>
    /// Computes the threat context for an action about to be taken at <paramref name="pov"/>.
    /// Returns <see cref="ThreatContext.None"/> when no enemy is nearby.
    /// </summary>
    public static ThreatContext ComputeContext(Scene scene, PoV pov, Protagonist protagonist)
    {
        // 1. Visual threat: same area
        var visual = SelectFrom(scene, pov.Where, pov.When, protagonist.DisplayName);
        if (visual != null)
            return new ThreatContext(ThreatLevel.Visual, visual);

        // 2. Audio threat: adjacent areas (one hop in either direction)
        foreach (var adjacent in GetAdjacentAreas(scene, pov.Where))
        {
            var audio = SelectFrom(scene, adjacent, pov.When, protagonist.DisplayName);
            if (audio != null)
                return new ThreatContext(ThreatLevel.Audio, audio);
        }

        return ThreatContext.None;
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private static NpcEntity? SelectFrom(Scene scene, Area area, TimePeriod when, string protagonistId)
    {
        var candidates = scene.GetNpcsAt(area, when)
            .Where(n => n.IsAlive && n.Entity is NpcEntity npc &&
                        npc.AffinityTable.IsEnemy(protagonistId))
            .Select(n => (NpcEntity)n.Entity)
            .ToList();

        if (candidates.Count == 0) return null;

        // Prefer the NPC who owns this section (section owner is most authoritative threat).
        var owner = candidates.FirstOrDefault(n =>
            n.OwnedSectionIds.Any(id =>
                scene.Sections.Any(s => s.Id.ToString() == id && s.Areas.Contains(area))));

        if (owner != null) return owner;

        return candidates.OrderByDescending(n => n.AuthorityLevel).First();
    }

    private static System.Collections.Generic.IEnumerable<Area> GetAdjacentAreas(Scene scene, Area area)
    {
        foreach (var reachable in scene.GetReachableAreas(area))
            yield return reachable;

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
