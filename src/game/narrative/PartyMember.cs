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
    private static readonly Random _sharedRng = new Random();

    // ── Body ─────────────────────────────────────────────────────
    private List<BodyPart> _bodyParts;

    public List<BodyPart> BodyParts => _bodyParts;
    public List<DerivedStat> DerivedStats { get; private set; }

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
    public List<Wound> Wounds { get; private set; } = new();

    // ── Species & anatomy ─────────────────────────────────────────
    /// <summary>The species of this party member (determines anatomy type and art folder).</summary>
    public Species Species { get; }

    /// <summary>Shortcut to the anatomy type used by this member's species.</summary>
    public AnatomyType AnatomyType => Species.AnatomyType;

    // ── Display name (subclasses define this differently) ────────
    /// <summary>Human-readable name shown in the party panel.</summary>
    public abstract string DisplayName { get; }

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
        Wounds = InitializeDebugWounds(factory);

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

    /// <summary>
    /// Try to place an acquired item into the best available slot:
    ///   1. Preferred anchor (if the item declares one and it is free).
    ///   2. Any free anchor that CanAccept the item.
    ///   3. Any equipped ContainerItem that CanContain + has space.
    ///   4. Falls back to <see cref="Inventory"/> overflow list.
    /// Returns true when placement succeeds (includes overflow).
    /// </summary>
    public bool TryAcquireItem(Item item)
    {
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

        // 3. Any equipped container that can take the item
        foreach (var kvp in EquippedItems)
        {
            foreach (var equipped in kvp.Value)
            {
                if (equipped is ContainerItem container && container.TryAdd(item))
                    return true;
            }
        }

        // 4. Overflow
        Console.WriteLine($"PartyMember: No free anchor/container for '{item.DisplayName}' — placed in overflow inventory.");
        Inventory.Add(item);
        return true; // acquisition itself always succeeds; caller is informed via log
    }

    /// <summary>
    /// Remove a specific item from wherever it is held (overflow, anchor slot, or container).
    /// Returns true when the item was found and removed.
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (Inventory.Remove(item)) return true;
        foreach (var list in EquippedItems.Values)
            if (list.Remove(item)) return true;
        foreach (var list in EquippedItems.Values)
            foreach (var equipped in list)
                if (equipped is ContainerItem c && c.TryRemove(item)) return true;
        return false;
    }

    /// <summary>
    /// Returns all items currently held: overflow inventory, every anchor slot,
    /// and the contents of any equipped containers.
    /// </summary>
    public List<Item> GetAllItems()
    {
        var result = new List<Item>(Inventory);
        foreach (var list in EquippedItems.Values)
        {
            foreach (var item in list)
            {
                result.Add(item);
                if (item is ContainerItem c)
                    result.AddRange(c.Contents);
            }
        }
        return result;
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
        var rng = new Random();
        foreach (var bp in _bodyParts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    part.Score = rng.Next(1, part.MaxScore + 1);
    }

    private HumorQueueSet InitializeHumorQueues()
    {
        var queues = new HumorQueueSet();
        queues.Initialize(this, _sharedRng);
        return queues;
    }

    /// <summary>
    /// Re-fills all four humor queues using the current organ scores.
    /// Call this after the player has finished setting scores in the creation screen.
    /// </summary>
    public void ReinitializeHumorQueues()
    {
        HumorQueues.Initialize(this, _sharedRng);
    }



    // ── ModusMentis initialisation ─────────────────────────────────────
    /// <summary>
    /// Populate modiMentis with a random selection from the registry and randomise their levels.
    /// </summary>
    public void InitializeModiMentis(ModusMentisRegistry registry, int modusMentisCount = 50)
    {
        var rng = new Random();
        var obs = registry.GetObservationModiMentis().OrderBy(_ => rng.Next()).Take(10);
        var think = registry.GetThinkingModiMentis().OrderBy(_ => rng.Next()).Take(20);
        var act = registry.GetActionModiMentis().OrderBy(_ => rng.Next()).Take(20);
        var selected = obs.Concat(think).Concat(act).Distinct().Take(modusMentisCount).ToList();

        ModiMentis.Clear();
        ModiMentis.AddRange(selected);
        foreach (var modusMentis in ModiMentis)
            modusMentis.Level = rng.Next(1, 11);
    }

    // ── Memory initialisation ─────────────────────────────────────
    /// <summary>
    /// Build the five MemoryModules from the party member's brain-organ derived stats.
    /// Call this after <see cref="InitializeModiMentis"/> so organ scores are already randomised.
    /// </summary>
    public void InitializeMemory()
    {
        int WorkingCap   = Math.Clamp(GetMemoryStat("working_memory_capacity"),    1, 20);
        int ProceduralCap= Math.Clamp(GetMemoryStat("procedural_memory_capacity"), 1, 20);
        int SemanticCap  = Math.Clamp(GetMemoryStat("semantic_memory_capacity"),   1, 20);
        int SensoryCap   = Math.Clamp(GetMemoryStat("sensory_memory_capacity"),    1, 20);
        int ResidualCap  = Math.Clamp(GetMemoryStat("residual_memory_capacity"),   1, 20);

        MemoryModules = new List<MemoryModule>
        {
            new MemoryModule(MemoryModuleType.Working,    WorkingCap),
            new MemoryModule(MemoryModuleType.Procedural, ProceduralCap),
            new MemoryModule(MemoryModuleType.Semantic,   SemanticCap),
            new MemoryModule(MemoryModuleType.Sensory,    SensoryCap),
            new MemoryModule(MemoryModuleType.Residual,   ResidualCap),
        };
    }

    private int GetMemoryStat(string name)
    {
        var stat = DerivedStats.FirstOrDefault(s => s.Name == name);
        if (stat == null) return 1;
        return stat.GetValue(this);
    }

    /// <summary>
    /// Randomly distribute modiMentis across compatible memory modules for testing.
    /// Each modusMentis is placed in the first module that accepts it and still has space.
    /// Preference order: typed long-term module first, then Working, then skip.
    /// </summary>
    public void AssignModiMentisToMemoryRandom()
    {
        if (MemoryModules.Count == 0) InitializeMemory();

        var rng = new Random();
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
    public ModusMentis? GetModusMentisById(string modusMentisId) =>
        LearnedModiMentis.FirstOrDefault(s => s.ModusMentisId == modusMentisId);

    // ── Body hierarchy queries ───────────────────────────────────
    public BodyPart? GetBodyPartById(string id) =>
        _bodyParts.FirstOrDefault(bp => bp.Id == id);
    public Organ? GetOrganById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).FirstOrDefault(o => o.Id == id);
    public OrganPart? GetOrganPartById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts).FirstOrDefault(p => p.Id == id);

    /// <summary>Returns the primary organ score for a modusMentis (used for modusMentis checks).</summary>
    public int GetOrganScoreForModusMentis(ModusMentis modusMentis)
    {
        if (modusMentis.Organs.Length == 0) return 0;
        return GetOrganById(modusMentis.Organs[0])?.Score ?? 0;
    }

    // ── Wound helpers ─────────────────────────────────────────────

    /// <summary>
    /// Return all wounds that affect the given organ part (directly or via its organ/body part).
    /// </summary>
    public List<Wound> GetWoundsForOrganPart(string organPartId, string organId, string bodyPartId) =>
        Wounds.Where(w => w.AffectsOrganPart(organPartId, organId, bodyPartId)).ToList();

    /// <summary>
    /// Return all wounds that affect any organ part belonging to the given body part.
    /// </summary>
    public List<Wound> GetWoundsForBodyPart(string bodyPartId) =>
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

    /// <summary>Maximum HP = trunk body part score.</summary>
    public int MaxHp =>
        _bodyParts.FirstOrDefault(bp => bp.Id == "trunk")?.Score ?? 0;

    /// <summary>Current HP = max HP minus total wound count (all severities cost 1 HP).</summary>
    public int CurrentHp => Math.Max(0, MaxHp - Wounds.Count);

    // ── Debug wound initialisation ────────────────────────────────
    private static List<Wound> InitializeDebugWounds(IAnatomyFactory factory)
    {
        var woundMap = factory.GetWoundClassMap();
        var specific = woundMap.Values
            .Where(w => w.TargetKind != WoundTargetKind.Wildcard).ToList();
        var rng = new Random();
        int count = rng.Next(2, 5);
        var result = specific.OrderBy(_ => rng.Next()).Take(count).ToList<Wound>();
        // Add 1-2 wildcard wounds with placeholder positions (will be assigned by viewer)
        var wildcards = woundMap.Values.OfType<WildcardWound>().ToList();
        if (wildcards.Count > 0)
        {
            int wc = rng.Next(1, 3);
            foreach (var template in wildcards.OrderBy(_ => rng.Next()).Take(wc))
            {
                var instance = (WildcardWound)System.Activator.CreateInstance(template.GetType())!;
                result.Add(instance);
            }
        }
        return result;
    }
}
