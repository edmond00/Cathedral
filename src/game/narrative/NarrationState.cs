using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for elements in a modusMentis chain.
/// The modusMentis chain represents the sequence of modiMentis involved in an action:
/// Observation -> Thinking -> Action
/// Each element has an associated modusMentis and optional link to its origin element.
/// </summary>
public abstract class ModusMentisChainElement
{
    /// <summary>
    /// The modusMentis associated with this chain element.
    /// </summary>
    public abstract ModusMentis ChainModusMentis { get; }
    
    /// <summary>
    /// The previous element in the modusMentis chain (if any).
    /// - Observations: always null (they are roots of the chain)
    /// - Thinking: points to the observation that triggered it
    /// - Action: points to the thinking block that generated it
    /// </summary>
    public ModusMentisChainElement? ChainOrigin { get; set; }
    
    /// <summary>
    /// Calculates the total modusMentis level sum by traversing the chain back to the root.
    /// This represents the number of dice that will be rolled for a modusMentis check.
    /// </summary>
    public int GetTotalModusMentisLevel()
    {
        int total = ChainModusMentis?.Level ?? 0;
        var current = ChainOrigin;
        while (current != null)
        {
            total += current.ChainModusMentis?.Level ?? 0;
            current = current.ChainOrigin;
        }
        return total;
    }
    
    /// <summary>
    /// Gets all modiMentis in the chain from root to this element.
    /// </summary>
    public List<ModusMentis> GetModusMentisChain()
    {
        var modiMentis = new List<ModusMentis>();
        var current = this;
        while (current != null)
        {
            if (current.ChainModusMentis != null)
            {
                modiMentis.Insert(0, current.ChainModusMentis); // Insert at beginning to maintain order
            }
            current = current.ChainOrigin;
        }
        return modiMentis;
    }
    
    /// <summary>
    /// Checks if a specific ModusMentisChainElement is an ancestor in this element's chain.
    /// This checks the actual element instances, not just matching modiMentis.
    /// </summary>
    public bool IsElementInChain(ModusMentisChainElement? element)
    {
        if (element == null) return false;
        
        var current = this;
        while (current != null)
        {
            if (ReferenceEquals(current, element))
            {
                return true;
            }
            current = current.ChainOrigin;
        }
        return false;
    }
}

/// <summary>
/// Tracks the current state of narration flow.
/// Manages history of narration blocks, current node, thinking attempts, etc.
/// </summary>
public class NarrationState
{
    public string CurrentNodeId { get; set; } = "";
    public int ThinkingAttemptsRemaining { get; set; } = 3;
    public string? SelectedKeyword { get; set; }
    public ModusMentis? SelectedThinkingModusMentis { get; set; }
    public List<NarrationBlock> NarrationHistory { get; } = new();
    
    public void AddBlock(NarrationBlock block)
    {
        NarrationHistory.Add(block);
    }
    
    public void ClearHistory()
    {
        NarrationHistory.Clear();
    }
    
    public List<string> GetAllKeywords()
    {
        return NarrationHistory
            .Where(b => b.Keywords != null)
            .SelectMany(b => b.Keywords!)
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// Represents a single block of narration text in the UI.
/// Can be observation, thinking (CoT), action result, or outcome.
/// Inherits from ModusMentisChainElement to participate in modusMentis chain calculations.
/// </summary>
public class NarrationBlock : ModusMentisChainElement
{
    public NarrationBlockType Type { get; init; }              // Observation, Thinking, Action, Outcome
    public ModusMentis ModusMentis { get; init; } = null!;                 // Which modusMentis generated this block
    public string Text { get; init; } = "";                    // The narration text
    public List<string>? Keywords { get; init; }               // Highlighted keywords (if observation)
    public List<ParsedNarrativeAction>? Actions { get; init; } // Clickable actions (if thinking)
    
    /// <summary>
    /// For observation blocks, indicates if this is an overall or focus observation.
    /// Used to determine circuitous outcome availability.
    /// </summary>
    public ObservationType? SourceObservationType { get; init; } = null;
    
    /// <summary>
    /// Implements ModusMentisChainElement.ChainModusMentis - returns the modusMentis of this block.
    /// </summary>
    public override ModusMentis ChainModusMentis => ModusMentis;
    
    /// <summary>
    /// Creates a new NarrationBlock with the specified parameters.
    /// </summary>
    public NarrationBlock(
        NarrationBlockType Type,
        ModusMentis ModusMentis,
        string Text,
        List<string>? Keywords,
        List<ParsedNarrativeAction>? Actions,
        ModusMentisChainElement? ChainOrigin = null,
        ObservationType? SourceObservationType = null)
    {
        this.Type = Type;
        this.ModusMentis = ModusMentis;
        this.Text = Text;
        this.Keywords = Keywords;
        this.Actions = Actions;
        this.ChainOrigin = ChainOrigin;
        this.SourceObservationType = SourceObservationType;
    }
}

/// <summary>
/// Types of narration blocks that can appear in the UI.
/// </summary>
public enum NarrationBlockType
{
    Observation,   // ModusMentis perceives environment
    Thinking,      // ModusMentis reasons about keyword (CoT)
    Action,        // Player selected action (modusMentis check result)
    Outcome        // Result of action (success/failure)
}

/// <summary>
/// Types of observation blocks in the narration system.
/// Used to determine if a keyword came from an overall or focus observation.
/// </summary>
public enum ObservationType
{
    /// <summary>The first observation generated when entering a node.</summary>
    Overall,
    /// <summary>A detailed observation generated by right-clicking a keyword.</summary>
    Focus
}

/// <summary>
/// Represents an action generated by a thinking modusMentis.
/// Extended version of ParsedAction for narrative system.
/// Inherits from ModusMentisChainElement to participate in modusMentis chain calculations.
/// The ChainOrigin should point to the thinking block that generated this action.
/// </summary>
public class ParsedNarrativeAction : ModusMentisChainElement
{
    public string ActionText { get; set; } = "";              // Full text including "try to " prefix
    public string DisplayText { get; set; } = "";             // Text without "try to " prefix (for UI)
    public string ActionModusMentisId { get; set; } = "";           // Which action modusMentis to use for check
    public ModusMentis? ActionModusMentis { get; set; }                   // Resolved modusMentis reference
    public ModusMentis ThinkingModusMentis { get; set; } = null!;         // Which thinking modusMentis generated this
    public OutcomeBase PreselectedOutcome { get; set; } = null!;  // Success outcome chosen by thinking modusMentis
    public string Keyword { get; set; } = "";                 // Keyword this action relates to
    
    /// <summary>
    /// Whether this is a circuitous action (requires going through an intermediate node).
    /// Circuitous actions have additional difficulty penalty.
    /// </summary>
    public bool IsCircuitous { get; set; } = false;
    
    /// <summary>
    /// For circuitous actions, the intermediate node that must be traversed.
    /// Null for straightforward actions.
    /// </summary>
    public NarrationNode? IntermediateNode { get; set; } = null;
    
    /// <summary>
    /// Implements ModusMentisChainElement.ChainModusMentis - returns the action modusMentis.
    /// </summary>
    public override ModusMentis ChainModusMentis => ActionModusMentis!;
}
