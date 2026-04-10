using System;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// Abstract archetype for <b>shallow</b> NPCs — anonymous creatures with no personal anatomy.
/// They cannot fight, join the party, or hold dialogue, but they can be slayed to yield a
/// lootable corpse, and they follow a time-period schedule across scene areas.
/// Spawns <see cref="ShallowNpcEntity"/> instances.
/// </summary>
public abstract class ShallowNpcArchetype : NpcArchetype
{
    /// <summary>Display name used for all instances of this type (e.g. "Chicken", "Rabbit").</summary>
    public abstract string TypeDisplayName { get; }

    /// <summary>Whether instances are hostile by default. Almost always false for farm animals.</summary>
    public virtual bool DefaultHostile => false;

    // ── Spawn ────────────────────────────────────────────────────────────────

    /// <summary>Spawns a new <see cref="ShallowNpcEntity"/> from this archetype.</summary>
    public ShallowNpcEntity Spawn(Random rng, string nodeContext = "")
    {
        var npcId    = $"{ArchetypeId}_{rng.Next(100000)}";
        var keywords = BuildNarrationKeywords();
        var hint     = BuildObservationHint(nodeContext);
        return new ShallowNpcEntity(npcId, TypeDisplayName, this, DefaultHostile, keywords, hint);
    }

    // ── Overridable builders ────────────────────────────────────────────────

    /// <summary>Override to provide narration keywords for instances of this type.</summary>
    protected abstract KeywordInContext[] BuildNarrationKeywords();

    /// <summary>Override to provide the observation hint for the LLM.</summary>
    protected abstract string BuildObservationHint(string nodeContext);

    /// <summary>Override to build a <see cref="CorpseSpot"/> when an instance is slain.</summary>
    public abstract CorpseSpot CreateCorpse(ShallowNpcEntity entity, Area area);
}
