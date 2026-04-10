using System;
using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// A named NPC entity — wraps an <see cref="EnemyCombatant"/> (full anatomy, modiMentis, wounds)
/// and optionally a <see cref="NpcPersona"/> + conversation graph for dialogue-capable NPCs.
/// Implements <see cref="INpcEntity"/> so it can be used anywhere the shared NPC contract is required.
/// </summary>
public class NpcEntity : INpcEntity
{
    /// <inheritdoc/>
    public string NpcId { get; }

    /// <inheritdoc/>
    public string DisplayName => Combatant.DisplayName;

    /// <summary>The underlying party member used for anatomy, wounds, stats, and combat.</summary>
    public EnemyCombatant Combatant { get; }

    /// <summary>The archetype that spawned this NPC (wolf, druid, etc.).</summary>
    public new NamedNpcArchetype Archetype { get; }

    NpcArchetype INpcEntity.Archetype => Archetype;

    /// <inheritdoc/>
    public bool IsHostile { get; set; }

    /// <summary>Whether this NPC can be talked to (has persona + conversation graph).</summary>
    public bool CanDialogue => Persona != null && ConversationRoot != null;

    // ── IsAlive ──────────────────────────────────────────────────────────────

    private bool _isSlain = false;

    /// <inheritdoc/>
    /// True while the combatant has HP remaining and has not been explicitly slain.
    /// Setting to false slays the NPC immediately (without combat).
    public bool IsAlive
    {
        get => !_isSlain && Combatant.CurrentHp > 0;
        set { if (!value) _isSlain = true; else _isSlain = false; }
    }

    // ── Dialogue ─────────────────────────────────────────────────────────────

    /// <summary>LLM persona defining speech patterns and knowledge. Null for non-speaking NPCs.</summary>
    public NpcPersona? Persona { get; }

    /// <summary>Entry node of the conversation graph. Null for non-speaking NPCs.</summary>
    public ConversationSubjectNode? ConversationRoot { get; }

    /// <summary>Per-instance affinity score (0–100). Only meaningful for dialogue-capable NPCs.</summary>
    public float Affinity { get; set; }

    /// <inheritdoc/>
    public bool IsPersistent { get; }

    /// <inheritdoc/>
    public KeywordInContext[] NarrationKeywordsInContext { get; }

    /// <inheritdoc/>
    public string ObservationHint { get; }

    /// <inheritdoc/>
    public string SpeciesName => Archetype.Species.DisplayName;

    // ── Constructor ──────────────────────────────────────────────────────────

    public NpcEntity(
        string npcId,
        EnemyCombatant combatant,
        NamedNpcArchetype archetype,
        bool isHostile,
        bool isPersistent,
        KeywordInContext[] narrationKeywordsInContext,
        string observationHint,
        NpcPersona? persona = null,
        ConversationSubjectNode? conversationRoot = null,
        float initialAffinity = 50f)
    {
        NpcId                      = npcId;
        Combatant                  = combatant;
        Archetype                  = archetype;
        IsHostile                  = isHostile;
        IsPersistent               = isPersistent;
        NarrationKeywordsInContext = narrationKeywordsInContext;
        ObservationHint            = observationHint;
        Persona                    = persona;
        ConversationRoot           = conversationRoot;
        Affinity                   = initialAffinity;
    }

    // ── Corpse generation ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public CorpseSpot GenerateCorpse(Area area)
        => CorpseRegistry.CreateForNamedNpc(this, area);

    // ── Dialogue conversion ──────────────────────────────────────────────────

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
