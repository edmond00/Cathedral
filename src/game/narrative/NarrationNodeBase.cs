using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Npc;

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
    /// Short context description used in critic prompts (e.g., "exploring a clearing", "examining a stream").
    /// This provides context to the LLM about what the player is currently doing at this node.
    /// </summary>
    public abstract string ContextDescription { get; }
    
    /// <summary>
    /// Natural language description for transitioning to this node (e.g., "approach the stream").
    /// Used in LLM prompts to describe possible outcomes.
    /// </summary>
    public abstract string TransitionDescription { get; }
    
    /// <summary>
    /// All possible outcomes available from this node.
    /// Populated at runtime by NarrationGraphFactory.
    /// </summary>
    public List<OutcomeBase> PossibleOutcomes { get; set; } = new();
    
    /// <summary>
    /// Can this node be used as an entry point when entering the location?
    /// </summary>
    public abstract bool IsEntryNode { get; }
    
    /// <summary>
    /// Node IDs that can be reached from this node via transitions.
    /// </summary>
    public abstract List<string> NodeKeywords { get; }
    
    /// <summary>
    /// NPC encounter slots for this node. Override in subclasses to define
    /// which NPC types can appear here and with what probability.
    /// Empty by default (no encounters).
    /// </summary>
    public virtual List<NpcEncounterSlot> PossibleEncounters => new();

    /// <summary>
    /// NPCs currently present at this node (populated at runtime by NpcSpawner).
    /// Persistent NPCs survive across visits; transient ones are re-rolled each time.
    /// </summary>
    public List<NpcEntity> SpawnedNpcs { get; set; } = new();

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
            
            // Add keywords from spawned NPCs
            foreach (var npc in SpawnedNpcs)
            {
                foreach (var keyword in npc.NarrationKeywords)
                {
                    allKeywords.Add(keyword);
                }
            }
            
            return allKeywords.ToList();
        }
    }
    
    /// <summary>
    /// Gets keywords grouped by their source outcome.
    /// Returns a dictionary mapping outcome display names to their associated keywords.
    /// Used to provide context to LLM about which keywords relate to which outcomes.
    /// </summary>
    public Dictionary<string, List<string>> GetKeywordsByOutcome()
    {
        var result = new Dictionary<string, List<string>>();
        
        // Node's own keywords (grouped under current node name)
        if (NodeKeywords.Count > 0)
        {
            result[NodeId] = new List<string>(NodeKeywords);
        }
        
        // Keywords from items discovered via reflection
        var items = GetAvailableItems();
        foreach (var item in items)
        {
            if (item.OutcomeKeywords.Count > 0)
            {
                result[item.DisplayName] = new List<string>(item.OutcomeKeywords);
            }
        }
        
        // Keywords from child NarrationNodes
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is NarrationNode childNode && childNode.NodeKeywords.Count > 0)
            {
                result[childNode.DisplayName] = new List<string>(childNode.NodeKeywords);
            }
        }
        
        // Keywords from spawned NPCs
        foreach (var npc in SpawnedNpcs)
        {
            if (npc.NarrationKeywords.Length > 0)
            {
                result[npc.DisplayName] = new List<string>(npc.NarrationKeywords);
            }
        }
        
        return result;
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
    /// These are "straightforward" outcomes - directly linked to the keyword.
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
        return TransitionDescription;
    }

    /// <summary>
    /// Gets all concrete outcomes directly available at this node (child nodes + items + spawned NPCs).
    /// Used for sampling which outcomes to generate observation sentences for.
    /// </summary>
    public List<ConcreteOutcome> GetAllDirectConcreteOutcomes()
    {
        var outcomes = new List<ConcreteOutcome>();

        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is NarrationNode childNode && childNode.NodeKeywords.Count > 0)
                outcomes.Add(childNode);
            else if (outcome is ConcreteOutcome co && !(outcome is NarrationNode) && co.OutcomeKeywords.Count > 0)
                outcomes.Add(co);
        }

        foreach (var item in GetAvailableItems())
        {
            if (item.OutcomeKeywords.Count > 0)
                outcomes.Add(item);
        }

        return outcomes;
    }


    /// If outcomes >= targetCount, samples one keyword from targetCount random outcomes.
    /// If outcomes < targetCount, samples multiple keywords per outcome to reach target.
    /// </summary>
    /// <param name="random">Random instance for consistent sampling</param>
    /// <param name="targetKeywordCount">Target number of keywords to return</param>
    /// <returns>List of (keyword, outcome) tuples</returns>
    public List<(string Keyword, ConcreteOutcome Outcome)> GetRepresentativeKeywordsPerOutcome(Random random, int targetKeywordCount = Cathedral.Config.Narrative.TargetKeywordCount)
    {
        var result = new List<(string, ConcreteOutcome)>();
        
        // Get all immediate concrete outcomes (direct child nodes + items at this node)
        var concreteOutcomes = new List<ConcreteOutcome>();
        
        // Add child nodes that are concrete outcomes
        // NOTE: We only add the node itself, not its recursive keywords
        // The child node's OutcomeKeywords will be used only when actually transitioning to that node
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ConcreteOutcome co)
            {
                // For child NarrationNodes, check if they have NodeKeywords
                // For other ConcreteOutcomes (like Items), check if they have OutcomeKeywords
                if (outcome is NarrationNode node && node.NodeKeywords.Count > 0)
                {
                    concreteOutcomes.Add(co);
                }
                else if (!(outcome is NarrationNode) && co.OutcomeKeywords.Count > 0)
                {
                    concreteOutcomes.Add(co);
                }
            }
        }
        
        // Add items discovered via reflection at THIS node only
        var items = GetAvailableItems();
        foreach (var item in items)
        {
            if (item.OutcomeKeywords.Count > 0)
            {
                concreteOutcomes.Add(item);
            }
        }
        
        // Apply new sampling logic based on target keyword count
        if (concreteOutcomes.Count == 0)
        {
            return result;
        }
        
        if (concreteOutcomes.Count >= targetKeywordCount)
        {
            // More outcomes than target: sample targetKeywordCount outcomes, one keyword each
            var shuffledOutcomes = concreteOutcomes.OrderBy(_ => random.Next()).Take(targetKeywordCount);
            foreach (var outcome in shuffledOutcomes)
            {
                // For child NarrationNodes, use NodeKeywords (not full OutcomeKeywords which include grandchildren)
                // For Items, use OutcomeKeywords as usual
                List<string> keywordsToUse = outcome is NarrationNode node ? node.NodeKeywords : outcome.OutcomeKeywords;
                
                if (keywordsToUse.Count > 0)
                {
                    var selectedKeyword = keywordsToUse[random.Next(keywordsToUse.Count)];
                    result.Add((selectedKeyword, outcome));
                }
            }
        }
        else
        {
            // Fewer outcomes than target: sample multiple keywords per outcome until target reached
            var keywordsPerOutcome = targetKeywordCount / concreteOutcomes.Count;
            var extraKeywords = targetKeywordCount % concreteOutcomes.Count;
            
            for (int i = 0; i < concreteOutcomes.Count; i++)
            {
                var outcome = concreteOutcomes[i];
                
                // For child NarrationNodes, use NodeKeywords (not full OutcomeKeywords which include grandchildren)
                // For Items, use OutcomeKeywords as usual
                List<string> keywordsToUse = outcome is NarrationNode node ? node.NodeKeywords : outcome.OutcomeKeywords;
                
                if (keywordsToUse.Count == 0) continue;
                
                // Calculate how many keywords to take from this outcome
                var keywordsToTake = keywordsPerOutcome + (i < extraKeywords ? 1 : 0);
                keywordsToTake = Math.Min(keywordsToTake, keywordsToUse.Count);
                
                // Sample keywords without replacement from this outcome
                var shuffledKeywords = keywordsToUse.OrderBy(_ => random.Next()).Take(keywordsToTake);
                foreach (var keyword in shuffledKeywords)
                {
                    result.Add((keyword, outcome));
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the concrete outcome that owns a specific keyword.
    /// Returns null if keyword is not found or belongs to multiple outcomes.
    /// </summary>
    public ConcreteOutcome? GetOutcomeOwningKeyword(string keyword)
    {
        var normalizedKeyword = keyword.ToLowerInvariant();
        
        // Check child nodes
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ConcreteOutcome co && 
                co.OutcomeKeywords.Any(k => k.ToLowerInvariant() == normalizedKeyword))
            {
                return co;
            }
        }
        
        // Check items from reflection
        var items = GetAvailableItems();
        foreach (var item in items)
        {
            if (item.OutcomeKeywords.Any(k => k.ToLowerInvariant() == normalizedKeyword))
            {
                return item;
            }
        }
        
        return null;
    }
}
