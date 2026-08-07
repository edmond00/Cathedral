using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for all party members (protagonist and companions).
/// Holds the shared physical and modusMentis state: body parts, organs, humors, derived stats,
/// inventory, and the modusMentis set used during narrative execution.
///
/// Subclasses add their own unique data:
///   - <see cref="Protagonist"/>: journal, companion list, current location
///   - <see cref="Companion"/>: display name, relationship to protagonist
/// </summary>
public abstract class PartyMember
{
    /// <summary>Humor-queue filling, shared by every party member — master-seeded so a run replays.</summary>
    private static readonly Random _sharedRng = GameRng.Stream("humor-queues");

    // ── Body ─────────────────────────────────────────────────────
    private List<BodyPart> _bodyParts;

    public List<BodyPart> BodyParts => _bodyParts;
    public List<DerivedStat> DerivedStats { get; private set; }

    /// <summary>
    /// Biological sex, when assigned (true = male, false = female). Set at NPC creation from a seeded
    /// flip and read by <see cref="GenderStat"/>. Left null for members whose sex is not stamped
    /// (e.g. beasts, which have no genitories), in which case <see cref="GenderStat"/> falls back to
    /// the genitories organ score. Kept separate from that score so it never affects HP or
    /// genitories-based skills.
    /// </summary>
    public bool? BiologicalSexMale { get; set; }

    // ── Humor queues ──────────────────────────────────────────────
    /// <summary>
    /// The four FIFO humor queues (Hepar, Paunch, Pulmones, Spleen), each holding
    /// 49 humor instances filled at creation time via organ-score-based secretion.
    /// </summary>
    public HumorQueueSet HumorQueues { get; private set; }

    // ── Memory ────────────────────────────────────────────────────
    public List<MemoryModule> MemoryModules { get; private set; }

    // ── ModiMentis ───────────────────────────────────────────────────
    public List<ModusMentis> ModiMentis { get; set; }
    /// <summary>Alias for ModiMentis — kept for call-site compatibility.</summary>
    public List<ModusMentis> LearnedModiMentis { get; set; }

    // ── Inventory ────────────────────────────────────────────────
    /// <summary>
    /// Items that could not be placed in any anchor slot or container.
    /// These overflow items are awaiting a free spot.
    /// </summary>
    public List<Item> Inventory { get; set; }

    /// <summary>
    /// Equipment slots: each anchor holds a list of items (up to <see cref="EquipmentAnchorExtensions.Capacity"/> slots worth).
    /// </summary>
    public Dictionary<EquipmentAnchor, List<Item>> EquippedItems { get; private set; }

    // ── Wounds ───────────────────────────────────────────────────
    /// <summary>Active wounds currently affecting this party member.</summary>
    public List<WoundInstance> Wounds { get; private set; } = new();

    // ── Age ───────────────────────────────────────────────────────
    /// <summary>
    /// The reading on the run's global clock (<see cref="GameClock.Days"/>) at the moment this member
    /// was born. Anyone alive when the run begins was therefore born before day zero, so this is
    /// normally negative — a protagonist starting at 14 years old has a birth time of about −5040.
    ///
    /// <para>
    /// Age is never stored: it is always <c>GameClock.Days − BirthTimeDays</c>, so every member ages
    /// automatically as the clock advances, with nothing to keep in sync. Set once at creation via
    /// <see cref="SetAgeAtCreation"/>.
    /// </para>
    /// </summary>
    public double BirthTimeDays { get; private set; } = -DefaultAgeDays;

    /// <summary>Age a member falls back to when nobody sets one explicitly: 25 years.</summary>
    private const double DefaultAgeDays = 25 * LifetimeStat.DaysPerYear;

    /// <summary>
    /// Stamps this member's birth time so that they are <paramref name="ageDays"/> old right now.
    /// Call at creation, after organ scores are settled (the age is clamped against the member's
    /// heart-derived lifetime, so it must be able to read that).
    ///
    /// <para>
    /// The age is capped at <see cref="MaxFractionOfLifeAtCreation"/> of the member's lifetime so no
    /// one is ever born already past their death date — an NPC rolled old from a wide archetype range
    /// but dealt a weak heart would otherwise drop dead the moment they joined the party.
    /// </para>
    /// </summary>
    public void SetAgeAtCreation(double ageDays)
    {
        double capped = Math.Min(Math.Max(0, ageDays), GetLifetimeDays() * MaxFractionOfLifeAtCreation);
        BirthTimeDays = GameClock.Days - capped;
    }

    /// <summary>The most of their lifetime a member may already have spent when created (90%).</summary>
    private const double MaxFractionOfLifeAtCreation = 0.9;

    /// <summary>This member's current age in days, measured against the run's global clock.</summary>
    public double GetAgeDays() => GameClock.Days - BirthTimeDays;

    /// <summary>
    /// The total span of days this member's body is granted, from the heart-derived
    /// <see cref="LifetimeStat"/>. Wound-aware: damage to the heart really does shorten it.
    /// </summary>
    public int GetLifetimeDays() => new LifetimeStat().GetValue(this);

    /// <summary>Days of life this member has left; zero once they are due to die.</summary>
    public double GetRemainingLifeDays() => Math.Max(0, GetLifetimeDays() - GetAgeDays());

    /// <summary>True once this member has lived out their full lifetime and should die of old age.</summary>
    public bool IsDeadOfOldAge() => GetAgeDays() >= GetLifetimeDays();

    // ── Species & anatomy ─────────────────────────────────────────
    /// <summary>The species of this party member (determines anatomy type and art folder).</summary>
    public Species Species { get; }

    /// <summary>Shortcut to the anatomy type used by this member's species.</summary>
    public AnatomyType AnatomyType => Species.AnatomyType;

    /// <summary>
    /// What this body is able to do beyond its organs (see <see cref="AnatomyCapability"/>) — read
    /// from the anatomy, never stored, so it cannot drift from the species.
    /// </summary>
    public AnatomyCapability Capabilities =>
        AnatomyFactoryRegistry.GetFactory(Species.AnatomyType).Capabilities;

    /// <summary>True when this body has every capability in <paramref name="required"/>.</summary>
    public bool Can(AnatomyCapability required) => (Capabilities & required) == required;

    // ── Display name (subclasses define this differently) ────────
    /// <summary>Human-readable name shown in the party panel.</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Stable identity used to key this member in NPC <see cref="Dialogue.Affinity.AffinityTable"/>s,
    /// deliberately decoupled from <see cref="DisplayName"/>. Defaults to the display name (companions),
    /// but the protagonist overrides it to a fixed constant so a player-chosen/generated display name
    /// never shifts affinity keys mid-run.
    /// </summary>
    public virtual string AffinityKey => DisplayName;

    // ── Constructor ──────────────────────────────────────────────
    protected PartyMember(Species species)
    {
        Species = species ?? throw new ArgumentNullException(nameof(species));
        var factory = AnatomyFactoryRegistry.GetFactory(species.AnatomyType);

        _bodyParts = factory.CreateBodyParts();
        ApplySpeciesMaxScores(species);
        RandomizeOrganScores();

        HumorQueues = InitializeHumorQueues();
        DerivedStats = factory.CreateDerivedStats();
        ModiMentis = new List<ModusMentis>();
        LearnedModiMentis = ModiMentis; // same reference
        Inventory = new List<Item>();
        MemoryModules = new List<MemoryModule>(); // populated after modiMentis are assigned via InitializeMemory()
        Wounds = new List<WoundInstance>();

        // Initialise all 13 anchor slots to empty lists
        EquippedItems = new Dictionary<EquipmentAnchor, List<Item>>();
        foreach (EquipmentAnchor anchor in Enum.GetValues<EquipmentAnchor>())
            EquippedItems[anchor] = new List<Item>();
    }

    // ── Equipment helpers ─────────────────────────────────────────

    /// <summary>Sum of SlotCount for all items in this anchor.</summary>
    public int UsedSlots(EquipmentAnchor anchor) =>
        EquippedItems[anchor].Sum(i => i.SlotCount);

    /// <summary>Remaining slot capacity in this anchor.</summary>
    public int AvailableSlots(EquipmentAnchor anchor) =>
        anchor.Capacity() - UsedSlots(anchor);

    /// <summary>
    /// Append an item to an anchor's list. Used for initialisation / debug setup —
    /// does not enforce capacity.
    /// </summary>
    public void Equip(EquipmentAnchor anchor, Item item) => EquippedItems[anchor].Add(item);

    // ── Carrying weight ───────────────────────────────────────────

    /// <summary>
    /// Total carrying cost of everything held — worn, in hand, and inside anything worn.
    /// Relies on <see cref="GetAllItems"/> descending into nested containers, or a flask inside a
    /// backpack would weigh nothing.
    /// </summary>
    public int CurrentWeight => GetAllItems().Sum(i => i.WeightPoints);

    /// <summary>How much this character can carry before anything further is refused.</summary>
    public int MaxCarryWeight =>
        DerivedStats.FirstOrDefault(s => s.Name == "maximum_weight")?.GetValue(this) ?? 100;

    /// <summary>Free carrying capacity; never negative.</summary>
    public int RemainingWeight => Math.Max(0, MaxCarryWeight - CurrentWeight);

    /// <summary>
    /// True when this character is carrying more than they can bear. Being overloaded never
    /// prevents picking something up — you can always stoop and take one more thing. What it
    /// prevents is <em>travel</em>: the party cannot set out until every member is under their
    /// limit, so the player has to decide what to leave behind rather than being blocked at the
    /// moment of acquisition.
    /// </summary>
    public bool IsOverloaded => CurrentWeight > MaxCarryWeight;

    /// <summary>
    /// One line describing this member for the party panel — species, trade, what they are.
    ///
    /// <para>Lives here rather than on <c>Companion</c> because the roster holds any
    /// <see cref="PartyMember"/> now: a recruited villager or a tamed wolf joins as the body it
    /// already is, and each has something to say about itself.</para>
    /// </summary>
    public virtual string PartyDescription => Species?.DisplayName ?? "";

    /// <summary>How much must be put down before this character can travel; 0 when they are fine.</summary>
    public int ExcessWeight => Math.Max(0, CurrentWeight - MaxCarryWeight);

    // ── Looks ─────────────────────────────────────────────────────

    /// <summary>
    /// Bonus dialogue dice this character's face is worth, from the <c>beauty</c> stat. Wounds to
    /// the visage cost dice here, which is the point of routing it through the stat rather than the
    /// body part's score.
    /// </summary>
    public int BeautyDice =>
        DerivedStats.FirstOrDefault(s => s.Name == "beauty")?.GetValue(this) ?? 0;

    /// <summary>
    /// The most beauty dice this character's anatomy could ever be worth — the length of the bar
    /// the dialogue footer draws <see cref="BeautyDice"/> into.
    /// </summary>
    public int MaxBeautyDice =>
        DerivedStats.FirstOrDefault(s => s.Name == "beauty")?.GetValueAtMaxScore(this) ?? 0;

    // ── Armour ────────────────────────────────────────────────────

    /// <summary>
    /// Bonus defence dice granted by what this character is <em>wearing</em> over
    /// <paramref name="bodyPartId"/>. Contributions from every slot covering the section add up, so
    /// a cloak over a tunic over an undershirt all count on the trunk.
    ///
    /// Two things are deliberate here. First, the garment must sit on an anchor its own
    /// <see cref="WearSlot"/> maps to — armour held in a fist or stuffed in a pack protects
    /// nothing, and iterating <see cref="EquippedItems"/> (which never contains a bag's contents)
    /// enforces that structurally. Second, the section is looked up against this character's actual
    /// <see cref="BodyParts"/> rather than trusting <c>Organ.BodyPartId</c>, because several organ
    /// classes hardcode <c>"visage"</c> even on a beast that has a muzzle instead.
    ///
    /// There is no cap: the ceiling is a content decision, watched by <c>--item-audit</c>.
    /// </summary>
    public int ArmorDiceForSection(string bodyPartId)
    {
        if (!BodyParts.Any(bp => bp.Id == bodyPartId)) return 0;

        int total = 0;
        foreach (var (anchor, items) in EquippedItems)
            foreach (var item in items)
                if (item is WearableItem worn
                    && worn.Slot.AnchorsTo(anchor)
                    && ArmorSections.SectionOf(worn.Slot) == bodyPartId)
                    total += worn.DefenseDice;

        return total;
    }

    /// <summary>
    /// The first vessel with room for <paramref name="liquid"/>, or null when there is none.
    /// Liquids live only in vessels, so this is the whole of their placement logic.
    /// </summary>
    public IContainer? FindVesselFor(Item liquid) =>
        GetAllItems()
            .OfType<IContainer>()
            .FirstOrDefault(c => c.Kind == ContainerKind.Vessel
                              && c.CanContain(liquid)
                              && liquid.SlotCount <= c.AvailableSlots);

    /// <summary>
    /// Try to place an acquired item into the best available slot:
    ///   1. Preferred anchor (if the item declares one and it is free).
    ///   2. Any free anchor that CanAccept the item.
    ///   3. Any equipped ContainerItem that CanContain + has space.
    /// Returns true when the item was placed, false when there is no room anywhere — in which case
    /// the item is NOT taken (it is dropped/ignored, never forced into a bottomless overflow). Pickup
    /// is normally gated earlier by <c>InventoryCapacityRule</c>, so a false here is a last-resort guard.
    /// </summary>
    public bool TryAcquireItem(Item item)
    {
        // 0. Liquids first: one never touches an anchor — it goes in a vessel or nowhere at all,
        //    which is why this precedes the anchor scan rather than joining it. Weight is not
        //    checked; an overloaded character can still pick things up, they just cannot travel.
        if (item.IsLiquid)
        {
            if (FindVesselFor(item) is { } vessel && vessel.TryAdd(item)) return true;
            Console.WriteLine($"PartyMember: no vessel with room for '{item.DisplayName}' — item dropped.");
            return false;
        }

        // 1. Preferred anchor
        if (item.PreferredAnchor.HasValue)
        {
            var preferred = item.PreferredAnchor.Value;
            if (preferred.CanAccept(item) && AvailableSlots(preferred) >= item.SlotCount)
            {
                EquippedItems[preferred].Add(item);
                return true;
            }
        }

        // 2. Any free compatible anchor with capacity
        foreach (EquipmentAnchor anchor in Enum.GetValues<EquipmentAnchor>())
        {
            if (anchor.CanAccept(item) && AvailableSlots(anchor) >= item.SlotCount)
            {
                EquippedItems[anchor].Add(item);
                return true;
            }
        }

        // 3. Any container being carried, however deeply nested
        foreach (var container in GetAllItems().OfType<IContainer>())
            if (container.TryAdd(item)) return true;

        // No room: drop the item rather than overflow.
        Console.WriteLine($"PartyMember: No free anchor/container for '{item.DisplayName}' — item dropped (inventory full).");
        return false;
    }

    /// <summary>
    /// The outcome of an acquisition check: whether the item can be taken, and — when it cannot —
    /// why, phrased as a first-person sentence fragment. The reason matters: it is handed to
    /// <c>InventoryCapacityRule</c>, which fails the action, and the refusal is then re-voiced by
    /// the acting modus mentis rather than swallowed. A pickup must never fail silently.
    /// </summary>
    public readonly record struct AcquireCheck(bool Ok, string? Reason)
    {
        public static AcquireCheck Allow => new(true, null);
        public static AcquireCheck Deny(string reason) => new(false, reason);
    }

    /// <summary>
    /// Non-mutating mirror of <see cref="TryAcquireItem"/>, reporting the reason on refusal.
    /// Checked in the order the player would notice: too heavy first, then nowhere to put it.
    /// </summary>
    public AcquireCheck CanAcquire(Item item)
    {
        // Weight is deliberately NOT checked here. Being overloaded does not stop you picking
        // something up — it stops you walking anywhere with it. See IsOverloaded, which gates
        // travel instead, so the player discovers the problem when they try to leave rather than
        // being quietly refused an item they are standing next to.

        // Liquids never sit on the body: a vessel with room is the only way to hold one.
        if (item.IsLiquid)
            return FindVesselFor(item) is not null
                ? AcquireCheck.Allow
                : AcquireCheck.Deny($"I have no empty vessel to hold {item.WithArticle()}");

        if (item.PreferredAnchor.HasValue)
        {
            var preferred = item.PreferredAnchor.Value;
            if (preferred.CanAccept(item) && AvailableSlots(preferred) >= item.SlotCount)
                return AcquireCheck.Allow;
        }

        foreach (EquipmentAnchor anchor in Enum.GetValues<EquipmentAnchor>())
            if (anchor.CanAccept(item) && AvailableSlots(anchor) >= item.SlotCount)
                return AcquireCheck.Allow;

        foreach (var container in GetAllItems().OfType<IContainer>())
            if (container.CanContain(item) && item.SlotCount <= container.AvailableSlots)
                return AcquireCheck.Allow;

        return AcquireCheck.Deny($"I have nowhere left to put {item.WithArticle()}");
    }

    /// <summary>Boolean form of <see cref="CanAcquire"/>, for callers that do not need the reason.</summary>
    public bool CanAcquireItem(Item item) => CanAcquire(item).Ok;

    /// <summary>
    /// Remove a specific item from wherever it is held (overflow, anchor slot, or container).
    /// Returns true when the item was found and removed.
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (Inventory.Remove(item)) return true;
        foreach (var list in EquippedItems.Values)
            if (list.Remove(item)) return true;

        // Descend into containers, however deeply nested. Test against IContainer, never
        // ContainerItem: a backpack is a WearableContainerItem and shares no base class with a
        // loose jug, so matching on the class silently skips everything the player is wearing.
        foreach (var list in EquippedItems.Values)
            foreach (var equipped in list)
                if (equipped is IContainer c && RemoveFromContainer(c, item)) return true;
        foreach (var held in Inventory)
            if (held is IContainer c && RemoveFromContainer(c, item)) return true;
        return false;
    }

    /// <summary>Removes <paramref name="item"/> from a container or any container nested inside it.</summary>
    private static bool RemoveFromContainer(IContainer container, Item item)
    {
        if (container.TryRemove(item)) return true;
        foreach (var inner in container.Contents)
            if (inner is IContainer nested && RemoveFromContainer(nested, item)) return true;
        return false;
    }

    /// <summary>
    /// Returns all items currently held: overflow inventory, every anchor slot, and the contents
    /// of every container, however deeply nested (a flask inside a backpack counts).
    /// </summary>
    public List<Item> GetAllItems()
    {
        var result = new List<Item>(Inventory);
        foreach (var item in Inventory)
            if (item is IContainer nested) CollectContents(nested, result);

        foreach (var list in EquippedItems.Values)
        {
            foreach (var item in list)
            {
                result.Add(item);
                if (item is IContainer c) CollectContents(c, result);
            }
        }
        return result;
    }

    /// <summary>
    /// Appends a container's contents to <paramref name="into"/>, descending into nested containers.
    /// Guards against a container that (incorrectly) ends up holding itself.
    /// </summary>
    private static void CollectContents(IContainer container, List<Item> into)
    {
        foreach (var item in container.Contents)
        {
            if (ReferenceEquals(item, container)) continue;
            into.Add(item);
            if (item is IContainer inner) CollectContents(inner, into);
        }
    }

    // ── Body initialisation ──────────────────────────────────────
    private void ApplySpeciesMaxScores(Species species)
    {
        foreach (var kv in species.OrganPartMaxScores)
        {
            var part = GetOrganPartById(kv.Key);
            part?.SetSpeciesMaxScore(kv.Value);
        }
    }

    private void RandomizeOrganScores()
    {
        foreach (var bp in _bodyParts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    part.Score = 1;
    }

    private HumorQueueSet InitializeHumorQueues()
    {
        var queues = new HumorQueueSet();
        queues.Initialize(this, _sharedRng);
        return queues;
    }

    /// <summary>
    /// Re-fills all four humor queues using the current organ scores, following the
    /// protagonist-creation rules: secrete only Blood / Phlegm / Yellow Bile (no Black Bile),
    /// then seed Juvenescence into a random <c>youthfulness</c>% of the slots.
    /// Call this after the player has finished setting scores in the creation screen.
    /// </summary>
    public void ReinitializeHumorQueues()
    {
        int youthfulness = DerivedStats
            .FirstOrDefault(s => s.Name == "youthfulness")?.GetValue(this) ?? 0;
        HumorQueues.InitializeForCreation(this, _sharedRng, youthfulness);
    }



    // ── ModusMentis initialisation ─────────────────────────────────────
    /// <summary>
    /// Populate modiMentis with a random selection from the registry and randomise their levels.
    /// </summary>
    public void InitializeModiMentis(ModusMentisRegistry registry, int modusMentisCount = 50)
    {
        var rng = GameRng.Stream("party-modimentis");
        var obs = registry.GetObservationModiMentis().OrderBy(_ => rng.Next()).Take(10);
        var think = registry.GetThinkingModiMentis().OrderBy(_ => rng.Next()).Take(20);
        var act = registry.GetActionModiMentis().OrderBy(_ => rng.Next()).Take(20);
        var selected = obs.Concat(think).Concat(act).Distinct().Take(modusMentisCount).ToList();

        ModiMentis.Clear();
        foreach (var template in selected)
        {
            var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
            instance.Level = 1;
            instance.CurrentXp = 0;
            ModiMentis.Add(instance);
        }
    }

    // ── Memory initialisation ─────────────────────────────────────
    /// <summary>
    /// Build the five MemoryModules from the party member's brain-organ derived stats.
    /// Call this after <see cref="InitializeModiMentis"/> so organ scores are already randomised.
    /// </summary>
    public void InitializeMemory()
    {
        int WorkingCap   = GetMemoryStat("working_memory_capacity");
        int ProceduralCap= GetMemoryStat("procedural_memory_capacity");
        int SemanticCap  = GetMemoryStat("semantic_memory_capacity");
        int SensoryCap   = GetMemoryStat("sensory_memory_capacity");
        int ResidualCap  = GetMemoryStat("residual_memory_capacity");

        MemoryModules = new List<MemoryModule>
        {
            new MemoryModule(MemoryModuleType.Working,    WorkingCap, maxCapacity: 25),
            new MemoryModule(MemoryModuleType.Procedural, ProceduralCap),
            new MemoryModule(MemoryModuleType.Semantic,   SemanticCap),
            new MemoryModule(MemoryModuleType.Sensory,    SensoryCap),
            new MemoryModule(MemoryModuleType.Residual,   ResidualCap),
        };
    }

    private int GetMemoryStat(string name)
        => DerivedStats.First(s => s.Name == name).GetValue(this);

    /// <summary>
    /// Randomly distribute modiMentis across compatible memory modules for testing.
    /// Each modusMentis is placed in the first module that accepts it and still has space.
    /// Preference order: typed long-term module first, then Working, then skip.
    /// </summary>
    public void AssignModiMentisToMemoryRandom()
    {
        if (MemoryModules.Count == 0) InitializeMemory();

        var rng = GameRng.Stream("party-memory-assign");
        // Shuffle modiMentis before assigning to get a varied distribution
        var shuffled = ModiMentis.OrderBy(_ => rng.Next()).ToList();

        foreach (var modusMentis in shuffled)
        {
            // Try the matching long-term module first, then Working as fallback
            var candidates = MemoryModules
                .Where(m => m.Type != MemoryModuleType.Residual)
                .OrderBy(m => m.Type == MemoryModuleType.Working ? 1 : 0) // prefer typed module
                .ToList();

            foreach (var module in candidates)
            {
                if (module.TryAdd(modusMentis)) break;
            }
        }
    }

    /// <summary>
    /// Game-driven entrypoint for acquiring a new modusMentis at runtime.
    /// The modusMentis is registered in <see cref="ModiMentis"/> and prepended to working memory
    /// so it is immediately active. If working memory is full, the oldest modusMentis is evicted,
    /// removed from <see cref="ModiMentis"/>, and returned so the caller can decide its fate
    /// (e.g. discard, log, or push to Residual).
    /// </summary>
    public ModusMentis? LearnModusMentis(ModusMentis modusMentis)
    {
        if (MemoryModules.Count == 0) InitializeMemory();
        var working = GetMemoryModule(MemoryModuleType.Working)
            ?? throw new InvalidOperationException("Working memory module is missing.");

        working.Compact();
        var dropped = working.Prepend(modusMentis);

        if (!ModiMentis.Contains(modusMentis))
            ModiMentis.Add(modusMentis);
        if (dropped != null)
            ModiMentis.Remove(dropped);

        return dropped;
    }

    /// <summary>
    /// Acquires a new modusMentis following the placement procedure used by the childhood
    /// reminescence phase (and any future skill-grant flow):
    ///   1. Try the long-term module matching <see cref="ModusMentis.MemoryType"/>
    ///      (Procedural / Semantic / Sensory). If it has free capacity, place there.
    ///   2. Otherwise try Working memory if it has a free slot.
    ///   3. Otherwise the last entry of Working is evicted into Residual (which itself
    ///      drops its last entry if full), and the new MM is placed at the front of Working.
    /// The new MM is also registered in <see cref="ModiMentis"/>; any MM removed from
    /// Residual is unlinked. Returns the MM permanently dropped from the protagonist (or null).
    /// </summary>
    public ModusMentis? AcquireModusMentis(ModusMentis modusMentis)
    {
        if (MemoryModules.Count == 0) InitializeMemory();

        if (!ModiMentis.Contains(modusMentis))
            ModiMentis.Add(modusMentis);
        if (modusMentis.Level <= 0) modusMentis.Level = 1;

        // Step 1: try the typed long-term module.
        var typedType = ToMemoryModuleType(modusMentis.MemoryType);
        var typedModule = typedType.HasValue ? GetMemoryModule(typedType.Value) : null;
        if (typedModule != null && typedModule.TryAdd(modusMentis))
            return null;

        // Step 2: working memory, if it has a free slot.
        var working = GetMemoryModule(MemoryModuleType.Working);
        if (working != null && working.TryAdd(modusMentis))
            return null;

        // Step 3: prepend into Working; the displaced last MM (if any) is moved to Residual.
        // Working is full (TryAdd returned false), so Prepend will drop its last MM.
        ModusMentis? permanentlyDropped = null;
        if (working == null)
            throw new InvalidOperationException("Working memory module is missing.");

        var displaced = working.Prepend(modusMentis);
        var residual = GetMemoryModule(MemoryModuleType.Residual);

        if (displaced != null && residual != null)
        {
            var droppedFromResidual = residual.Prepend(displaced);
            if (droppedFromResidual != null)
            {
                permanentlyDropped = droppedFromResidual;
                ModiMentis.Remove(droppedFromResidual);
            }
        }
        else if (displaced != null)
        {
            permanentlyDropped = displaced;
            ModiMentis.Remove(displaced);
        }

        return permanentlyDropped;
    }

    /// <summary>
    /// Removes a modusMentis from the member entirely: unlinks it from <see cref="ModiMentis"/> and
    /// from every memory module it may occupy. Used when one MM is swapped out for another (e.g. the
    /// childhood reminescence MM being replaced once the childhood phase ends). Returns true if it
    /// was present in <see cref="ModiMentis"/>.
    /// </summary>
    public bool RemoveModusMentis(ModusMentis modusMentis)
    {
        bool removed = ModiMentis.Remove(modusMentis);
        foreach (var module in MemoryModules)
            module.Remove(modusMentis);
        return removed;
    }

    private static MemoryModuleType? ToMemoryModuleType(Cathedral.Game.Narrative.Memory.ModusMentisMemoryType t)
        => t switch
        {
            Cathedral.Game.Narrative.Memory.ModusMentisMemoryType.Procedural => MemoryModuleType.Procedural,
            Cathedral.Game.Narrative.Memory.ModusMentisMemoryType.Semantic   => MemoryModuleType.Semantic,
            Cathedral.Game.Narrative.Memory.ModusMentisMemoryType.Sensory    => MemoryModuleType.Sensory,
            _ => null
        };

    /// <summary>
    /// Tries to place an item into equipment first (preferred anchor → any compatible anchor),
    /// then into any equipped container, then as a held item in <see cref="EquipmentAnchor.RightHold"/>
    /// or <see cref="EquipmentAnchor.LeftHold"/>. Falls back to overflow inventory if all fail.
    /// Mirrors the childhood-reminescence acquisition procedure (wear &gt; container &gt; hold &gt; skip).
    /// </summary>
    public bool AcquireItem(Item item) => TryAcquireItem(item);

    /// <summary>
    /// Moves a modusMentis from working memory into a long-term module (Procedural, Semantic, or Sensory).
    /// Compacts working memory after removal so the FIFO queue stays dense and eviction order is correct.
    /// Returns false if the modusMentis is not currently in working memory or the target module rejects it
    /// (incompatible type or full), in which case the modusMentis is put back at the front of working memory.
    /// </summary>
    public bool ConsolidateModusMentis(ModusMentis modusMentis, MemoryModuleType targetType)
    {
        var working = GetMemoryModule(MemoryModuleType.Working);
        if (working == null || !working.Remove(modusMentis)) return false;

        var target = GetMemoryModule(targetType);
        if (target == null || !target.TryAdd(modusMentis))
        {
            working.Compact();
            working.Prepend(modusMentis);
            return false;
        }

        working.Compact();
        return true;
    }

    /// <summary>
    /// Discards a modusMentis still sitting in working memory: it is unlinked from
    /// <see cref="ModiMentis"/> (so it stops being usable) and its slot is freed. Working memory is
    /// compacted afterwards, so the freed slot ends up at the *back* of the FIFO queue rather than
    /// as a hole in the middle — that is what makes rejection a way to bank space against the next
    /// eviction, and it keeps <see cref="MemoryModule.Prepend"/>'s shift from silently swallowing
    /// the last entry.
    /// Returns false if the modusMentis is not in working memory.
    /// </summary>
    public bool RejectModusMentis(ModusMentis modusMentis)
    {
        var working = GetMemoryModule(MemoryModuleType.Working);
        if (working == null || !working.Remove(modusMentis)) return false;

        working.Compact();
        ModiMentis.Remove(modusMentis);
        return true;
    }

    /// <summary>Get a memory module by type.</summary>
    public MemoryModule? GetMemoryModule(MemoryModuleType type) =>
        MemoryModules.FirstOrDefault(m => m.Type == type);

    // ── ModusMentis queries ────────────────────────────────────────────
    public List<ModusMentis> GetObservationModiMentis() =>
        LearnedModiMentis.Where(s => s.Functions.Contains(ModusMentisFunction.Observation)).ToList();
    public List<ModusMentis> GetThinkingModiMentis() =>
        LearnedModiMentis.Where(s => s.Functions.Contains(ModusMentisFunction.Thinking)).ToList();
    public List<ModusMentis> GetActionModiMentis() =>
        LearnedModiMentis.Where(s => s.Functions.Contains(ModusMentisFunction.Action)).ToList();
    public List<ModusMentis> GetSpeakingModiMentis() =>
        LearnedModiMentis.Where(s => s.Functions.Contains(ModusMentisFunction.Speaking)).ToList();
    public ModusMentis? GetModusMentisById(string modusMentisId) =>
        LearnedModiMentis.FirstOrDefault(s => s.ModusMentisId == modusMentisId);

    // ── Body hierarchy queries ───────────────────────────────────
    public BodyPart? GetBodyPartById(string id) =>
        _bodyParts.FirstOrDefault(bp => bp.Id == id);
    public Organ? GetOrganById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).FirstOrDefault(o => o.Id == id);
    public OrganPart? GetOrganPartById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts).FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// The anatomy sources a modusMentis draws on, each with the max-level contribution it currently
    /// grants <i>this</i> body — e.g. <c>("Eyes", +2, region: false), ("Cerebrum", +1, false)</c> or
    /// <c>("Trunk", +4, true)</c>. Order follows <see cref="ModusMentis.Organs"/>, but no entry is
    /// "primary": every one counts equally in <see cref="GetMaxLevelForModusMentis"/>, so the sum of
    /// the contributions here is exactly that method's result minus its floor of 1.
    /// A source absent from this anatomy (a beast organ on a human body) is returned under its raw
    /// id with a contribution of 0, which is what it actually grants.
    /// </summary>
    public List<(string Label, int Contribution, bool IsRegion)> GetAnatomySourcesForModusMentis(
        ModusMentis modusMentis) =>
        modusMentis.Organs.Select(id =>
        {
            int contribution = GetMaxLevelContribution(id);
            var organ = GetOrganById(id);
            if (organ != null) return (organ.DisplayName, contribution, false);
            var region = GetBodyPartById(id);
            return region != null ? (region.DisplayName, contribution, true) : (id, 0, false);
        }).ToList();

    // ── ModusMentis XP / leveling ──────────────────────────────────────

    /// <summary>
    /// Maximum level a modusMentis can reach: a floor of 1 plus the max-level contribution of every
    /// organ / body region it is related to (its <see cref="ModusMentis.Organs"/> entries, which may
    /// name organs or body regions). Each contribution comes from that source's
    /// <see cref="IMaxLevelContributionStat"/> — an organ adds +0..+3, a region +0..+6 — and already
    /// accounts for wounds (a disabled source contributes +0).
    /// </summary>
    public int GetMaxLevelForModusMentis(ModusMentis modusMentis) =>
        1 + modusMentis.Organs.Sum(GetMaxLevelContribution);

    /// <summary>Max-level contribution of a single organ or body-region id (0 if none/absent).</summary>
    private int GetMaxLevelContribution(string id) =>
        DerivedStats.FirstOrDefault(s => s is IMaxLevelContributionStat
                                      && (s.RelatedOrganId == id || s.RelatedBodyPartId == id))
                    ?.GetValue(this) ?? 0;

    /// <summary>XP a modusMentis needs to gain one level (pineal-gland-derived stat, 6-12).</summary>
    public int GetModusMentisXpThreshold() =>
        DerivedStats.First(s => s.Name == "modus_mentis_xp_threshold").GetValue(this);

    /// <summary>
    /// Awards XP to a modusMentis. When CurrentXp reaches the pineal threshold the bar resets to 0
    /// and the level increments. No-op once the modusMentis has reached its max level.
    ///
    /// <para>Returns what the award came to, so callers can tell the player without re-deriving any
    /// of these rules — see <see cref="ModusMentisXpAward"/>. A caller with nothing to show may
    /// ignore the return; one that shows something must use it rather than reading level and bar
    /// itself, so every message about experience is worded in one place.</para>
    /// </summary>
    public ModusMentisXpAward AwardModusMentisXp(ModusMentis modusMentis, int amount = 1)
    {
        int maxLevel = GetMaxLevelForModusMentis(modusMentis);
        if (modusMentis.Level >= maxLevel)                   // capped — no XP
            return ModusMentisXpAward.None(modusMentis);

        int threshold   = GetModusMentisXpThreshold();
        int levelBefore = modusMentis.Level;

        modusMentis.CurrentXp += amount;
        if (modusMentis.CurrentXp >= threshold)
        {
            modusMentis.CurrentXp = 0;                       // reset bar
            modusMentis.Level++;
            if (modusMentis.Level >= maxLevel) modusMentis.CurrentXp = 0;
        }

        return new ModusMentisXpAward(modusMentis, Landed: true,
            Levelled: modusMentis.Level > levelBefore,
            modusMentis.Level, modusMentis.CurrentXp, threshold);
    }

    // ── Wound helpers ─────────────────────────────────────────────

    /// <summary>
    /// Return all wounds that affect the given organ part (directly or via its organ/body part).
    /// </summary>
    public List<WoundInstance> GetWoundsForOrganPart(string organPartId, string organId, string bodyPartId) =>
        Wounds.Where(w => w.AffectsOrganPart(organPartId, organId, bodyPartId)).ToList();

    /// <summary>
    /// Return all wounds that affect any organ part belonging to the given body part.
    /// </summary>
    public List<WoundInstance> GetWoundsForBodyPart(string bodyPartId) =>
        Wounds.Where(w => w.AffectsBodyPart(bodyPartId)
                       || _bodyParts.FirstOrDefault(bp => bp.Id == bodyPartId)?.Organs
                              .Any(o => w.AffectsOrgan(o.Id, bodyPartId)) == true
                       || _bodyParts.FirstOrDefault(bp => bp.Id == bodyPartId)?.Organs
                              .SelectMany(o => o.Parts).Any(p => w.AffectsOrganPart(p.Id, p.Id, bodyPartId)) == true)
        .Distinct().ToList();

    /// <summary>
    /// Effective score of an organ part after applying wounds.
    /// High-handicap wounds disable the part (returns int.MinValue to signal disabled).
    /// Medium-handicap wounds apply −1 each. Low (wildcard) wounds have no organ effect.
    /// </summary>
    public int GetEffectiveScore(OrganPart part, Organ organ, BodyPart bp)
    {
        var wounds = GetWoundsForOrganPart(part.Id, organ.Id, bp.Id);
        if (wounds.Any(w => w.Handicap == WoundHandicap.High))
            return int.MinValue; // disabled
        int penalty = wounds.Count(w => w.Handicap == WoundHandicap.Medium);
        return part.Score - penalty;
    }

    /// <summary>
    /// Returns true if any wound fully disables the given organ part (or its parent organ/body part).
    /// </summary>
    public bool IsOrganPartDisabled(string organPartId, string organId, string bodyPartId) =>
        Wounds.Any(w => w.AffectsOrganPart(organPartId, organId, bodyPartId)
                     && w.Handicap == WoundHandicap.High);

    // ── Healing ───────────────────────────────────────────────────

    /// <summary>
    /// Days this body needs to close a wound it can close, from the <c>wound_healing</c> stat.
    /// Wound-aware, so an injured viscera lengthens the recovery of everything else.
    /// </summary>
    public int WoundHealDurationDays
    {
        get
        {
            var stat = DerivedStats.FirstOrDefault(s => s.Name == "wound_healing");
            return stat?.GetValue(this) ?? WoundHealingStat.SlowestHealDays;
        }
    }

    /// <summary>
    /// Remove every wound that has had time to close, and report what closed.
    ///
    /// <para>
    /// Only Low and Medium wounds ever heal, and only ones actually suffered during the run —
    /// <see cref="WoundInstance.CanHeal"/> is the single place that rule lives. Healing restores HP
    /// for free, since <see cref="CurrentHp"/> is <see cref="MaxHp"/> minus the wound count.
    /// </para>
    ///
    /// <para>
    /// Call this when the clock has moved (see <see cref="GameClock"/>), not every frame: nothing
    /// changes between advances, because progress is measured against <see cref="GameClock.Days"/>
    /// rather than ticked.
    /// </para>
    /// </summary>
    public List<WoundInstance> HealWounds()
    {
        int duration = WoundHealDurationDays;
        var closed = Wounds.Where(w => w.CanHeal && w.DaysOld >= duration).ToList();
        foreach (var w in closed) Wounds.Remove(w);
        return closed;
    }

    /// <summary>
    /// Maximum HP = trunk body part score, defined by <see cref="HealthPointStat"/>.
    /// Uses the raw source score (not <c>GetValue</c>) so wounds are not double-counted —
    /// they are subtracted once in <see cref="CurrentHp"/>.
    /// </summary>
    public int MaxHp
    {
        get
        {
            var stat = DerivedStats.FirstOrDefault(s => s.Name == "health_point");
            return stat != null
                ? stat.GetRawValue(this)
                : _bodyParts.FirstOrDefault(bp => bp.Id == "trunk")?.Score ?? 0;
        }
    }

    /// <summary>Current HP = max HP minus total wound count (all severities cost 1 HP).</summary>
    public int CurrentHp => Math.Max(0, MaxHp - Wounds.Count);

    /// <summary>
    /// Maximum noetic points = encephalon body part score, defined by <see cref="NoeticPointsStat"/>.
    /// This is the per-node pool of thinking attempts (one per encephalon level).
    /// </summary>
    public int MaxNoeticPoints
    {
        get
        {
            var stat = DerivedStats.FirstOrDefault(s => s.Name == "noetic_points");
            return stat != null
                ? stat.GetRawValue(this)
                : Math.Max(1, _bodyParts.FirstOrDefault(bp => bp.Id == "encephalon")?.Score ?? 1);
        }
    }

}
