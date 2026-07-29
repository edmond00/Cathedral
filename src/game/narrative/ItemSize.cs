using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Physical size of an item, which determines how many inventory slots it occupies.
///
/// The three sizes are exact multiples of each other — a medium is two smalls, a large is three —
/// because every anchor capacity is a multiple of three (3, 6 or 9, see
/// <see cref="EquipmentAnchorExtensions.Capacity"/>). That makes the arithmetic come out even: a
/// nine-slot hold takes three smalls, or one small and one medium, or one large, and is exactly
/// full in each case.
///
/// They used to be 3 / 5 / 7, which broke that. A medium left four slots free — enough to look
/// like room but not enough for another small — and the inventory drew one three-row placeholder
/// in the gap, so an anchor holding a medium was visibly shorter than the same anchor empty.
/// </summary>
public enum ItemSize
{
    Small  = 3,   // 3 slots — one third of a full anchor
    Medium = 6,   // 6 slots — two smalls
    Large  = 9,   // 9 slots — three smalls, and a full anchor on its own
}

public static class ItemSizeExtensions
{
    /// <summary>Number of inventory slots this size occupies.</summary>
    public static int SlotCount(this ItemSize size) => (int)size;
}
