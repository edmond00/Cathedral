using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>What a container is built to hold — the subcategory of <see cref="ItemCategory.Container"/>.</summary>
public enum ContainerKind
{
    /// <summary>Holds liquids and nothing else: a bottle, a canteen, a jug.</summary>
    Vessel,

    /// <summary>Holds anything except liquids: a backpack, a sack, a crate.</summary>
    Storage,
}

/// <summary>
/// The ability to hold other items, expressed as an interface rather than a base class because it
/// is orthogonal to <see cref="ItemCategory"/>. A backpack is worn (so it is
/// <see cref="ItemCategory.Wearing"/>, occupying the Outerwear anchor) yet it stores; a jug on a
/// table stores without being worn. Both need the same containment logic, and neither should have
/// to inherit the other's category.
///
/// Implemented by <see cref="ContainerItem"/> (not worn) and <see cref="WearableContainerItem"/>
/// (worn); both delegate to <see cref="ContainerRules.DefaultCanContain"/> so the rules live once.
/// </summary>
public interface IContainer
{
    /// <summary>Whether this holds liquids or solids — the two are never mixed.</summary>
    ContainerKind Kind { get; }

    /// <summary>Total slot capacity.</summary>
    int ContentSlots { get; }

    /// <summary>Items currently held.</summary>
    List<Item> Contents { get; }

    /// <summary>Slots consumed by current contents.</summary>
    int UsedSlots { get; }

    /// <summary>Remaining free slots.</summary>
    int AvailableSlots { get; }

    /// <summary>Whether <paramref name="item"/> is allowed inside.</summary>
    bool CanContain(Item item);

    /// <summary>Adds <paramref name="item"/>; false when refused or when there is no room.</summary>
    bool TryAdd(Item item);

    /// <summary>Removes <paramref name="item"/>; false when it is not present.</summary>
    bool TryRemove(Item item);
}

/// <summary>
/// The containment rules, in one place so a worn container and a loose one cannot drift apart.
/// </summary>
public static class ContainerRules
{
    /// <summary>
    /// A vessel takes any liquid, but only one <em>kind</em> at a time — water and wine never share
    /// a bottle, though several draughts of the same wine may. A storage container takes anything
    /// that is not a liquid.
    /// </summary>
    public static bool DefaultCanContain(IContainer container, Item item)
    {
        if (item.IsLiquid)
            return container.Kind == ContainerKind.Vessel
                && (container.Contents.Count == 0 || container.Contents[0].ItemId == item.ItemId);

        return container.Kind == ContainerKind.Storage;
    }

    /// <summary>Shared <c>TryAdd</c>: refuse on kind, then on capacity.</summary>
    public static bool DefaultTryAdd(IContainer container, Item item)
    {
        if (!container.CanContain(item)) return false;
        if (item.SlotCount > container.AvailableSlots) return false;
        container.Contents.Add(item);
        return true;
    }

    /// <summary>Slots consumed by a container's current contents.</summary>
    public static int UsedSlots(IContainer container) => container.Contents.Sum(i => i.SlotCount);
}
