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

    // ── Spawn ─────────────────────────────────────────────────────────────────

    /// <summary>Spawns a new <see cref="NpcEntity"/> from this archetype.</summary>
    public NpcEntity Spawn(Random rng, string nodeContext = "", AffinityTable? savedAffinity = null)
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

        var npcId = DefaultPersistent
            ? $"{ArchetypeId}_{name.ToLowerInvariant().Replace(' ', '_')}"
            : $"{ArchetypeId}_{rng.Next(100000)}";

        combatant.InitializeModiMentis(ModusMentisRegistry.Instance, ModiMentisCount);

        // Age is sampled from this archetype's band and stamped as a birth time against the clock as
        // it reads now, so the NPC is exactly this old today and ages onward from here.
        // SetAgeAtCreation clamps the roll to the combatant's own heart-derived lifetime, so a wide
        // band can never produce someone born already past their death date.
        int lo = Math.Min(MinAgeDays, MaxAgeDays);
        int hi = Math.Max(MinAgeDays, MaxAgeDays);
        combatant.SetAgeAtCreation(rng.Next(lo, hi + 1));

        var hint           = PickObservationHint(npcId, nodeContext);
        var wayToSpeak     = CanSpeak ? GenerateWayToSpeakDescription(name, rng) : null;
        var affinityTable  = savedAffinity ?? new AffinityTable();

        return new NpcEntity(
            npcId, combatant, this,
            DefaultPersistent,
            hint,
            canSpeak:              CanSpeak,
            wayToSpeakDescription: wayToSpeak,
            affinityTable:         affinityTable,
            isBrave:               IsBrave,
            authorityLevel:        AuthorityLevel,
            ownedSectionIds:       DefaultOwnedSectionIds);
    }

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
    private string PickObservationHint(string npcId, string nodeContext)
    {
        var variants = ObservationHintVariants(nodeContext);
        var rng = new Random(GameRng.DerivedSeed($"npc_hint_{npcId}"));
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
}

/// <summary>Forced gender for an archetype's spawned NPCs (see <see cref="NamedNpcArchetype.GenderBias"/>).</summary>
public enum NameGender
{
    Male,
    Female,
}
