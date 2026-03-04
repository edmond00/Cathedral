using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.Memory;

/// <summary>
/// Identifies which of the five memory modules a slot belongs to.
/// </summary>
public enum MemoryModuleType
{
    /// <summary>Short-term buffer for any skill. Sized by the Encephalon stat.</summary>
    Working,

    /// <summary>Long-term home for motor/physical skills (SkillMemoryType.Procedural). Sized by Cerebellum stat.</summary>
    Procedural,

    /// <summary>Long-term home for conceptual skills (SkillMemoryType.Semantic). Sized by Cerebrum stat.</summary>
    Semantic,

    /// <summary>Long-term home for perceptual skills (SkillMemoryType.Sensory). Sized by Hippocampus stat.</summary>
    Sensory,

    /// <summary>FIFO queue for skills queued for forgetting. Sized by Anamnesis stat.</summary>
    Residual
}

/// <summary>
/// A single memory slot. Holds at most one skill at a time.
/// </summary>
public class MemorySlot
{
    /// <summary>The skill occupying this slot, or null if empty.</summary>
    public Skill? Skill { get; set; }

    /// <summary>True when a skill is present.</summary>
    public bool IsFilled => Skill != null;

    /// <summary>
    /// True when this slot is permanently unavailable due to brain damage or other conditions.
    /// Reserved for future use — blocked slots are rendered differently in the UI.
    /// </summary>
    public bool IsBlocked { get; set; }
}

/// <summary>
/// One of the five memory modules belonging to a party member.
/// Each module has a fixed capacity determined by the governing derived stat.
/// </summary>
public class MemoryModule
{
    public MemoryModuleType Type { get; }

    /// <summary>Maximum number of skill slots in this module.</summary>
    public int Capacity => Slots.Count;

    /// <summary>All slots in this module, always exactly <see cref="Capacity"/> entries.</summary>
    public List<MemorySlot> Slots { get; }

    /// <summary>Skills currently held in this module.</summary>
    public IEnumerable<Skill> FilledSkills => Slots.Where(s => s.IsFilled).Select(s => s.Skill!);

    /// <summary>Number of occupied (non-blocked) slots.</summary>
    public int FilledCount => Slots.Count(s => s.IsFilled);

    /// <summary>Whether this module uses FIFO ordering (Residual module).</summary>
    public bool IsResidualQueue => Type == MemoryModuleType.Residual;

    public MemoryModule(MemoryModuleType type, int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Type = type;
        Slots = Enumerable.Range(0, capacity).Select(_ => new MemorySlot()).ToList();
    }

    /// <summary>
    /// Returns true if <paramref name="skill"/> is compatible with this module.
    /// Working and Residual accept any skill; typed modules match on SkillMemoryType.
    /// </summary>
    public bool AcceptsSkill(Skill skill)
    {
        if (skill == null) return false;
        return Type switch
        {
            MemoryModuleType.Working  => true,
            MemoryModuleType.Residual => true,
            MemoryModuleType.Procedural => skill.MemoryType == SkillMemoryType.Procedural,
            MemoryModuleType.Semantic   => skill.MemoryType == SkillMemoryType.Semantic,
            MemoryModuleType.Sensory    => skill.MemoryType == SkillMemoryType.Sensory,
            _ => false
        };
    }

    /// <summary>
    /// Attempt to place <paramref name="skill"/> into the first available (empty, non-blocked) slot.
    /// Returns true on success, false if full or incompatible.
    /// </summary>
    public bool TryAdd(Skill skill)
    {
        if (!AcceptsSkill(skill)) return false;
        var free = Slots.FirstOrDefault(s => !s.IsFilled && !s.IsBlocked);
        if (free == null) return false;
        free.Skill = skill;
        return true;
    }

    /// <summary>Removes <paramref name="skill"/> from this module. Returns true if found.</summary>
    public bool Remove(Skill skill)
    {
        var slot = Slots.FirstOrDefault(s => s.Skill == skill);
        if (slot == null) return false;
        slot.Skill = null;
        return true;
    }

    /// <summary>Remove and return the skill in the oldest slot (index 0) for the FIFO queue.</summary>
    public Skill? DequeueOldest()
    {
        var slot = Slots.FirstOrDefault(s => s.IsFilled);
        if (slot == null) return null;
        var skill = slot.Skill;
        slot.Skill = null;
        return skill;
    }
}
