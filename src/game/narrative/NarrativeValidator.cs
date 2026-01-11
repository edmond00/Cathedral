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
        
        // Validation 1: Every Item type must be declared inside a NarrationNode
        var itemsOutsideNodes = itemTypes.Where(t => t.DeclaringType == null || !typeof(NarrationNode).IsAssignableFrom(t.DeclaringType)).ToList();
        if (itemsOutsideNodes.Any())
        {
            throw new InvalidOperationException(
                $"Items must be declared as inner classes of NarrationNode. Found {itemsOutsideNodes.Count} items outside nodes: " +
                string.Join(", ", itemsOutsideNodes.Select(t => t.Name)));
        }
        
        // Validation 2: No two Item types share the same name
        var itemNameGroups = itemTypes.GroupBy(t => t.Name).Where(g => g.Count() > 1).ToList();
        if (itemNameGroups.Any())
        {
            throw new InvalidOperationException(
                $"Item types must have unique names. Found duplicate names: " +
                string.Join(", ", itemNameGroups.Select(g => g.Key)));
        }
        
        // Validation 3: Every Item has exactly one origin node (its declaring type)
        foreach (var itemType in itemTypes)
        {
            if (itemType.DeclaringType == null)
            {
                throw new InvalidOperationException($"Item {itemType.Name} has no declaring type");
            }
            
            if (!typeof(NarrationNode).IsAssignableFrom(itemType.DeclaringType))
            {
                throw new InvalidOperationException($"Item {itemType.Name} is not declared in a NarrationNode");
            }
        }
        
        // Validation 4: Items must be sealed
        var unsealedItems = itemTypes.Where(t => !t.IsSealed).ToList();
        if (unsealedItems.Any())
        {
            throw new InvalidOperationException(
                $"Items must be sealed. Found {unsealedItems.Count} unsealed items: " +
                string.Join(", ", unsealedItems.Select(t => t.Name)));
        }
        
        Console.WriteLine("✓ All items are inner classes of nodes");
        Console.WriteLine("✓ All item names are unique");
        Console.WriteLine("✓ All items have exactly one origin node");
        Console.WriteLine("✓ All items are sealed");
        Console.WriteLine("Narrative structure validation complete!");
    }
    
    /// <summary>
    /// Prints a summary of all nodes and their items for debugging.
    /// </summary>
    public static void PrintNarrativeStructure()
    {
        Console.WriteLine("\n=== Narrative Structure ===\n");
        
        var assembly = Assembly.GetExecutingAssembly();
        var nodeTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NarrationNode).IsAssignableFrom(t))
            .ToList();
        
        foreach (var nodeType in nodeTypes)
        {
            var node = (NarrationNode?)Activator.CreateInstance(nodeType);
            if (node == null) continue;
            
            Console.WriteLine($"Node: {node.NodeId} ({nodeType.Name})");
            
            var items = node.GetAvailableItems();
            if (items.Any())
            {
                Console.WriteLine($"  Items:");
                foreach (var item in items)
                {
                    Console.WriteLine($"    - {item.DisplayName} ({item.ItemId})");
                }
            }
            else
            {
                Console.WriteLine($"  No items");
            }
            
            var transitions = node.PossibleOutcomes.OfType<NarrationNode>().ToList();
            if (transitions.Any())
            {
                Console.WriteLine($"  Transitions:");
                foreach (var transition in transitions)
                {
                    Console.WriteLine($"    - {transition.NodeId}");
                }
            }
            
            Console.WriteLine();
        }
    }
}
