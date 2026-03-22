using System;
using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Unified NPC entity bridging the narration, fight, and dialogue systems.
/// Wraps an <see cref="EnemyCombatant"/> (full anatomy, modiMentis, wounds) and
/// optionally a <see cref="NpcPersona"/> + conversation graph for dialogue-capable NPCs.
/// </summary>
public class NpcEntity
{
    /// <summary>Stable identifier used to persist named NPCs across visits.</summary>
    public string NpcId { get; }

    /// <summary>Display name shown in narration, combat, and dialogue (e.g., "Grey Wolf", "Hermit Aldous").</summary>
    public string DisplayName => Combatant.DisplayName;

    /// <summary>The underlying party member used for anatomy, wounds, stats, and combat.</summary>
    public EnemyCombatant Combatant { get; }

    /// <summary>The archetype that spawned this NPC (wolf, druid, etc.).</summary>
    public NpcArchetype Archetype { get; }

    /// <summary>Whether this NPC is hostile by default (beasts, some humans).</summary>
    public bool IsHostile { get; set; }

    /// <summary>Whether this NPC can be talked to (has persona + conversation graph).</summary>
    public bool CanDialogue => Persona != null && ConversationRoot != null;

    /// <summary>Whether this NPC is still alive (delegates to combatant HP).</summary>
    public bool IsAlive => Combatant.CurrentHp > 0;

    // ── Dialogue fields (null for beasts) ──

    /// <summary>LLM persona defining speech patterns and knowledge. Null for non-speaking NPCs.</summary>
    public NpcPersona? Persona { get; }

    /// <summary>Entry node of the conversation graph. Null for non-speaking NPCs.</summary>
    public ConversationSubjectNode? ConversationRoot { get; }

    /// <summary>
    /// Per-instance affinity score (0–100). Only meaningful for dialogue-capable NPCs.
    /// Modified by dialogue outcomes.
    /// </summary>
    public float Affinity { get; set; }

    /// <summary>Whether this is a named/persistent NPC that survives across visits.</summary>
    public bool IsPersistent { get; }

    /// <summary>
    /// Keywords injected into the narration node when this NPC is present.
    /// E.g., "wolf", "grey", "prowling" for a wolf — or "hermit", "cloaked", "old" for a human.
    /// </summary>
    public string[] NarrationKeywords { get; }

    /// <summary>
    /// Short description used in LLM observation prompts to hint at the NPC's presence.
    /// E.g., "a grey wolf watches from the treeline" or "an old hermit sits by a smouldering fire".
    /// </summary>
    public string ObservationHint { get; }

    public NpcEntity(
        string npcId,
        EnemyCombatant combatant,
        NpcArchetype archetype,
        bool isHostile,
        bool isPersistent,
        string[] narrationKeywords,
        string observationHint,
        NpcPersona? persona = null,
        ConversationSubjectNode? conversationRoot = null,
        float initialAffinity = 50f)
    {
        NpcId = npcId;
        Combatant = combatant;
        Archetype = archetype;
        IsHostile = isHostile;
        IsPersistent = isPersistent;
        NarrationKeywords = narrationKeywords;
        ObservationHint = observationHint;
        Persona = persona;
        ConversationRoot = conversationRoot;
        Affinity = initialAffinity;
    }

    /// <summary>
    /// Creates a <see cref="NpcInstance"/> for the dialogue system from this entity.
    /// Only valid when <see cref="CanDialogue"/> is true.
    /// </summary>
    public NpcInstance ToDialogueNpc()
    {
        if (Persona == null || ConversationRoot == null)
            throw new InvalidOperationException($"NPC '{DisplayName}' has no dialogue capability.");

        return new NpcInstance(NpcId, Persona, ConversationRoot, Affinity);
    }
}
