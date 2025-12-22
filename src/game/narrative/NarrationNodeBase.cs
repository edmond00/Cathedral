using System;
using System.Collections.Generic;
using System.Linq;

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
    public abstract List<string> PossibleTransitions { get; }
    
    /// <summary>
    /// Keywords that make this node discoverable as a transition from other nodes.
    /// These are the keywords that describe this location itself.
    /// </summary>
    public abstract List<string> NodeKeywords { get; }
    
    /// <summary>
    /// All keywords available at this node: node's own keywords plus keywords from immediate outcomes.
    /// This is used to determine what the player can interact with at this location.
    /// </summary>
    public override List<string> Keywords
    {
        get
        {
            var allKeywords = new HashSet<string>(NodeKeywords, StringComparer.OrdinalIgnoreCase);
            
            // Add keywords from immediate concrete outcomes (items, etc.)
            // BUT NOT from child NarrationNodes (to avoid circular references)
            foreach (var outcome in PossibleOutcomes)
            {
                if (outcome is ConcreteOutcome concreteOutcome && outcome is not NarrationNode)
                {
                    foreach (var keyword in concreteOutcome.Keywords)
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
    /// </summary>
    public List<OutcomeBase> GetOutcomesForKeyword(string keyword)
    {
        var normalizedKeyword = keyword.ToLowerInvariant();
        return PossibleOutcomes
            .Where(outcome => outcome is ConcreteOutcome co && co.Keywords.Any(k => k.ToLowerInvariant() == normalizedKeyword))
            .ToList();
    }
    
    public override string ToNaturalLanguageString()
    {
        return $"transition {NodeId}";
    }
}
