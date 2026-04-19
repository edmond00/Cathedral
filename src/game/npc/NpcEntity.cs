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
    public NamedNpcArchetype Archetype { get; }

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

    // ── Witness / authority ───────────────────────────────────────────────────

    /// <summary>
    /// When true this NPC will not flee or submit when confronting a criminal —
    /// they will demand a fight instead (sets <see cref="FightRequestedByDialogue"/>).
    /// </summary>
    public bool IsBrave { get; }

    /// <summary>
    /// Relative authority level (0 = none, higher = more official).
    /// Guards and law-enforcement archetypes set this > 0; commoners leave it 0.
    /// </summary>
    public int AuthorityLevel { get; }

    /// <summary>
    /// Ids of scene sections this NPC considers their own property.
    /// An intruder in one of these sections triggers witness confrontation.
    /// Populated at spawn time from <see cref="NamedNpcArchetype.DefaultOwnedSectionIds"/>;
    /// the scene factory may append additional IDs after the scene is built.
    /// </summary>
    public List<string> OwnedSectionIds { get; }

    /// <summary>
    /// Set to true by a "caught red-handed" dialogue when the NPC demands combat
    /// instead of accepting an apology or lie. Checked by the game controller
    /// after dialogue ends to transition into fight mode.
    /// </summary>
    public bool FightRequestedByDialogue { get; set; }

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
        string              observationHint,
        bool                canSpeak                = false,
        string?             wayToSpeakDescription   = null,
        AffinityTable?      affinityTable           = null,
        bool                isBrave                 = false,
        int                 authorityLevel          = 0,
        IReadOnlyList<string>? ownedSectionIds      = null)
    {
        NpcId                      = npcId;
        Combatant                  = combatant;
        Archetype                  = archetype;
        IsHostile                  = isHostile;
        IsPersistent               = isPersistent;
        ObservationHint            = observationHint;
        CanSpeak                   = canSpeak;
        WayToSpeakDescription      = wayToSpeakDescription;
        AffinityTable              = affinityTable ?? new AffinityTable();
        IsBrave                    = isBrave;
        AuthorityLevel             = authorityLevel;
        OwnedSectionIds            = ownedSectionIds != null ? new List<string>(ownedSectionIds) : [];
    }

    // ── Corpse generation ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public CorpseSpot GenerateCorpse(Area area)
        => CorpseRegistry.CreateForNamedNpc(this, area);
}
