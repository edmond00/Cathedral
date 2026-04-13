using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// Selects an NPC witness for an illegal action that was committed in the open.
///
/// Selection priority:
///   1. Named NPCs (<see cref="NpcEntity"/>) who own the section the crime occurred in
///      (highest authority over the space).
///   2. Named NPCs who are present and can speak (general bystanders).
///   3. No witness — the action went unnoticed.
///
/// Only alive, speaking-capable NPCs are considered.
/// </summary>
public static class WitnessSelector
{
    /// <summary>
    /// Tries to select a witness for a crime committed by the party member at <paramref name="area"/>
    /// during <paramref name="when"/>.
    /// Returns null when no suitable witness is present.
    /// </summary>
    public static NpcEntity? Select(Scene scene, Area area, TimePeriod when)
    {
        var candidates = scene.GetNpcsAt(area, when)
            .Where(n => n.IsAlive && n.Entity is NpcEntity npc && npc.CanSpeak)
            .Select(n => (NpcEntity)n.Entity)
            .ToList();

        if (candidates.Count == 0) return null;

        // Prefer the NPC who owns this section (highest authority).
        var owner = candidates.FirstOrDefault(n =>
            n.OwnedSectionIds.Any(id =>
                scene.Sections.Any(s => s.Id.ToString() == id && s.Areas.Contains(area))));

        if (owner != null) return owner;

        // Fall back to highest authority level (guard > civilian).
        return candidates
            .OrderByDescending(n => n.AuthorityLevel)
            .First();
    }
}
