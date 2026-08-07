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
    /// <remarks>
    /// The name-derived <c>{archetypeId}_{name}</c> that <see cref="NamedNpcArchetype.Spawn"/> also
    /// files affinity under. It used to be computed inside Spawn and dropped, which left the entity
    /// holding only <see cref="NpcId"/> — random for anyone non-persistent, and so useless to
    /// anything that has to recognise this individual on the next visit.
    /// </remarks>
    public string PersistentId { get; }

    /// <inheritdoc/>
    public string DisplayName => Combatant.DisplayName;

    /// <summary>The underlying party member used for anatomy, wounds, stats, and combat.</summary>
    public EnemyCombatant Combatant { get; }

    /// <summary>The archetype that spawned this NPC (wolf, druid, etc.).</summary>
    public NamedNpcArchetype Archetype { get; }

    NpcArchetype INpcEntity.Archetype => Archetype;

    // ── Dialogue ──────────────────────────────────────────────────────────────

    /// <summary>Whether this NPC can be spoken to (has a way-to-speak description).</summary>
    public bool CanSpeak { get; }

    /// <summary>
    /// Every piece of natural-language text about this individual — appearance, LLM persona prompt,
    /// and the dialogue-flavour overrides its personality traits imposed. Composed once at spawn.
    /// </summary>
    public Traits.NpcTextProfile Text { get; }

    /// <summary>
    /// Natural-language description of how this NPC speaks — used as the LLM system prompt
    /// for the NPC's dedicated dialogue slot. Null when <see cref="CanSpeak"/> is false.
    /// This is the archetype's brief <b>plus</b> whatever its traits added, so a greedy smith and a
    /// pious one are told different things about themselves.
    /// </summary>
    public string? WayToSpeakDescription => CanSpeak ? Text.PersonaPrompt : null;

    // ── Trait-aware dialogue flavour ──────────────────────────────────────────
    //
    // Each of these prefers what this individual's traits decided and falls back to the archetype's
    // default. DialogueTemplate reads them rather than the archetype directly, so a {npc:…} token
    // resolves per person instead of per trade.

    /// <summary>How this NPC introduces themselves — <c>{npc:introduction}</c>.</summary>
    public string SelfIntroduction => Text.SelfIntroduction ?? Archetype.SelfIntroduction;

    /// <summary>What their working day is actually like — <c>{npc:labour}</c>.</summary>
    public string DailyLabour => Text.DailyLabour ?? Archetype.DailyLabour;

    /// <summary>This NPC's own view on <paramref name="topic"/> — <c>{npc:opinion_*}</c>.</summary>
    public string OpinionOn(Dialogue.Tree.DialogueTopic topic)
        => Text.Opinion(topic) ?? Archetype.OpinionOn(topic);

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
    /// Set when this person has agreed, in conversation, to travel with the player. Acted on by the
    /// controller after the dialogue session closes — the same deferred pattern
    /// <see cref="TradeRequest"/> and <see cref="JobRequest"/> use, and for the same reason: a
    /// dialogue outcome can reach the NPC and nothing else.
    /// </summary>
    public bool JoinRequested { get; set; }

    /// <summary>
    /// Set alongside <see cref="FightRequestedByDialogue"/> when the fight was goaded out of this
    /// person specifically, so nobody else joins in. Being provoked into swinging at one person is
    /// not a call for help — and getting somebody on their own is the entire reason to provoke them
    /// rather than simply attack.
    /// </summary>
    public bool FightIsPersonal { get; set; }

    /// <summary>
    /// Copper this person handed over after a successful beg, waiting to be credited to the party
    /// wallet once the conversation closes. Same deferred pattern as every other dialogue result.
    /// </summary>
    public int AlmsGiven { get; set; }

    /// <summary>
    /// The person this NPC has been asked to introduce the player to, set by <c>IntroduceMeVerb</c>
    /// before the conversation opens and read by the dialogue adapter when it builds the context.
    /// Same hand-off as <see cref="PendingJobOffer"/>: the verb knows which of several possible
    /// third parties the player picked, and the adapter is where that has to arrive.
    /// </summary>
    public NpcEntity? PendingIntroductionTarget { get; set; }

    /// <summary>Set when an introduction succeeded, so the controller can move the player to them.</summary>
    public NpcEntity? IntroductionGranted { get; set; }

    /// <summary>
    /// Set to true by a "caught red-handed" dialogue when the NPC demands combat
    /// instead of accepting an apology or lie. Checked by the game controller
    /// after dialogue ends to transition into fight mode.
    /// </summary>
    public bool FightRequestedByDialogue { get; set; }

    // ── Trade ───────────────────────────────────────────────────────────────────

    /// <summary>Tag of the goods this NPC sells (player can buy). Null = sells nothing.</summary>
    public Narrative.ItemTag? SellTag => Archetype.SellTag;

    /// <summary>Tag of the goods this NPC buys (player can sell). Null = buys nothing.</summary>
    public Narrative.ItemTag? BuyTag => Archetype.BuyTag;

    /// <summary>
    /// Set by a successful propose-to-buy / propose-to-sell dialogue. Checked by the game
    /// controller after dialogue ends to open the trade menu (mirrors <see cref="FightRequestedByDialogue"/>).
    /// </summary>
    public Trade.TradeMode TradeRequest { get; set; } = Trade.TradeMode.None;

    // ── Work ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The job the player asked to work, set when the REQUEST_JOB verb succeeds and read by the
    /// request-job dialogue's terminal outcome. Cleared once consumed.
    /// </summary>
    public Narrative.Work.Job? PendingJobOffer { get; set; }

    /// <summary>
    /// Set by a successful request-job dialogue to <see cref="PendingJobOffer"/>. Checked by the game
    /// controller after dialogue ends to open the work menu (mirrors <see cref="TradeRequest"/>).
    /// </summary>
    public Narrative.Work.Job? JobRequest { get; set; }

    private Trade.NpcTradeCatalog? _buyCatalog;
    private Trade.NpcTradeCatalog? _sellCatalog;

    /// <summary>The catalogue of goods this NPC sells to the player (lazy, cached, seeded by id).</summary>
    public Trade.NpcTradeCatalog? BuyCatalog =>
        SellTag is { } tag ? _buyCatalog ??= Trade.NpcTradeCatalog.Build(NpcId, Trade.TradeMode.Buy, tag) : null;

    /// <summary>The catalogue of goods this NPC buys from the player (lazy, cached, seeded by id).</summary>
    public Trade.NpcTradeCatalog? SellCatalog =>
        BuyTag is { } tag ? _sellCatalog ??= Trade.NpcTradeCatalog.Build(NpcId, Trade.TradeMode.Sell, tag) : null;

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
    /// <remarks>
    /// The archetype's chosen appearance line with each trait's visible mark appended, so a scar or a
    /// missing eye is something the player can actually notice on observing them.
    /// </remarks>
    public string ObservationHint => Text.ObservationHint;

    /// <inheritdoc/>
    public string SpeciesName => Archetype.Species.DisplayName;

    // ── Constructor ───────────────────────────────────────────────────────────

    public NpcEntity(
        string              npcId,
        string              persistentId,
        EnemyCombatant      combatant,
        NamedNpcArchetype   archetype,
        bool                isPersistent,
        Traits.NpcTextProfile text,
        bool                canSpeak                = false,
        AffinityTable?      affinityTable           = null,
        bool                isBrave                 = false,
        int                 authorityLevel          = 0,
        IReadOnlyList<string>? ownedSectionIds      = null)
    {
        NpcId                      = npcId;
        PersistentId               = persistentId;
        Combatant                  = combatant;
        Archetype                  = archetype;
        IsPersistent               = isPersistent;
        Text                       = text;
        CanSpeak                   = canSpeak;
        AffinityTable              = affinityTable ?? new AffinityTable();
        IsBrave                    = isBrave;
        AuthorityLevel             = authorityLevel;
        OwnedSectionIds            = ownedSectionIds != null ? new List<string>(ownedSectionIds) : [];
    }

    // ── Corpse generation ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public List<PointOfInterest> GenerateCorpse()
        => CorpseRegistry.CreateForNamedNpc(this);
}
