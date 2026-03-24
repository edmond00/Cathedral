using System;
using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Defines an NPC "template" — species, hostility, name pool, keywords, observation hints,
/// and optional dialogue persona/graph factories.
/// Concrete subclasses (WolfArchetype, DruidArchetype, …) know how to spawn <see cref="NpcEntity"/> instances.
/// </summary>
public abstract class NpcArchetype
{
    /// <summary>Archetype identifier (e.g., "wolf", "druid").</summary>
    public abstract string ArchetypeId { get; }

    /// <summary>Species used for anatomy/combat (e.g., SpeciesRegistry.Wolf).</summary>
    public abstract Species Species { get; }

    /// <summary>Whether spawned NPCs are hostile by default.</summary>
    public abstract bool DefaultHostile { get; }

    /// <summary>Whether spawned NPCs should persist across visits (named characters).</summary>
    public abstract bool DefaultPersistent { get; }

    /// <summary>Pool of possible display names. One is picked randomly on spawn.</summary>
    public abstract string[] NamePool { get; }

    /// <summary>How many modiMentis to assign at creation.</summary>
    public virtual int ModiMentisCount => 8;

    /// <summary>
    /// Spawns a new <see cref="NpcEntity"/> from this archetype.
    /// </summary>
    /// <param name="rng">Seeded RNG for deterministic generation.</param>
    /// <param name="nodeContext">Description of the narration node where the NPC spawns (for observation hints).</param>
    public NpcEntity Spawn(Random rng, string nodeContext = "")
    {
        var name = NamePool[rng.Next(NamePool.Length)];
        var npcId = DefaultPersistent
            ? $"{ArchetypeId}_{name.ToLowerInvariant().Replace(' ', '_')}"
            : $"{ArchetypeId}_{rng.Next(100000)}";

        var combatant = new EnemyCombatant(name, Species);
        combatant.InitializeModiMentis(ModusMentisRegistry.Instance, ModiMentisCount);

        var keywords = BuildNarrationKeywords(name);
        var hint = BuildObservationHint(name, nodeContext);
        var persona = CreatePersona();
        var graph = CreateConversationGraph();

        return new NpcEntity(
            npcId,
            combatant,
            this,
            DefaultHostile,
            DefaultPersistent,
            keywords,
            hint,
            persona,
            graph);
    }

    /// <summary>Override to provide narration keywords for this NPC type.</summary>
    protected abstract string[] BuildNarrationKeywords(string name);

    /// <summary>Override to provide the observation hint sentence.</summary>
    protected abstract string BuildObservationHint(string name, string nodeContext);

    /// <summary>Override to provide a dialogue persona. Return null for non-speaking NPCs (beasts).</summary>
    protected virtual NpcPersona? CreatePersona() => null;

    /// <summary>Override to provide a conversation graph root. Return null for non-speaking NPCs.</summary>
    protected virtual ConversationSubjectNode? CreateConversationGraph() => null;
}
