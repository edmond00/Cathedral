using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Validates world coherence rules for the narrative system at startup.
///
/// Items may live either as nested inner classes of NarrationNode / ObservationObject
/// (legacy pattern) or as standalone classes in the Cathedral.Game.Narrative.Items
/// namespace. Both locations are fully supported.
/// </summary>
public static class NarrativeWorldValidator
{
    public static void ValidateWorldCoherence()
    {
        Console.WriteLine("=== Validating Narrative World Coherence ===");

        ValidateUniqueItemIds();

        Console.WriteLine("=== Narrative World Coherence: PASSED ===");
    }

    // ── Item ID uniqueness ────────────────────────────────────────────────────

    /// <summary>
    /// Instantiates every concrete Item type and checks that no two share the same ItemId.
    /// </summary>
    private static void ValidateUniqueItemIds()
    {
        Console.WriteLine("Checking: All items must have unique ItemIds...");

        var assembly = Assembly.GetExecutingAssembly();
        var allItemTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .ToList();

        var idToTypes = new Dictionary<string, List<string>>();

        foreach (var itemType in allItemTypes)
        {
            Item? item;
            try { item = (Item?)Activator.CreateInstance(itemType); }
            catch { continue; }
            if (item == null) continue;

            if (!idToTypes.TryGetValue(item.ItemId, out var list))
                idToTypes[item.ItemId] = list = new List<string>();
            list.Add(itemType.FullName ?? itemType.Name);
        }

        var duplicates = idToTypes.Where(kv => kv.Value.Count > 1).ToList();
        if (duplicates.Any())
        {
            var msg = "VALIDATION FAILED: The following ItemIds are duplicated:\n" +
                      string.Join("\n", duplicates.Select(kv =>
                          $"  - \"{kv.Key}\": {string.Join(", ", kv.Value)}"));
            throw new InvalidOperationException(msg);
        }

        Console.WriteLine($"  ✓ All {allItemTypes.Count} item types have unique ItemIds");
    }

    // ── Debug helper ──────────────────────────────────────────────────────────

    /// <summary>Prints all item types grouped by their location (standalone vs nested).</summary>
    public static void PrintWorldStructure()
    {
        Console.WriteLine("\n=== Narrative World Structure ===");

        var assembly = Assembly.GetExecutingAssembly();
        var allItemTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();

        var standalone = allItemTypes.Where(t => !t.IsNested).ToList();
        var nested     = allItemTypes.Where(t => t.IsNested).ToList();

        Console.WriteLine($"\nStandalone items ({standalone.Count}):");
        foreach (var t in standalone)
        {
            var item = (Item?)Activator.CreateInstance(t);
            Console.WriteLine($"  [{item?.ItemId}]  {t.Name}  ({t.Namespace})");
        }

        Console.WriteLine($"\nNested items ({nested.Count}):");
        foreach (var t in nested)
        {
            var item = (Item?)Activator.CreateInstance(t);
            Console.WriteLine($"  [{item?.ItemId}]  {t.DeclaringType?.Name}.{t.Name}");
        }

        Console.WriteLine("\n=================================\n");
    }
}
