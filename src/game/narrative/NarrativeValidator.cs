using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Validates the narrative node and item structure to ensure world coherence.
/// </summary>
public static class NarrativeValidator
{
    /// <summary>
    /// Validates all narration nodes and items in the assembly.
    /// Since PossibleOutcomes are now populated at runtime by factories,
    /// this only validates node templates and item structure.
    /// Throws exceptions if validation fails.
    /// </summary>
    public static void ValidateNarrativeStructure()
    {
        Console.WriteLine("Validating narrative structure...");

        var assembly = Assembly.GetExecutingAssembly();
        var allTypes = assembly.GetTypes();

        // Find all Item types
        var itemTypes = allTypes
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .ToList();

        // Find all NarrationNode types
        var nodeTypes = allTypes
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NarrationNode).IsAssignableFrom(t))
            .ToList();

        Console.WriteLine($"Found {itemTypes.Count} item types and {nodeTypes.Count} node types");
        Console.WriteLine($"Note: PossibleOutcomes validation skipped (populated at runtime by factories)");

        // Validation: No two Item types share the same ItemId
        var idGroups = new Dictionary<string, List<string>>();
        foreach (var itemType in itemTypes)
        {
            Item? item;
            try { item = (Item?)Activator.CreateInstance(itemType); }
            catch { continue; }
            if (item == null) continue;

            if (!idGroups.TryGetValue(item.ItemId, out var list))
                idGroups[item.ItemId] = list = new List<string>();
            list.Add(itemType.Name);
        }

        var duplicates = idGroups.Where(kv => kv.Value.Count > 1).ToList();
        if (duplicates.Any())
        {
            throw new InvalidOperationException(
                $"Item types must have unique ItemIds. Found duplicates: " +
                string.Join(", ", duplicates.Select(kv => $"\"{kv.Key}\" ({string.Join(", ", kv.Value)})")));
        }

        Console.WriteLine("✓ All item ItemIds are unique");
        Console.WriteLine("Narrative structure validation complete!");
    }

    /// <summary>
    /// Prints a summary of all node templates and their items for debugging.
    /// Note: PossibleOutcomes are populated at runtime, so transitions are not shown here.
    /// </summary>
    public static void PrintNarrativeStructure()
    {
        Console.WriteLine("\n=== Narrative Node Templates ===\n");

        var assembly = Assembly.GetExecutingAssembly();
        var nodeTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NarrationNode).IsAssignableFrom(t))
            .ToList();

        foreach (var nodeType in nodeTypes)
        {
            var node = (NarrationNode?)Activator.CreateInstance(nodeType);
            if (node == null) continue;

            Console.WriteLine($"Node: {node.NodeId} ({nodeType.Name})");
            Console.WriteLine($"  Is Entry Node: {node.IsEntryNode}");
            Console.WriteLine($"  Keywords: {string.Join(", ", node.NodeKeywordsInContext.Select(k => k.Keyword))}");

            var items = node.GetAvailableItems();
            if (items.Any())
            {
                Console.WriteLine($"  Items ({items.Count}):");
                foreach (var item in items)
                    Console.WriteLine($"    - {item.DisplayName} ({item.ItemId})");
            }
            else
            {
                Console.WriteLine($"  No items");
            }

            Console.WriteLine($"  Note: Transitions populated at runtime by factories");
            Console.WriteLine();
        }
    }
}
