namespace Cathedral.Game.Narrative;

/// <summary>
/// What an item fundamentally <i>is</i>, and therefore what the game may do with it.
///
/// This is the primary taxonomy: exactly one category per item, and it decides which systems
/// look at the item at all. It replaced <c>ItemType</c>, which had grown to do four unrelated
/// jobs at once (naming equipment anchors, mirroring <see cref="ConsumableType"/>, declaring
/// storage physics, and absorbing everything else under "Other").
///
/// Subcategory is deliberately <i>not</i> a second flat enum — each category names its own type,
/// so an invalid pairing cannot be expressed:
/// <list type="bullet">
///   <item><b>Weapon</b> → <c>IWeaponItem.WeaponCategory</c> (long_blade, axe, bow, …)</item>
///   <item><b>Wearing</b> → <see cref="WearSlot"/></item>
///   <item><b>Consumable</b> → <see cref="ConsumableType"/></item>
///   <item><b>Container</b> → <see cref="ContainerKind"/></item>
///   <item><b>Tool</b>, <b>Crafting</b>, <b>Other</b> → none</item>
/// </list>
/// <see cref="Item.SubcategoryKey"/> flattens whichever applies to a string, for UI and audit only.
///
/// Distinct from <see cref="ItemTag"/>, which is a free economic taxonomy used purely to build NPC
/// trade catalogues: an item has one category but any number of tags.
/// </summary>
public enum ItemCategory
{
    /// <summary>Carried to fight with: grants a fighting medium and its skills.</summary>
    Weapon,

    /// <summary>Carried to work with: grants bonus dice when combined with a narration action.</summary>
    Tool,

    /// <summary>Worn on a body anchor. May protect, may flatter, may hold things — at least one.</summary>
    Wearing,

    /// <summary>Eaten, drunk or inhaled; pushes humors into an organ queue.</summary>
    Consumable,

    /// <summary>Holds other items and is not itself worn (a jug on a table, a crate).</summary>
    Container,

    /// <summary>Raw material — ore, timber, fibre, hide. Inert until a crafting system exists.</summary>
    Crafting,

    /// <summary>Fits nothing above. A keepsake, a curiosity, a corpse part.</summary>
    Other,
}
