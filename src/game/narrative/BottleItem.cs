using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A container that holds only a single type of liquid.
/// Multiple instances of the same liquid are allowed, but different liquids cannot be mixed.
/// </summary>
public abstract class BottleItem : ContainerItem
{
    /// <summary>
    /// Bottles accept only Liquid items, and only one liquid type at a time.
    /// Mixing water and wine is forbidden; having multiple wine items is fine.
    /// </summary>
    public override bool CanContain(Item item)
    {
        // Must be liquid
        if (!item.Types.Contains(ItemType.Liquid)) return false;

        // If the bottle already has contents, the new item's ItemId must match
        // the existing liquid's ItemId (same liquid type, e.g. "spring_water").
        if (Contents.Count > 0 && Contents[0].ItemId != item.ItemId)
            return false;

        return true;
    }

    /// <summary>Narrative keywords for LLM interaction. Bottles use generic terms by default.</summary>
    public override List<string> OutcomeKeywords => new() { "bottle", "flask", "liquid" };

    public override List<ItemType> Types => new() { ItemType.BeltGear };
}
