using System;
using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Abstract archetype for <b>named</b> NPCs — characters with their own anatomy, derived stats,
/// and optional dialogue capability. Can fight, join the party, and engage in conversation.
/// Spawns <see cref="NpcEntity"/> instances.
/// </summary>
public abstract class NamedNpcArchetype : NpcArchetype
{
    /// <summary>Species used for anatomy and combat.</summary>
    public abstract Species Species { get; }

    /// <summary>Whether spawned NPCs are hostile by default.</summary>
    public abstract bool DefaultHostile { get; }

    /// <summary>Whether spawned NPCs persist across visits (named characters).</summary>
    public abstract bool DefaultPersistent { get; }

    /// <summary>Pool of possible display names. One is picked randomly on spawn.</summary>
    public abstract string[] NamePool { get; }

    /// <summary>How many modiMentis to assign at creation.</summary>
    public virtual int ModiMentisCount => 8;

    // ── Spawn ────────────────────────────────────────────────────────────────

    /// <summary>Spawns a new <see cref="NpcEntity"/> from this archetype.</summary>
    public NpcEntity Spawn(Random rng, string nodeContext = "")
    {
        var name = NamePool[rng.Next(NamePool.Length)];
        var npcId = DefaultPersistent
            ? $"{ArchetypeId}_{name.ToLowerInvariant().Replace(' ', '_')}"
            : $"{ArchetypeId}_{rng.Next(100000)}";

        var combatant = new EnemyCombatant(name, Species);
        combatant.InitializeModiMentis(ModusMentisRegistry.Instance, ModiMentisCount);

        var keywords = BuildNarrationKeywordsInContext(name);
        var hint     = BuildObservationHint(name, nodeContext);
        var persona  = CreatePersona();
        var graph    = CreateConversationGraph();

        return new NpcEntity(
            npcId, combatant, this,
            DefaultHostile, DefaultPersistent,
            keywords, hint, persona, graph);
    }

    // ── Overridable builders ────────────────────────────────────────────────

    /// <summary>Override to provide narration keywords for this NPC type.</summary>
    protected abstract KeywordInContext[] BuildNarrationKeywordsInContext(string name);

    /// <summary>Override to provide the observation hint sentence.</summary>
    protected abstract string BuildObservationHint(string name, string nodeContext);

    /// <summary>Override to provide a dialogue persona. Return null for non-speaking NPCs.</summary>
    protected virtual NpcPersona? CreatePersona() => null;

    /// <summary>Override to provide a conversation graph. Return null for non-speaking NPCs.</summary>
    protected virtual ConversationSubjectNode? CreateConversationGraph() => null;
}
