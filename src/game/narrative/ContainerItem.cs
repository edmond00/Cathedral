using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for items that can hold other items (backpack, satchel, etc.).
/// Container items block liquid contents by default — override <see cref="CanContain"/> to change.
/// </summary>
public abstract class ContainerItem : Item
{
    /// <summary>Total slot capacity of this container.</summary>
    public abstract int ContentSlots { get; }

    /// <summary>Items currently stored in this container.</summary>
    public List<Item> Contents { get; } = new();

    /// <summary>Slots currently consumed by stored items.</summary>
    public int UsedSlots => Contents.Sum(i => i.SlotCount);

    /// <summary>Remaining free slots.</summary>
    public int AvailableSlots => ContentSlots - UsedSlots;

    /// <summary>
    /// Returns true when <paramref name="item"/> is allowed inside this container.
    /// Default implementation blocks liquids.
    /// </summary>
    public virtual bool CanContain(Item item) => !item.Types.Contains(ItemType.Liquid);

    /// <summary>Attempt to add <paramref name="item"/> to the container. Returns false when refused.</summary>
    public bool TryAdd(Item item)
    {
        if (!CanContain(item)) return false;
        if (item.SlotCount > AvailableSlots) return false;
        Contents.Add(item);
        return true;
    }

    /// <summary>Remove <paramref name="item"/> from the container. Returns false when not present.</summary>
    public bool TryRemove(Item item) => Contents.Remove(item);

    // ContainerItem is BeltGear type (worn on belt / back)
    public override List<ItemType> Types => new() { ItemType.BeltGear };
}
