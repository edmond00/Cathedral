using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// A named NPC entity — wraps an <see cref="EnemyCombatant"/> (full anatomy, modiMentis, wounds)
/// and optionally dialogue capability (<see cref="CanSpeak"/>, <see cref="WayToSpeakDescription"/>).
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

    // ── Dialogue ──────────────────────────────────────────────────────────────

    /// <summary>Whether this NPC can be spoken to (has a way-to-speak description).</summary>
    public bool CanSpeak { get; }

    /// <summary>
    /// Natural-language description of how this NPC speaks — used as the LLM system prompt
    /// for the NPC's dedicated dialogue slot.
    /// Null when <see cref="CanSpeak"/> is false.
    /// </summary>
    public string? WayToSpeakDescription { get; }

    /// <summary>
    /// Per-instance affinity table tracking relationships with party members and other NPCs.
    /// Populated at spawn time from the scene's persisted affinity store.
    /// </summary>
    public AffinityTable AffinityTable { get; }

    // ── IsAlive ───────────────────────────────────────────────────────────────

    private bool _isSlain = false;

    /// <inheritdoc/>
    public bool IsAlive
    {
        get => !_isSlain && Combatant.CurrentHp > 0;
        set { if (!value) _isSlain = true; else _isSlain = false; }
    }

    /// <inheritdoc/>
    public bool IsPersistent { get; }

    /// <inheritdoc/>
    public KeywordInContext[] NarrationKeywordsInContext { get; }

    /// <inheritdoc/>
    public string ObservationHint { get; }

    /// <inheritdoc/>
    public string SpeciesName => Archetype.Species.DisplayName;

    // ── Constructor ───────────────────────────────────────────────────────────

    public NpcEntity(
        string              npcId,
        EnemyCombatant      combatant,
        NamedNpcArchetype   archetype,
        bool                isHostile,
        bool                isPersistent,
        KeywordInContext[]  narrationKeywordsInContext,
        string              observationHint,
        bool                canSpeak                = false,
        string?             wayToSpeakDescription   = null,
        AffinityTable?      affinityTable           = null)
    {
        NpcId                      = npcId;
        Combatant                  = combatant;
        Archetype                  = archetype;
        IsHostile                  = isHostile;
        IsPersistent               = isPersistent;
        NarrationKeywordsInContext = narrationKeywordsInContext;
        ObservationHint            = observationHint;
        CanSpeak                   = canSpeak;
        WayToSpeakDescription      = wayToSpeakDescription;
        AffinityTable              = affinityTable ?? new AffinityTable();
    }

    // ── Corpse generation ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public CorpseSpot GenerateCorpse(Area area)
        => CorpseRegistry.CreateForNamedNpc(this, area);
}
