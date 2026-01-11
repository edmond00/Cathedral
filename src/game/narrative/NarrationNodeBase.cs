using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a discrete narrative context within a location that can be reached as an outcome.
/// Nodes have their own keywords (for being discovered as transitions) plus keywords from immediate outcomes.
/// Neutral descriptions are generated with random qualifiers for variety.
/// </summary>
public abstract class NarrationNode : ConcreteOutcome
{
    /// <summary>
    /// Unique identifier for this node (e.g., "clearing", "stream").
    /// </summary>
    public abstract string NodeId { get; }
    
    /// <summary>
    /// All possible outcomes available from this node.
    /// </summary>
    public abstract List<OutcomeBase> PossibleOutcomes { get; }
    
    /// <summary>
    /// Can this node be used as an entry point when entering the location?
    /// </summary>
    public abstract bool IsEntryNode { get; }
    
    /// <summary>
    /// Node IDs that can be reached from this node via transitions.
    /// </summary>
    public abstract List<string> NodeKeywords { get; }
    
    /// <summary>
    /// Gets all items available at this node by discovering Item inner classes via reflection.
    /// Items are automatically discovered - they do not need to be manually listed.
    /// </summary>
    public List<Item> GetAvailableItems()
    {
        var items = new List<Item>();
        var nodeType = GetType();
        
        // Find all nested types that inherit from Item
        var itemTypes = nodeType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Item).IsAssignableFrom(t));
        
        foreach (var itemType in itemTypes)
        {
            try
            {
                // Create an instance of the item
                var item = (Item?)Activator.CreateInstance(itemType);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to instantiate item type {itemType.Name}: {ex.Message}");
            }
        }
        
        return items;
    }
    
    /// <summary>
    /// All keywords available at this node: node's own keywords plus keywords from items and child nodes.
    /// This is used to determine what the player can interact with at this location.
    /// </summary>
    public override List<string> OutcomeKeywords
    {
        get
        {
            var allKeywords = new HashSet<string>(NodeKeywords, StringComparer.OrdinalIgnoreCase);
            
            // Add keywords from items discovered via reflection
            var items = GetAvailableItems();
            foreach (var item in items)
            {
                foreach (var keyword in item.OutcomeKeywords)
                {
                    allKeywords.Add(keyword);
                }
            }
            
            // Add keywords from child NarrationNodes
            foreach (var outcome in PossibleOutcomes)
            {
                if (outcome is NarrationNode childNode)
                {
                    foreach (var keyword in childNode.NodeKeywords)
                    {
                        allKeywords.Add(keyword);
                    }
                }
            }
            
            return allKeywords.ToList();
        }
    }
    
    /// <summary>
    /// Display name is just the node type without qualifiers (e.g., "clearing" not "sun-dappled clearing").
    /// </summary>
    public override string DisplayName => NodeId;
    
    /// <summary>
    /// Generates a neutral description with random qualifiers for variety.
    /// Override this to provide node-specific description generation.
    /// </summary>
    /// <param name="locationId">Location ID used as RNG seed for consistency</param>
    public abstract string GenerateNeutralDescription(int locationId = 0);
    
    /// <summary>
    /// Gets all outcomes that have a specific keyword.
    /// Includes both child nodes and items discovered via reflection.
    /// </summary>
    public List<OutcomeBase> GetOutcomesForKeyword(string keyword)
    {
        var normalizedKeyword = keyword.ToLowerInvariant();
        var outcomes = new List<OutcomeBase>();
        
        // Check child nodes
        outcomes.AddRange(PossibleOutcomes
            .Where(outcome => outcome is ConcreteOutcome co && co.OutcomeKeywords.Any(k => k.ToLowerInvariant() == normalizedKeyword)));
        
        // Check items from reflection
        var items = GetAvailableItems();
        outcomes.AddRange(items
            .Where(item => item.OutcomeKeywords.Any(k => k.ToLowerInvariant() == normalizedKeyword)));
        
        return outcomes;
    }
    
    public override string ToNaturalLanguageString()
    {
        return $"transition {NodeId}";
    }
}
