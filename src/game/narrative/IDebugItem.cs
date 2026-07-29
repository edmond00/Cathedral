namespace Cathedral.Game.Narrative;

/// <summary>
/// Marks an item as a development fixture rather than world content.
///
/// <see cref="ItemRegistry"/> discovers items by reflection, which means anything with a public
/// parameterless constructor is a live item — including the inventory test fixtures. Those were
/// reaching NPC trade catalogues and duplicating real items' display names ("Iron Dagger",
/// "Leather Boots" each existed twice, one real and one debug). Implementing this keeps a fixture
/// instantiable from test/debug code while excluding it from the registry, and therefore from
/// trade, loot and every audit count.
/// </summary>
public interface IDebugItem
{
}
