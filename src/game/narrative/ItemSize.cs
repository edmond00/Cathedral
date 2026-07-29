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

public static class ItemSizeExtensions
{
    /// <summary>Number of inventory slots this size occupies.</summary>
    public static int SlotCount(this ItemSize size) => (int)size;
}
