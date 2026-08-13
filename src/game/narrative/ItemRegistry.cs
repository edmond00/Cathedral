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
                     && t.GetConstructor(Type.EmptyTypes) != null
                     // Development fixtures stay instantiable from debug code but never become
                     // world content — otherwise they surface in shops and clash with real items.
                     && !typeof(IDebugItem).IsAssignableFrom(t));

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

    /// <summary>
    /// Every instantiable item type by <see cref="Item.ItemId"/> — <b>including</b> the
    /// <see cref="IDebugItem"/> fixtures that <see cref="All"/> deliberately withholds.
    ///
    /// <para>The two lists answer different questions. <see cref="All"/> is "what may the world be
    /// stocked with", and a debug fixture must never appear in a shop. This is "what can an id name",
    /// and a debug item granted with <c>--grant-item</c> is really in a pack, so a save that could not
    /// name it would quietly lose it on load.</para>
    /// </summary>
    private static Dictionary<string, Type>? _typesById;

    /// <summary>
    /// A fresh instance of the item type whose <see cref="Item.ItemId"/> is <paramref name="itemId"/>,
    /// or null when no type claims that id. Null means a save names an item this build does not have,
    /// which the caller should treat as a corrupt save rather than as an empty slot.
    /// </summary>
    public static Item? GetById(string itemId)
    {
        _typesById ??= BuildTypeIndex();
        return _typesById.TryGetValue(itemId, out var type)
            ? (Item)Activator.CreateInstance(type)!
            : null;
    }

    private static Dictionary<string, Type> BuildTypeIndex()
    {
        var index = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => t.IsSubclassOf(typeof(Item)) && !t.IsAbstract
                              && t.GetConstructor(Type.EmptyTypes) != null))
        {
            try
            {
                if (Activator.CreateInstance(type) is not Item probe) continue;
                if (index.TryGetValue(probe.ItemId, out var claimed))
                {
                    // Two types answering to one id makes a save ambiguous, and --item-audit already
                    // reports duplicate ItemIds. Keep the first and say which lost, rather than
                    // letting reflection order decide silently.
                    Console.Error.WriteLine(
                        $"ItemRegistry: item id '{probe.ItemId}' is claimed by both {claimed.Name} " +
                        $"and {type.Name}; keeping {claimed.Name}.");
                    continue;
                }
                index[probe.ItemId] = type;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ItemRegistry: failed to index {type.Name}: {ex.Message}");
            }
        }
        return index;
    }
}
