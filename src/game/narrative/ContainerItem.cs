using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A container that is <b>not</b> worn — a jug on a shelf, a crate, a wicker basket set down.
/// Worn containers (backpacks, belt canteens) are <see cref="WearableContainerItem"/> instead,
/// because they must occupy an equipment anchor; both share the rules in
/// <see cref="ContainerRules"/> so the two can never drift apart.
///
/// A <see cref="ContainerKind.Vessel"/> takes liquids and only one kind at a time; a
/// <see cref="ContainerKind.Storage"/> takes everything except liquids.
/// </summary>
public abstract class ContainerItem : Item, IContainer
{
    /// <inheritdoc/>
    public abstract ContainerKind Kind { get; }

    /// <summary>Total slot capacity of this container.</summary>
    public abstract int ContentSlots { get; }

    /// <summary>Items currently stored in this container.</summary>
    public List<Item> Contents { get; } = new();

    /// <summary>Slots currently consumed by stored items.</summary>
    public int UsedSlots => ContainerRules.UsedSlots(this);

    /// <summary>Remaining free slots.</summary>
    public int AvailableSlots => ContentSlots - UsedSlots;

    /// <summary>Returns true when <paramref name="item"/> is allowed inside this container.</summary>
    public virtual bool CanContain(Item item) => ContainerRules.DefaultCanContain(this, item);

    /// <summary>Attempt to add <paramref name="item"/> to the container. Returns false when refused.</summary>
    public bool TryAdd(Item item) => ContainerRules.DefaultTryAdd(this, item);

    /// <summary>Remove <paramref name="item"/> from the container. Returns false when not present.</summary>
    public bool TryRemove(Item item) => Contents.Remove(item);

    public override ItemCategory Category       => ItemCategory.Container;
    public override string       SubcategoryKey => Kind.ToString();
}
