using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Npc;

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
        ValidateKeywordsInContext();

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

    // ── Keyword validation ────────────────────────────────────────────────────

    /// <summary>
    /// Eagerly instantiates every node, item, observation, and NPC archetype and accesses
    /// their KeywordsInContext properties. Because KeywordInContext.Parse() validates on
    /// construction, any malformed phrase is caught here at startup rather than at runtime.
    /// Collects all errors before throwing so the full list is visible at once.
    /// </summary>
    private static void ValidateKeywordsInContext()
    {
        Console.WriteLine("Checking: All KeywordsInContext are valid...");

        var assembly = Assembly.GetExecutingAssembly();
        var errors = new List<string>();

        // ── Nodes ─────────────────────────────────────────────────────────────
        var nodeTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NarrationNode).IsAssignableFrom(t))
            .ToList();

        foreach (var nodeType in nodeTypes)
        {
            NarrationNode? node;
            try { node = (NarrationNode?)Activator.CreateInstance(nodeType); }
            catch { continue; }
            if (node == null) continue;

            try { _ = node.NodeKeywordsInContext; }
            catch (Exception ex)
            {
                errors.Add($"Node {nodeType.Name}.NodeKeywordsInContext: {ex.Message}");
            }
        }

        // ── Items ─────────────────────────────────────────────────────────────
        var itemTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t))
            .ToList();

        foreach (var itemType in itemTypes)
        {
            Item? item;
            try { item = (Item?)Activator.CreateInstance(itemType); }
            catch { continue; }
            if (item == null) continue;

            try { _ = item.OutcomeKeywordsInContext; }
            catch (Exception ex)
            {
                errors.Add($"Item {itemType.Name}.OutcomeKeywordsInContext: {ex.Message}");
            }
        }

        // ── ObservationObjects ────────────────────────────────────────────────
        var observationTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ObservationObject).IsAssignableFrom(t))
            .ToList();

        foreach (var obsType in observationTypes)
        {
            ObservationObject? obs;
            try { obs = (ObservationObject?)Activator.CreateInstance(obsType); }
            catch { continue; }
            if (obs == null) continue;

            try { _ = obs.ObservationKeywordsInContext; }
            catch (Exception ex)
            {
                errors.Add($"ObservationObject {obsType.Name}.ObservationKeywordsInContext: {ex.Message}");
            }

            foreach (var sub in obs.SubOutcomes)
            {
                try { _ = sub.OutcomeKeywordsInContext; }
                catch (Exception ex)
                {
                    errors.Add($"ObservationObject {obsType.Name} sub-outcome {sub.DisplayName}.OutcomeKeywordsInContext: {ex.Message}");
                }
            }
        }

        // ── NPC archetypes ────────────────────────────────────────────────────
        var archetypeTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NpcArchetype).IsAssignableFrom(t))
            .ToList();

        var buildMethod = typeof(NpcArchetype).GetMethod(
            "BuildNarrationKeywordsInContext",
            BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var archetypeType in archetypeTypes)
        {
            NpcArchetype? archetype;
            try { archetype = (NpcArchetype?)Activator.CreateInstance(archetypeType); }
            catch { continue; }
            if (archetype == null || buildMethod == null) continue;

            try { buildMethod.Invoke(archetype, new object[] { "Test" }); }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                errors.Add($"NpcArchetype {archetypeType.Name}.BuildNarrationKeywordsInContext: {tie.InnerException.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"NpcArchetype {archetypeType.Name}.BuildNarrationKeywordsInContext: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"VALIDATION FAILED: {errors.Count} invalid KeywordInContext definition(s):\n" +
                string.Join("\n", errors.Select(e => $"  - {e}")));
        }

        Console.WriteLine($"  ✓ All KeywordsInContext valid ({nodeTypes.Count} nodes, {itemTypes.Count} items, {observationTypes.Count} observations, {archetypeTypes.Count} NPC archetypes)");
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
