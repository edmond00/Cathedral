using System;
using Cathedral;
using Cathedral.Game.Dialogue.Affinity;
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

    /// <summary>
    /// Whether spawned NPCs start as enemies of the protagonist (e.g. bears, wolves, boars).
    /// When true, the enemy flag is set in each spawned NPC's AffinityTable at scene
    /// initialization (see the scene-build loop in the game controller).
    /// </summary>
    public virtual bool DefaultEnemy => false;

    /// <summary>Whether spawned NPCs persist across visits (named characters).</summary>
    public abstract bool DefaultPersistent { get; }

    /// <summary>
    /// Optional forced gender for spawned NPCs of inherently gendered roles (a dairymaid, a reeve).
    /// Null means the gender is a seeded 50/50 flip. Only meaningful for human archetypes; beasts
    /// have no gender and ignore this.
    /// </summary>
    protected virtual NameGender? GenderBias => null;

    /// <summary>How many modiMentis to assign at creation.</summary>
    public virtual int ModiMentisCount => 8;

    // ── Age ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Youngest age, in days, an instance of this archetype may spawn at. Override together with
    /// <see cref="MaxAgeDays"/> to give an archetype its own age band — an apprentice should be
    /// young, a reeve middle-aged, a wolf short-lived. The default band is a working adult of
    /// 18–55 years.
    /// </summary>
    public virtual int MinAgeDays => 18 * LifetimeStat.DaysPerYear;

    /// <summary>Oldest age, in days, an instance of this archetype may spawn at.</summary>
    public virtual int MaxAgeDays => 55 * LifetimeStat.DaysPerYear;

    /// <summary>
    /// Whether spawned NPCs can be spoken to.
    /// Subclasses that provide a <see cref="GenerateWayToSpeakDescription"/> should override this to true.
    /// </summary>
    public virtual bool CanSpeak => false;

    /// <summary>
    /// What a person of this sort can actually teach you about their own work — the lesson GATHER
    /// KNOWLEDGE grants when the player asks them about their trade.
    ///
    /// <para>Defaults to <c>peasantry</c>, the knowledge of ordinary working life, which is the
    /// honest answer for anyone whose trade is not a craft. Craft archetypes override it with the
    /// craft itself, so asking a blacksmith about his work teaches metalwork and asking a miller
    /// about his teaches milling.</para>
    /// </summary>
    public virtual string TradeModusMentisId => "peasantry";

    /// <summary>
    /// Archetype ids this person is willing and able to put the player in front of — see
    /// <c>IntroduceMeVerb</c>. Empty for almost everybody.
    ///
    /// <para>An introduction has to be socially real to be worth having, so this is deliberately not
    /// "anyone I share a village with": an apprentice can walk you to their own master, and a
    /// labourer can take you to the reeve who sets their work, because in both cases the speaker has
    /// standing with the third party. Nobody else has standing with anybody.</para>
    /// </summary>
    public virtual IReadOnlyList<string> CanIntroduceToArchetypes => Array.Empty<string>();

    /// <summary>
    /// What this person calls the people in <see cref="CanIntroduceToArchetypes"/> — "my master",
    /// "the reeve". Fills <c>{third:relation}</c>, so the offer reads in their own words.
    /// </summary>
    public virtual string IntroductionRelation => "someone I know";

    /// <summary>
    /// Small things this sort of person keeps loose about them, for <c>PickpocketVerb</c> to find.
    /// Empty by default, in which case a picked pocket yields only coins.
    ///
    /// <para>Factories rather than instances, because each pick must produce a fresh item — a shared
    /// instance would end up in two inventories at once.</para>
    /// </summary>
    public virtual IReadOnlyList<Func<Narrative.Item>> PocketItems => Array.Empty<Func<Narrative.Item>>();

    /// <summary>
    /// The period this archetype sleeps through. Night for everyone so far; the property exists
    /// because a miller starting before dawn or a watchman sleeping by day are the obvious next
    /// content, and because <c>SceneNpc.IsSleeping</c> needs somewhere to ask.
    /// </summary>
    public virtual Narrative.TimePeriod SleepPeriod => Narrative.TimePeriod.Night;

    /// <summary>
    /// Whether spawned NPCs confront criminals bravely (demand fight) rather than submitting.
    /// Override to true for guards, owners, or aggressive archetypes.
    /// </summary>
    public virtual bool IsBrave => false;

    /// <summary>
    /// Relative authority level (0 = civilian, higher = more official enforcement power).
    /// Override in guard/lawkeeper archetypes.
    /// </summary>
    public virtual int AuthorityLevel => 0;

    /// <summary>
    /// Optional combat-personality override consulted by the fight builder. When non-null
    /// this is used directly as the fighter's <c>AiPersonality</c>; when null the personality
    /// is derived from <see cref="IsBrave"/> and <see cref="AuthorityLevel"/>. Override on
    /// archetypes that want to hand-tune their feel (wolf vs bear vs brigand).
    /// </summary>
    public virtual Cathedral.Fight.AiPersonality? AiPersonalityOverride => null;

    /// <summary>
    /// Section ids that spawned NPCs own by default.
    /// Override to list section ids this archetype has authority over (e.g. farmhouse interior).
    /// </summary>
    public virtual IReadOnlyList<string> DefaultOwnedSectionIds => [];

    // ── Trade ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tag of the goods this NPC <b>sells</b> (the player can buy these from them).
    /// Null means the NPC sells nothing. See the trade-system plan, Part A.
    /// </summary>
    public virtual Narrative.ItemTag? SellTag => null;

    /// <summary>
    /// Tag of the goods this NPC <b>buys</b> (the player can sell these to them).
    /// Null means the NPC buys nothing.
    /// </summary>
    public virtual Narrative.ItemTag? BuyTag => null;

    /// <summary>
    /// The social stratum this NPC measures others against. Garments appealing to it grant bonus
    /// dialogue dice — dressing like a townsman opens a craftsman's door and does nothing at all
    /// for a hermit.
    ///
    /// Required for every archetype that can speak; <c>--npc-audit</c> fails on a null. Beasts and
    /// other non-speakers leave it null, since nothing ever consults it for them.
    /// </summary>
    public virtual SocialCategory? Social => null;

    // ── Spawn ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a new <see cref="NpcEntity"/> from this archetype.
    /// <paramref name="affinityResolver"/>, when given, supplies the NPC's persistent
    /// <see cref="AffinityTable"/>; it is called with the NPC's stable name-derived id
    /// (<c>{archetypeId}_{name}</c>) — per NPC, so two NPCs of the same role never share a table.
    /// The resolver form exists because the name is only generated here, inside Spawn, so callers
    /// cannot compute the key beforehand. Null (or a null return) spawns with a fresh table.
    /// </summary>
    public NpcEntity Spawn(Random rng, string nodeContext = "", Func<string, AffinityTable?>? affinityResolver = null)
    {
        // Seed a dedicated name RNG from the (deterministic, per-location) scene stream so a given
        // NPC's name is stable across visits and reproducible under --seed, independent of later
        // draw order. See the naming plan / SceneFactory.CreateSeededRandom.
        var nameRng = new Random(rng.Next());

        var combatant = new EnemyCombatant("", Species);

        // Humans get a seeded biological sex stamped on the combatant; GenderStat then reflects it.
        // Beasts have no genitories and no gender — they use the descriptive beast generator.
        bool isHuman = Species.AnatomyType == AnatomyType.Human;
        if (isHuman)
            combatant.BiologicalSexMale = GenderFor(nameRng);

        // Read gender back through GenderStat so it is the single source of truth for the name.
        string name = isHuman
            ? Naming.NameGenerator.GenerateHuman(NpcLabelResolver.GenderIsMale(combatant), nameRng)
            : Naming.NameGenerator.GenerateBeast(nameRng);
        combatant.SetDisplayName(name);

        // The stable name-derived id: names are per-NPC and reproducible across visits (nameRng),
        // so this identifies THIS individual — used as the affinity-persistence key for everyone,
        // and as the npcId for persistent NPCs.
        var stableId = $"{ArchetypeId}_{name.ToLowerInvariant().Replace(' ', '_')}";
        var npcId = DefaultPersistent ? stableId : $"{ArchetypeId}_{rng.Next(100000)}";

        // Body, skills, belongings and personality traits — all rolled off the stable id, so this
        // individual is the same person every visit and under every replay of the same --seed.
        var traits = Generation.NpcContentGenerator.Generate(this, combatant, stableId, isHuman);

        // Appearance and persona are keyed to the stable id too: npcId carries a fresh random number
        // for non-persistent NPCs and would give the same person a new face on every visit.
        var hint          = PickObservationHint(stableId, nodeContext);
        var wayToSpeak    = CanSpeak ? GenerateWayToSpeakDescription(name, nameRng) : "";
        var text          = Generation.NpcContentGenerator.BuildTextProfile(hint, wayToSpeak, traits, stableId);
        var affinityTable = affinityResolver?.Invoke(stableId) ?? new AffinityTable();

        return new NpcEntity(
            npcId, stableId, combatant, this,
            DefaultPersistent,
            text,
            canSpeak:              CanSpeak,
            affinityTable:         affinityTable,
            isBrave:               IsBrave,
            authorityLevel:        AuthorityLevel,
            ownedSectionIds:       DefaultOwnedSectionIds);
    }

    // ── Generation inputs ─────────────────────────────────────────────────────

    /// <summary>
    /// Organ-part ids this trade builds up — a smith's arms and hands, a shepherd's eyes and legs.
    /// The generator aims most of its organ points here, so an archetype is physically recognisable
    /// before any personality trait is applied. Ids not present on the species are ignored.
    /// </summary>
    public virtual IReadOnlyList<string> OrganEmphasis => Array.Empty<string>();

    /// <summary>
    /// What every member of this trade is carrying: working tools and the clothes on their back.
    /// Factories, not instances — each NPC needs its own items.
    /// </summary>
    public virtual IReadOnlyList<Func<Narrative.Item>> Loadout => Array.Empty<Func<Narrative.Item>>();

    /// <summary>
    /// Extras a member of this trade <i>might</i> be carrying; the generator takes a random 0–3 of
    /// them. This is what stops every miller in the world carrying an identical pack.
    /// </summary>
    public virtual IReadOnlyList<Func<Narrative.Item>> OptionalLoadout => Array.Empty<Func<Narrative.Item>>();

    /// <summary>Resolves the sex to stamp: a forced <see cref="GenderBias"/> or a seeded 50/50 flip.</summary>
    private bool GenderFor(Random rng) => GenderBias switch
    {
        NameGender.Male   => true,
        NameGender.Female => false,
        _                 => rng.Next(2) == 0,
    };

    // ── Observation hint ──────────────────────────────────────────────────────

    /// <summary>
    /// Override to provide 2-4 interchangeable observation-hint variants. Each describes only
    /// the NPC's appearance/activity — <b>never</b> the proper name and <b>never</b> the role
    /// (the role now lives in the dynamic label; see <see cref="RoleNoun"/>). One variant is
    /// chosen deterministically per NPC by <see cref="PickObservationHint"/>.
    /// </summary>
    protected abstract string[] ObservationHintVariants(string nodeContext);

    /// <summary>
    /// Picks one <see cref="ObservationHintVariants"/> entry, seeded deterministically by the
    /// NPC id so a given NPC always looks the same within a run (and reproducibly under
    /// <c>--seed</c>). Uses <see cref="GameRng.DerivedSeed"/> (a stable FNV-1a hash) rather than
    /// <see cref="string.GetHashCode"/>, which is randomized per process.
    /// </summary>
    private string PickObservationHint(string stableId, string nodeContext)
    {
        var variants = ObservationHintVariants(nodeContext);
        var rng = new Random(GameRng.DerivedSeed($"npc_hint_{stableId}"));
        return variants[rng.Next(variants.Length)];
    }

    // ── Dynamic label ─────────────────────────────────────────────────────────

    /// <summary>
    /// Short role noun used to build the NPC's contextual label (e.g. "blacksmith", "wolf").
    /// Fed into <see cref="BuildRoleClause"/> and thereby <see cref="NpcLabelResolver"/>.
    /// </summary>
    public abstract string RoleNoun { get; }

    /// <summary>
    /// Whether the role clause mentions the current location ("the blacksmith of the village").
    /// Override to false for creatures/roles where a location reads oddly (wild animals).
    /// </summary>
    protected virtual bool LabelMentionsLocation => true;

    /// <summary>
    /// Builds the role portion of the label from the current location noun (already lower-cased,
    /// possibly empty). Always grammatical and short; override wholesale for irregular phrasing.
    /// </summary>
    public virtual string BuildRoleClause(string locationNoun)
        => LabelMentionsLocation && locationNoun.Length > 0
            ? $"the {RoleNoun} of the {locationNoun}"
            : $"the {RoleNoun}";

    /// <summary>
    /// Override to return a natural-language description of how this NPC speaks.
    /// This text is used as the LLM system prompt for the NPC's dialogue slot.
    /// Only called when <see cref="CanSpeak"/> is true.
    /// </summary>
    protected virtual string GenerateWayToSpeakDescription(string name, Random rng) => string.Empty;

    // ── Dialogue flavour ──────────────────────────────────────────────────────
    //
    // Everything below feeds the {scope:field} tokens an authored dialogue replica can contain
    // (see DialogueTemplate). It is short, neutral, in-character prose: the token is expanded
    // BEFORE the persona rewrite, so these are raw material for the LLM, not finished lines.
    // Every member has a role-derived default, so an archetype that overrides nothing still
    // expands every token to something grammatical.

    /// <summary>
    /// How this NPC names their own place in the world when meeting someone for the first time —
    /// a clause that completes "I'm …" or stands alone: <c>"the smith here — I keep the forge at
    /// the lane's end"</c>. Used by <c>{npc:introduction}</c> in the meet-stranger tree.
    /// </summary>
    public virtual string SelfIntroduction => $"the {RoleNoun} here";

    /// <summary>
    /// Where this NPC's working day happens, as a noun phrase: "the forge", "the fields",
    /// "the mill". Used by <c>{npc:workplace}</c>.
    /// </summary>
    public virtual string Workplace => "these parts";

    /// <summary>
    /// The name of the craft or calling itself, as a bare noun: "smithing", "the plough",
    /// "the flock". Used by <c>{npc:craft}</c>.
    /// </summary>
    public virtual string Craft => $"a {RoleNoun}'s work";

    /// <summary>
    /// One concrete sentence about what this NPC actually does all day — the material, the tool,
    /// the complaint. Used by <c>{npc:labour}</c>.
    /// </summary>
    public virtual string DailyLabour => $"the work of a {RoleNoun}, day in and day out";

    /// <summary>
    /// The archetype's own views on ordinary conversational subjects, as first-person clauses
    /// ready to be dropped into a replica: <c>[DialogueTopic.Weather] = "rain I can work in; it's
    /// the wind that ruins a fire"</c>. Override with the handful of topics the role actually has
    /// a distinctive view on — <see cref="OpinionOn"/> falls back to a neutral villager's answer
    /// for the rest, so a partial table is the normal case.
    /// </summary>
    protected virtual IReadOnlyDictionary<Dialogue.Tree.DialogueTopic, string> TopicOpinions
        => EmptyOpinions;

    private static readonly IReadOnlyDictionary<Dialogue.Tree.DialogueTopic, string> EmptyOpinions =
        new Dictionary<Dialogue.Tree.DialogueTopic, string>();

    private IReadOnlyDictionary<Dialogue.Tree.DialogueTopic, string>? _opinionCache;

    /// <summary>
    /// This archetype's opinion on <paramref name="topic"/> — its own if it has one, otherwise the
    /// shared villager default. Never empty, so <c>{npc:opinion_*}</c> always expands.
    /// </summary>
    public string OpinionOn(Dialogue.Tree.DialogueTopic topic)
    {
        _opinionCache ??= TopicOpinions;
        return _opinionCache.TryGetValue(topic, out var opinion) && !string.IsNullOrWhiteSpace(opinion)
            ? opinion
            : DefaultOpinion(topic);
    }

    /// <summary>
    /// The answer any villager could give — used wherever an archetype has authored nothing. Written
    /// flat on purpose: it should read as small talk, not as a character note.
    /// </summary>
    private static string DefaultOpinion(Dialogue.Tree.DialogueTopic topic) => topic switch
    {
        Dialogue.Tree.DialogueTopic.Weather    => "it does what it likes and we do what we can under it",
        Dialogue.Tree.DialogueTopic.Seasons    => "each part of the year asks its own work of you, and it never asks politely",
        Dialogue.Tree.DialogueTopic.Harvest    => "a good year feeds you and a bad one teaches you; we've had both",
        Dialogue.Tree.DialogueTopic.Food       => "plain fare, enough of it, and hot — that's all I ask of a table",
        Dialogue.Tree.DialogueTopic.Beasts     => "a beast will tell you what it needs if you've the patience to watch it",
        Dialogue.Tree.DialogueTopic.Wilds      => "the wild has its uses, but I'd not sleep out in it by choice",
        Dialogue.Tree.DialogueTopic.Water      => "too little and everything withers, too much and it all rots; there's no pleasing it",
        Dialogue.Tree.DialogueTopic.Kin        => "you're born to your people and you make the best of them",
        Dialogue.Tree.DialogueTopic.Health     => "keep dry, keep fed, and don't let a small hurt turn into a big one",
        Dialogue.Tree.DialogueTopic.Work       => "work's work — it doesn't care whether you're in the mood",
        Dialogue.Tree.DialogueTopic.Rest       => "an evening with nothing owed to anyone is worth more than most folk reckon",
        Dialogue.Tree.DialogueTopic.Stories    => "the old tales are half nonsense, but the half that isn't will keep you alive",
        Dialogue.Tree.DialogueTopic.Neighbours => "folk here are decent enough, so long as you don't cross them twice",
        Dialogue.Tree.DialogueTopic.Trade      => "a fair price is one both sides grumble at",
        Dialogue.Tree.DialogueTopic.Omens      => "I don't hold with signs, but I've noticed some of them are right",
        Dialogue.Tree.DialogueTopic.Roads      => "roads bring news and thieves in about equal measure",
        _                                      => "I've no strong view on it, truth be told",
    };
}

/// <summary>Forced gender for an archetype's spawned NPCs (see <see cref="NamedNpcArchetype.GenderBias"/>).</summary>
public enum NameGender
{
    Male,
    Female,
}
