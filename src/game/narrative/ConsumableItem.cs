using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// How a consumable item is used. Determines which humor queue receives the
/// item's composition, and the label shown on the consume button in the UI.
/// </summary>
public enum ConsumableType
{
    /// <summary>Eaten — humors pushed to the Paunch queue.</summary>
    Food,
    /// <summary>Drunk — humors pushed to the Hepar queue.</summary>
    Drink,
    /// <summary>Inhaled — humors pushed to the Pulmones queue.</summary>
    Inhalant,
}

/// <summary>
/// Base class for items that can be consumed (eaten, drunk, or inhaled).
/// Subclasses implement <see cref="GenerateComposition"/> to return a randomised
/// but thematically coherent list of humors.
///
/// The composition is generated once (lazily) and cached for the lifetime of the
/// item instance, so two Pear items picked up separately can differ.
/// </summary>
public abstract class ConsumableItem : Item
{
    // ── Consumption type ─────────────────────────────────────────

    /// <summary>How this item is consumed (Food / Drink / Inhalant).</summary>
    public abstract ConsumableType ConsumableType { get; }

    /// <summary>Organ queue that receives the humors when consumed.</summary>
    public string TargetOrganId => ConsumableType switch
    {
        ConsumableType.Food     => "paunch",
        ConsumableType.Drink    => "hepar",
        ConsumableType.Inhalant => "pulmones",
        _                       => "paunch",
    };

    /// <summary>Label for the consume button in the inventory UI.</summary>
    public string ConsumeButtonLabel => ConsumableType switch
    {
        ConsumableType.Food     => "EAT",
        ConsumableType.Drink    => "DRINK",
        ConsumableType.Inhalant => "INHALE",
        _                       => "CONSUME",
    };

    // ── Hardness ─────────────────────────────────────────────────

    /// <summary>
    /// When true, this item requires functional teeth to consume.
    /// A character whose Teeths organ score is 0 (or the organ is disabled)
    /// cannot eat hard items.
    /// </summary>
    public virtual bool IsHard => false;

    // ── Composition ───────────────────────────────────────────────

    private List<BodyHumor>? _composition;

    /// <summary>
    /// The humor composition of this item instance.
    /// Generated once on first access using <see cref="GenerateComposition"/>.
    /// Order matters: the UI reveals humors from front to back based on Nose score.
    /// Put the most characteristic humor first.
    /// </summary>
    public List<BodyHumor> Composition
    {
        get
        {
            _composition ??= GenerateComposition(new Random());
            return _composition;
        }
    }

    /// <summary>
    /// Generate a randomised humor composition for one instance of this item.
    /// Called once per instance. Use <paramref name="rng"/> for any random choices.
    ///
    /// Guidelines:
    /// • Use <see cref="HumorCountRange"/> to determine how many humors to produce.
    /// • Focus on humors matching the item's <see cref="ConsumableType"/> category
    ///   but small cross-category inclusions are fine.
    /// • Put the most characteristic humor first (revealed first at low Nose score).
    /// </summary>
    protected abstract List<BodyHumor> GenerateComposition(Random rng);

    /// <summary>
    /// Returns (min, max) humor count based on item size:
    /// Small=(1,2), Medium=(2,3), Large=(3,5).
    /// </summary>
    protected (int min, int max) HumorCountRange => Size switch
    {
        ItemSize.Large  => (3, 5),
        ItemSize.Medium => (2, 3),
        _               => (1, 2),
    };

    /// <summary>
    /// Pick a count in [min, max] (inclusive) from <see cref="HumorCountRange"/>.
    /// </summary>
    protected int PickHumorCount(Random rng)
    {
        var (min, max) = HumorCountRange;
        return rng.Next(min, max + 1);
    }

    // ── Eligibility ───────────────────────────────────────────────

    /// <summary>
    /// Returns true when the character is able to consume this item right now.
    /// Checks teeth for hard items (calls into derived-stat system).
    /// </summary>
    public bool CanConsume(PartyMember member)
    {
        if (IsHard)
        {
            var teethStat = new TeethHardFoodStat();
            int val = teethStat.GetValue(member);
            if (val <= 0) return false;
        }
        return true;
    }

    /// <summary>
    /// Human-readable reason the character cannot consume this item, or null when they can.
    /// </summary>
    public string? GetCannotConsumeReason(PartyMember member)
    {
        if (IsHard)
        {
            var teethStat = new TeethHardFoodStat();
            if (teethStat.GetValue(member) <= 0)
                return "Requires functional teeth";
        }
        return null;
    }

    // ── Consumption ───────────────────────────────────────────────

    /// <summary>
    /// Consume this item: push all humors to the appropriate organ queue,
    /// then remove the item from the member's inventory.
    /// </summary>
    public void Consume(PartyMember member)
    {
        foreach (var humor in Composition)
            member.HumorQueues.ProduceHumor(TargetOrganId, humor);
        member.RemoveItem(this);
    }

    // ── ItemType tagging ──────────────────────────────────────────

    /// <summary>
    /// Consumable items advertise their type via <see cref="ItemType"/>.
    /// Subclasses should override Types to include the matching tag.
    /// </summary>
    public override List<ItemType> Types => ConsumableType switch
    {
        ConsumableType.Food     => new() { ItemType.Food },
        ConsumableType.Drink    => new() { ItemType.Drink },
        ConsumableType.Inhalant => new() { ItemType.Inhalant },
        _                       => new() { ItemType.Other },
    };
}
