using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Global registry of all concrete <see cref="Item"/> types, discovered via reflection at
/// startup (same pattern as <c>VerbRegistry</c> / <c>ModusMentisRegistry</c>).
///
/// Only items with a public parameterless constructor are included — that excludes things
/// like corpse body-part drops that require constructor arguments, which is fine since those
/// are never sold in shops. One prototype instance is kept per type so trade catalogues can
/// read <see cref="Item.Tags"/>, <see cref="Item.PriceCoin"/> and <see cref="Item.PriceReference"/>.
/// </summary>
public class ItemRegistry
{
    private static ItemRegistry? _instance;

    /// <summary>Singleton instance (lazy-initialized).</summary>
    public static ItemRegistry Instance => _instance ??= new ItemRegistry();

    private readonly List<Item> _prototypes = new();

    private ItemRegistry()
    {
        var itemType = typeof(Item);
        var concreteTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(itemType) && !t.IsAbstract
                     && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in concreteTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is Item item)
                    _prototypes.Add(item);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ItemRegistry: failed to instantiate {type.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"ItemRegistry: Discovered {_prototypes.Count} item type(s).");
    }

    /// <summary>All discovered item prototypes (one instance per type).</summary>
    public IReadOnlyList<Item> All => _prototypes;

    /// <summary>Prototypes carrying the given trade tag.</summary>
    public IEnumerable<Item> WithTag(ItemTag tag) => _prototypes.Where(i => i.Tags.Contains(tag));

    /// <summary>Creates a fresh instance of the item type matching <paramref name="prototype"/>.</summary>
    public static Item NewInstance(Item prototype) =>
        (Item)Activator.CreateInstance(prototype.GetType())!;
}
