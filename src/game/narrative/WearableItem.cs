using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Something worn on the body. Beyond occupying an anchor, a garment may do up to three jobs, and
/// <b>must do at least one</b> — a wearable that neither protects, nor flatters, nor holds anything
/// is dead weight, and <c>--item-audit</c> reports it as such:
///
/// <list type="number">
///   <item><b>Protect.</b> <see cref="DefenseDice"/> are added to the defender's pool when an
///     attack targets the body section this slot covers (see <c>ArmorSections</c>). Contributions
///     from different slots accumulate — legwear and footwear both guard the lower limbs.</item>
///   <item><b>Flatter.</b> <see cref="DialogueAppeal"/> grants one bonus dialogue die per matching
///     item when speaking to an NPC of that social standing.</item>
///   <item><b>Hold.</b> Implement <see cref="IContainer"/> via <see cref="WearableContainerItem"/>.</item>
/// </list>
///
/// Both bonuses require the garment to be <i>actually worn</i> — an item sitting in a backpack or
/// held in a fist grants nothing. That test is <c>Slot.AnchorsTo(anchor)</c> against the anchor the
/// item was found on, applied identically by the armour and dialogue lookups.
/// </summary>
public abstract class WearableItem : Item
{
    /// <summary>Where on the body this is worn. Decides the anchor and the protected section.</summary>
    public abstract WearSlot Slot { get; }

    public sealed override ItemCategory Category       => ItemCategory.Wearing;
    public sealed override string       SubcategoryKey => Slot.ToString();

    /// <summary>
    /// Defaults to the slot's own anchor, which is right for every garment — the 28 wearables that
    /// used to restate this individually no longer need to.
    /// </summary>
    public override EquipmentAnchor? PreferredAnchor => Slot.DefaultAnchor();

    /// <summary>
    /// Bonus defence dice, 0–3, granted when an attack targets this slot's body section.
    ///
    /// <b>0 for ordinary clothing.</b> There is deliberately no cap in code, so values stack
    /// freely across a section — three trunk garments at 3 dice each would put nine dice on top of
    /// natural defence and make a torso practically unhittable. Reserve 1–2 for real armour and
    /// keep the <c>--item-audit</c> "max achievable armour per section" line honest.
    /// </summary>
    public virtual int DefenseDice => 0;

    /// <summary>
    /// Social standings this garment reads well to, at most three. Each grants one bonus dialogue
    /// die, and several garments appealing to the same standing accumulate — but one garment never
    /// grants more than a single die to any one standing.
    /// </summary>
    public virtual IReadOnlyList<SocialCategory> DialogueAppeal => Array.Empty<SocialCategory>();

    /// <summary>
    /// Whether this garment does anything at all beyond occupying a slot. Enforced by the audit
    /// rather than the compiler, since "holds things" is expressed by an interface.
    /// </summary>
    public bool HasFunction => DefenseDice > 0 || DialogueAppeal.Count > 0 || this is IContainer;
}

/// <summary>
/// A worn container — a backpack on the shoulders, a canteen on the belt. Wearing is the category
/// (it occupies an anchor); holding is the interface. See <see cref="IContainer"/> for why the two
/// are kept orthogonal.
/// </summary>
public abstract class WearableContainerItem : WearableItem, IContainer
{
    /// <inheritdoc/>
    public abstract ContainerKind Kind { get; }

    /// <inheritdoc/>
    public abstract int ContentSlots { get; }

    /// <inheritdoc/>
    public List<Item> Contents { get; } = new();

    /// <inheritdoc/>
    public int UsedSlots => ContainerRules.UsedSlots(this);

    /// <inheritdoc/>
    public int AvailableSlots => ContentSlots - UsedSlots;

    /// <inheritdoc/>
    public virtual bool CanContain(Item item) => ContainerRules.DefaultCanContain(this, item);

    /// <inheritdoc/>
    public bool TryAdd(Item item) => ContainerRules.DefaultTryAdd(this, item);

    /// <inheritdoc/>
    public bool TryRemove(Item item) => Contents.Remove(item);
}
