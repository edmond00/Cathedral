using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// Shared contract for both <see cref="NpcEntity"/> (named, anatomy-bearing) and
/// <see cref="ShallowNpcEntity"/> (anonymous, lootable-only) instances.
/// </summary>
public interface INpcEntity
{
    /// <summary>Stable identifier for persistence or disambiguation.</summary>
    string NpcId { get; }

    /// <summary>Display name shown in narration and UI.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this NPC is still alive. Setting to false kills the NPC.
    /// Dead NPCs are hidden from scene observations and cannot be interacted with.
    /// </summary>
    bool IsAlive { get; set; }

    /// <summary>Whether this NPC is hostile to the player by default.</summary>
    bool IsHostile { get; }

    /// <summary>Short LLM observation hint (e.g. "a grey wolf watches from the shadows").</summary>
    string ObservationHint { get; }

    /// <summary>The archetype that spawned this entity.</summary>
    NpcArchetype Archetype { get; }

    /// <summary>Human-readable species name for display purposes (e.g. "Human", "Wolf", "Chicken").</summary>
    string SpeciesName { get; }

    /// <summary>
    /// Generates a temporary corpse <see cref="CorpseSpot"/> to be placed in the area where
    /// this NPC died. The spot is added at runtime and not persisted between scenes.
    /// </summary>
    CorpseSpot GenerateCorpse(Area area);
}
