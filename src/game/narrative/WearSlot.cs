using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Where a garment sits on the body — the subcategory of <see cref="ItemCategory.Wearing"/>.
///
/// These are the ten wearable members of the old <c>ItemType</c>, lifted out unchanged. A slot
/// decides three things: which <see cref="EquipmentAnchor"/> the item may occupy, which body
/// section its armour protects (see <c>ArmorSections</c>), and how it is labelled in the UI.
///
/// Note the asymmetry with <see cref="EquipmentAnchor"/>: there are ten slots but thirteen
/// anchors, because hands and holds are paired. <see cref="WearSlotExtensions.AnchorsTo"/> owns
/// that mapping — never compare a slot to an anchor by name.
/// </summary>
public enum WearSlot
{
    Headgear,
    Eyewear,
    Neckwear,
    Outerwear,
    Bodywear,
    Underwear,
    BeltGear,
    Handwear,
    Legwear,
    Footwear,
}

public static class WearSlotExtensions
{
    /// <summary>
    /// The anchor an item of this slot fills when nothing else is specified. Handwear is
    /// right-handed by default; the left hand is reachable but never auto-chosen.
    /// </summary>
    public static EquipmentAnchor DefaultAnchor(this WearSlot slot) => slot switch
    {
        WearSlot.Headgear  => EquipmentAnchor.Headgear,
        WearSlot.Eyewear   => EquipmentAnchor.Eyewear,
        WearSlot.Neckwear  => EquipmentAnchor.Neckwear,
        WearSlot.Outerwear => EquipmentAnchor.Outerwear,
        WearSlot.Bodywear  => EquipmentAnchor.Bodywear,
        WearSlot.Underwear => EquipmentAnchor.Underwear,
        WearSlot.BeltGear  => EquipmentAnchor.BeltGear,
        WearSlot.Handwear  => EquipmentAnchor.RightHandwear,
        WearSlot.Legwear   => EquipmentAnchor.Legwear,
        WearSlot.Footwear  => EquipmentAnchor.Footwear,
        _                  => EquipmentAnchor.BeltGear,
    };

    /// <summary>
    /// Whether an item of this slot may be worn on <paramref name="anchor"/>. Every slot maps to
    /// exactly one anchor except Handwear, which accepts either hand — that single exception is
    /// why this is a lookup rather than a name comparison.
    /// </summary>
    public static bool AnchorsTo(this WearSlot slot, EquipmentAnchor anchor) => slot switch
    {
        WearSlot.Handwear => anchor is EquipmentAnchor.RightHandwear or EquipmentAnchor.LeftHandwear,
        _                 => anchor == slot.DefaultAnchor(),
    };

    /// <summary>Human-readable label, reusing the anchor labels so the UI stays consistent.</summary>
    public static string Label(this WearSlot slot) => slot.DefaultAnchor().Label();

    /// <summary>Every slot, for audits and UI enumeration.</summary>
    public static IReadOnlyList<WearSlot> All { get; } =
        (WearSlot[])Enum.GetValues(typeof(WearSlot));
}
