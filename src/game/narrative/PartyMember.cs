using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for all party members (protagonist and companions).
/// Holds the shared physical and skill state: body parts, organs, humors, derived stats,
/// inventory, and the skill set used during narrative execution.
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

    // ── Skills ───────────────────────────────────────────────────
    public List<Skill> Skills { get; set; }
    /// <summary>Alias for Skills — kept for call-site compatibility.</summary>
    public List<Skill> LearnedSkills { get; set; }

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

    // ── Display name (subclasses define this differently) ────────
    /// <summary>Human-readable name shown in the party panel.</summary>
    public abstract string DisplayName { get; }

    // ── Constructor ──────────────────────────────────────────────
    protected PartyMember()
    {
        _bodyParts = InitializeBodyParts();
        HumorQueues = InitializeHumorQueues();
        DerivedStats = InitializeDerivedStats();
        Skills = new List<Skill>();
        LearnedSkills = Skills; // same reference
        Inventory = new List<Item>();
        MemoryModules = new List<MemoryModule>(); // populated after skills are assigned via InitializeMemory()

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

    // ── Body initialisation ──────────────────────────────────────
    private static List<BodyPart> InitializeBodyParts()
    {
        var rng = new Random();
        var parts = new List<BodyPart>
        {
            new EncephalonBodyPart(),
            new VisageBodyPart(),
            new TrunkBodyPart(),
            new UpperLimbsBodyPart(),
            new LowerLimbsBodyPart()
        };
        foreach (var bp in parts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    part.Score = rng.Next(1, 11);
        return parts;
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

    private static List<DerivedStat> InitializeDerivedStats() => new()
    {
        // Memory capacity stats (drive memory module slot counts)
        new WorkingMemoryCapacityStat(),    // drives Working Memory slot count
        new ProceduralMemoryCapacityStat(), // drives Procedural Memory slot count
        new SemanticMemoryCapacityStat(),   // drives Semantic Memory slot count
        new SensoryMemoryCapacityStat(),    // drives Sensory Memory slot count
        new ResidualMemoryCapacityStat(),   // drives Residual Memory slot count
        // Secretion percentage stats (displayed in Humors tab)
        new HeparBloodSecretionStat(),       new HeparPhlegmSecretionStat(),
        new HeparYellowBileSecretionStat(),  new HeparBlackBileSecretionStat(),
        new PaunchBloodSecretionStat(),      new PaunchPhlegmSecretionStat(),
        new PaunchYellowBileSecretionStat(), new PaunchBlackBileSecretionStat(),
        new PulmonesBloodSecretionStat(),    new PulmonesPhlegmSecretionStat(),
        new PulmonesYellowBileSecretionStat(),new PulmonesBlackBileSecretionStat(),
        new SpleenBloodSecretionStat(),      new SpleenPhlegmSecretionStat(),
        new SpleenYellowBileSecretionStat(), new SpleenBlackBileSecretionStat(),
    };

    // ── Skill initialisation ─────────────────────────────────────
    /// <summary>
    /// Populate skills with a random selection from the registry and randomise their levels.
    /// </summary>
    public void InitializeSkills(SkillRegistry registry, int skillCount = 50)
    {
        var rng = new Random();
        var obs = registry.GetObservationSkills().OrderBy(_ => rng.Next()).Take(10);
        var think = registry.GetThinkingSkills().OrderBy(_ => rng.Next()).Take(20);
        var act = registry.GetActionSkills().OrderBy(_ => rng.Next()).Take(20);
        var selected = obs.Concat(think).Concat(act).Distinct().Take(skillCount).ToList();

        Skills.Clear();
        Skills.AddRange(selected);
        foreach (var skill in Skills)
            skill.Level = rng.Next(1, 11);
    }

    // ── Memory initialisation ─────────────────────────────────────
    /// <summary>
    /// Build the five MemoryModules from the party member's brain-organ derived stats.
    /// Call this after <see cref="InitializeSkills"/> so organ scores are already randomised.
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
        return stat.CalculateValue(stat.GetSourceScore(this));
    }

    /// <summary>
    /// Randomly distribute skills across compatible memory modules for testing.
    /// Each skill is placed in the first module that accepts it and still has space.
    /// Preference order: typed long-term module first, then Working, then skip.
    /// </summary>
    public void AssignSkillsToMemoryRandom()
    {
        if (MemoryModules.Count == 0) InitializeMemory();

        var rng = new Random();
        // Shuffle skills before assigning to get a varied distribution
        var shuffled = Skills.OrderBy(_ => rng.Next()).ToList();

        foreach (var skill in shuffled)
        {
            // Try the matching long-term module first, then Working as fallback
            var candidates = MemoryModules
                .Where(m => m.Type != MemoryModuleType.Residual)
                .OrderBy(m => m.Type == MemoryModuleType.Working ? 1 : 0) // prefer typed module
                .ToList();

            foreach (var module in candidates)
            {
                if (module.TryAdd(skill)) break;
            }
        }
    }

    /// <summary>Get a memory module by type.</summary>
    public MemoryModule? GetMemoryModule(MemoryModuleType type) =>
        MemoryModules.FirstOrDefault(m => m.Type == type);

    // ── Skill queries ────────────────────────────────────────────
    public List<Skill> GetObservationSkills() =>
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Observation)).ToList();
    public List<Skill> GetThinkingSkills() =>
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Thinking)).ToList();
    public List<Skill> GetActionSkills() =>
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Action)).ToList();
    public Skill? GetSkillById(string skillId) =>
        LearnedSkills.FirstOrDefault(s => s.SkillId == skillId);

    // ── Body hierarchy queries ───────────────────────────────────
    public BodyPart? GetBodyPartById(string id) =>
        _bodyParts.FirstOrDefault(bp => bp.Id == id);
    public Organ? GetOrganById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).FirstOrDefault(o => o.Id == id);
    public OrganPart? GetOrganPartById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts).FirstOrDefault(p => p.Id == id);

    /// <summary>Returns the primary organ score for a skill (used for skill checks).</summary>
    public int GetOrganScoreForSkill(Skill skill)
    {
        if (skill.Organs.Length == 0) return 0;
        return GetOrganById(skill.Organs[0])?.Score ?? 0;
    }
}
