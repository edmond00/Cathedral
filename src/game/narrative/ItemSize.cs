using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Physical size of an item, which determines how many inventory slots it occupies.
/// </summary>
public enum ItemSize
{
    Small  = 3,   // 3 slots
    Medium = 5,   // 5 slots
    Large  = 7,   // 7 slots
}

/// <summary>
/// Category tags for items — each mirrors a specific equipment anchor.
/// Items with anchor-matching types (Headgear, Footwear, etc.) can only be placed
/// on that anchor.  "Other" items can only go in general containers (holds, backpacks).
/// "Liquid" items can only go in bottles.
/// </summary>
public enum ItemType
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
    Other,
    Liquid,
}

public static class ItemSizeExtensions
{
    /// <summary>Number of inventory slots this size occupies.</summary>
    public static int SlotCount(this ItemSize size) => (int)size;
}
