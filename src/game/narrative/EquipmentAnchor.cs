using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The 13 equipment anchor points on a party member's body.
/// Matches the entries in assets/art/body/human/gears.csv.
/// </summary>
public enum EquipmentAnchor
{
    Headgear,
    Eyewear,
    Neckwear,
    Outerwear,
    Bodywear,
    Underwear,
    BeltGear,
    RightHandwear,
    LeftHandwear,
    RightHold,
    LeftHold,
    Legwear,
    Footwear,
}

public static class EquipmentAnchorExtensions
{
    private static readonly Dictionary<EquipmentAnchor, string> Labels = new()
    {
        { EquipmentAnchor.Headgear,      "headgear"      },
        { EquipmentAnchor.Eyewear,       "eyewear"       },
        { EquipmentAnchor.Neckwear,      "neckwear"      },
        { EquipmentAnchor.Outerwear,     "outerwear"     },
        { EquipmentAnchor.Bodywear,      "bodywear"      },
        { EquipmentAnchor.Underwear,     "underwear"     },
        { EquipmentAnchor.BeltGear,      "belt gear"     },
        { EquipmentAnchor.RightHandwear, "r. handwear"   },
        { EquipmentAnchor.LeftHandwear,  "l. handwear"   },
        { EquipmentAnchor.RightHold,     "r. hold"       },
        { EquipmentAnchor.LeftHold,      "l. hold"       },
        { EquipmentAnchor.Legwear,       "legwear"       },
        { EquipmentAnchor.Footwear,      "footwear"      },
    };

    /// <summary>Display label matching the gears.csv alias.</summary>
    public static string Label(this EquipmentAnchor anchor) =>
        Labels.TryGetValue(anchor, out var label) ? label : anchor.ToString().ToLower();

    /// <summary>
    /// Maximum number of item slots this anchor can hold.
    /// Small anchors (head/eyes/neck/hands) hold 3 slots; medium (legs/feet/underwear) hold 6;
    /// large anchors (body/outer/holds/belt) hold 9.
    /// </summary>
    public static int Capacity(this EquipmentAnchor anchor) => anchor switch
    {
        EquipmentAnchor.Headgear      => 3,
        EquipmentAnchor.Eyewear       => 3,
        EquipmentAnchor.Neckwear      => 3,
        EquipmentAnchor.Outerwear     => 9,
        EquipmentAnchor.Bodywear      => 9,
        EquipmentAnchor.Underwear     => 6,
        EquipmentAnchor.BeltGear      => 9,
        EquipmentAnchor.RightHandwear => 3,
        EquipmentAnchor.LeftHandwear  => 3,
        EquipmentAnchor.RightHold     => 9,
        EquipmentAnchor.LeftHold      => 9,
        EquipmentAnchor.Legwear       => 6,
        EquipmentAnchor.Footwear      => 6,
        _                             => 3,
    };

    /// <summary>
    /// Returns false when the anchor refuses the given item.
    ///
    /// Liquids are refused everywhere: they may only ever sit inside a
    /// <see cref="ContainerKind.Vessel"/>, never on the body directly. Hold anchors (the hands)
    /// are otherwise general-purpose and take anything. Every other anchor is a garment slot and
    /// accepts only a <see cref="WearableItem"/> whose <see cref="WearableItem.Slot"/> maps to it.
    /// </summary>
    public static bool CanAccept(this EquipmentAnchor anchor, Item item)
    {
        if (item.IsLiquid) return false;

        if (anchor is EquipmentAnchor.RightHold or EquipmentAnchor.LeftHold)
            return true;

        return item is WearableItem wearable && wearable.Slot.AnchorsTo(anchor);
    }
}
