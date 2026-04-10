namespace Cathedral.Game.Npc;

/// <summary>
/// Defines a possible NPC encounter within a <see cref="Narrative.NarrationNode"/>.
/// Each slot specifies an archetype, spawn probability, and maximum instance count.
/// </summary>
public class NpcEncounterSlot
{
    /// <summary>The archetype to spawn from.</summary>
    public NamedNpcArchetype Archetype { get; }

    /// <summary>Probability (0.0–1.0) that this NPC spawns when the node is visited.</summary>
    public float SpawnChance { get; }

    /// <summary>Maximum number of NPCs from this slot per node visit.</summary>
    public int MaxCount { get; }

    public NpcEncounterSlot(NamedNpcArchetype archetype, float spawnChance = 0.3f, int maxCount = 1)
    {
        Archetype = archetype;
        SpawnChance = spawnChance;
        MaxCount = maxCount;
    }
}
