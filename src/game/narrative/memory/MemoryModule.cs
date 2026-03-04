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

    /// <summary>
    /// True when this slot exists in the maximum pool but is not yet unlocked
    /// (organ score too low to reach this index). Rendered as greyed-out in the UI.
    /// </summary>
    public bool IsUnusable { get; set; }
}

/// <summary>
/// One of the five memory modules belonging to a party member.
/// Each module has an <see cref="ActiveCapacity"/> (organ score × 2) usable slots
/// and a <see cref="MaxCapacity"/> total pool shown greyed-out in the UI.
/// </summary>
public class MemoryModule
{
    public MemoryModuleType Type { get; }

    /// <summary>Number of unlocked, usable slots (determined by the governing organ stat).</summary>
    public int ActiveCapacity { get; }

    /// <summary>Total slot pool size (usable + unusable). Equal to MaxCapacity constant.</summary>
    public int MaxCapacity { get; }

    /// <summary>Backward-compat alias for <see cref="ActiveCapacity"/>.</summary>
    public int Capacity => ActiveCapacity;

    /// <summary>All slots in this module (<see cref="MaxCapacity"/> entries total).
    /// Slots at index &gt;= <see cref="ActiveCapacity"/> have <see cref="MemorySlot.IsUnusable"/> = true.</summary>
    public List<MemorySlot> Slots { get; }

    /// <summary>Skills currently held in usable, non-blocked slots.</summary>
    public IEnumerable<Skill> FilledSkills =>
        Slots.Where(s => !s.IsUnusable && s.IsFilled).Select(s => s.Skill!);

    /// <summary>Number of occupied (usable, non-blocked) slots.</summary>
    public int FilledCount => Slots.Count(s => !s.IsUnusable && s.IsFilled);

    /// <summary>Whether this module uses FIFO ordering (Residual module).</summary>
    public bool IsResidualQueue => Type == MemoryModuleType.Residual;

    public MemoryModule(MemoryModuleType type, int activeCapacity, int maxCapacity = 20)
    {
        if (activeCapacity < 0) throw new ArgumentOutOfRangeException(nameof(activeCapacity));
        if (maxCapacity < activeCapacity) maxCapacity = activeCapacity;
        Type           = type;
        ActiveCapacity = activeCapacity;
        MaxCapacity    = maxCapacity;
        Slots = Enumerable.Range(0, maxCapacity)
            .Select(i => new MemorySlot { IsUnusable = i >= activeCapacity })
            .ToList();
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
        if (skill == null) return false;
        if (!AcceptsSkill(skill)) return false;
        var free = Slots.FirstOrDefault(s => !s.IsUnusable && !s.IsFilled && !s.IsBlocked);
        if (free == null) return false;
        free.Skill = skill;
        return true;
    }

    /// <summary>Removes <paramref name="skill"/> from this module. Returns true if found.</summary>
    public bool Remove(Skill skill)
    {
        var slot = Slots.FirstOrDefault(s => !s.IsUnusable && s.Skill == skill);
        if (slot == null) return false;
        slot.Skill = null;
        return true;
    }

    /// <summary>Remove and return the skill in the oldest usable slot (index 0) for the FIFO queue.</summary>
    public Skill? DequeueOldest()
    {
        var slot = Slots.FirstOrDefault(s => !s.IsUnusable && s.IsFilled);
        if (slot == null) return null;
        var skill = slot.Skill;
        slot.Skill = null;
        return skill;
    }

    /// <summary>
    /// FIFO push-front: inserts <paramref name="skill"/> at the first active slot,
    /// shifting existing skills one position toward the last slot.
    /// If all active slots are filled, the last skill is silently dropped.
    /// Returns the dropped skill, or null if there was a free slot.
    /// </summary>
    public Skill? Prepend(Skill skill)
    {
        var active = Slots.Where(s => !s.IsUnusable && !s.IsBlocked).ToList();
        if (active.Count == 0) return null;

        Skill? dropped = null;
        if (active.All(s => s.IsFilled))
            dropped = active[^1].Skill;

        // Shift right: each slot receives the skill from the slot before it
        for (int i = active.Count - 1; i > 0; i--)
            active[i].Skill = active[i - 1].Skill;
        active[0].Skill = skill;

        return dropped;
    }
}
