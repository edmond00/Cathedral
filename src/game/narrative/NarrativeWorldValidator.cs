using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Validates the world coherence rules for the narrative system.
/// Ensures all items are inner classes of nodes and have unique names.
/// </summary>
public static class NarrativeWorldValidator
{
    /// <summary>
    /// Validates all narrative world coherence rules.
    /// Throws exceptions if validation fails.
    /// </summary>
    public static void ValidateWorldCoherence()
    {
        Console.WriteLine("=== Validating Narrative World Coherence ===");
        
        ValidateAllItemsAreInnerClasses();
        ValidateUniqueItemNames();
        ValidateItemOrigins();
        
        Console.WriteLine("=== Narrative World Coherence: PASSED ===");
    }
    
    /// <summary>
    /// Ensures every Item type is declared inside a NarrationNode.
    /// </summary>
    private static void ValidateAllItemsAreInnerClasses()
    {
        Console.WriteLine("Checking: All items must be inner classes of nodes...");
        
        var assembly = Assembly.GetExecutingAssembly();
        var allItemTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .ToList();
        
        var invalidItems = new List<Type>();
        
        foreach (var itemType in allItemTypes)
        {
            // Check if it's a nested type
            if (!itemType.IsNested)
            {
                invalidItems.Add(itemType);
                continue;
            }
            
            // Check if the declaring type is a NarrationNode
            var declaringType = itemType.DeclaringType;
            if (declaringType == null || !typeof(NarrationNode).IsAssignableFrom(declaringType))
            {
                invalidItems.Add(itemType);
            }
        }
        
        if (invalidItems.Any())
        {
            var errorMessage = "VALIDATION FAILED: The following items are not inner classes of NarrationNode:\n" +
                               string.Join("\n", invalidItems.Select(t => $"  - {t.FullName}"));
            throw new InvalidOperationException(errorMessage);
        }
        
        Console.WriteLine($"  ✓ All {allItemTypes.Count} item types are properly nested in nodes");
    }
    
    /// <summary>
    /// Ensures no two Item types share the same name.
    /// </summary>
    private static void ValidateUniqueItemNames()
    {
        Console.WriteLine("Checking: All items must have unique names...");
        
        var assembly = Assembly.GetExecutingAssembly();
        var allItemTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .ToList();
        
        var nameGroups = allItemTypes
            .GroupBy(t => t.Name)
            .Where(g => g.Count() > 1)
            .ToList();
        
        if (nameGroups.Any())
        {
            var errorMessage = "VALIDATION FAILED: The following item names are used by multiple types:\n" +
                               string.Join("\n", nameGroups.Select(g => 
                                   $"  - {g.Key}: {string.Join(", ", g.Select(t => t.FullName))}"));
            throw new InvalidOperationException(errorMessage);
        }
        
        Console.WriteLine($"  ✓ All {allItemTypes.Count} item types have unique names");
    }
    
    /// <summary>
    /// Validates that every Item has exactly one origin node (its declaring type).
    /// </summary>
    private static void ValidateItemOrigins()
    {
        Console.WriteLine("Checking: All items must have exactly one origin node...");
        
        var assembly = Assembly.GetExecutingAssembly();
        var allItemTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .ToList();
        
        var itemsWithInvalidOrigins = new List<string>();
        
        foreach (var itemType in allItemTypes)
        {
            if (!itemType.IsNested)
            {
                itemsWithInvalidOrigins.Add($"{itemType.Name}: No origin (not nested)");
                continue;
            }
            
            var declaringType = itemType.DeclaringType;
            if (declaringType == null)
            {
                itemsWithInvalidOrigins.Add($"{itemType.Name}: No declaring type");
                continue;
            }
            
            if (!typeof(NarrationNode).IsAssignableFrom(declaringType))
            {
                itemsWithInvalidOrigins.Add($"{itemType.Name}: Origin is not a NarrationNode ({declaringType.Name})");
            }
        }
        
        if (itemsWithInvalidOrigins.Any())
        {
            var errorMessage = "VALIDATION FAILED: The following items have invalid origins:\n" +
                               string.Join("\n", itemsWithInvalidOrigins.Select(s => $"  - {s}"));
            throw new InvalidOperationException(errorMessage);
        }
        
        Console.WriteLine($"  ✓ All {allItemTypes.Count} items have valid single origins");
    }
    
    /// <summary>
    /// Prints a summary of all nodes and their items (for debugging).
    /// </summary>
    public static void PrintWorldStructure()
    {
        Console.WriteLine("\n=== Narrative World Structure ===");
        
        var assembly = Assembly.GetExecutingAssembly();
        var allNodeTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NarrationNode).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();
        
        foreach (var nodeType in allNodeTypes)
        {
            Console.WriteLine($"\n{nodeType.Name}:");
            
            // Find nested item types
            var itemTypes = nodeType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
                .ToList();
            
            if (itemTypes.Any())
            {
                foreach (var itemType in itemTypes)
                {
                    Console.WriteLine($"  → {itemType.Name}");
                }
            }
            else
            {
                Console.WriteLine("  (no items)");
            }
        }
        
        Console.WriteLine("\n=================================\n");
    }
}
