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
/// A single observation sentence paired with its one assigned keyword.
/// Stored on an observation NarrationBlock so the scroll buffer can assign each keyword
/// only to the wrapped lines of its own sentence, preventing cross-sentence highlighting.
/// </summary>
public record NarrationSentence(string Text, List<string> Keywords);

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
    public List<string>? Keywords { get; init; }               // Highlighted keywords (if observation, max 1 per sentence)
    public List<ParsedNarrativeAction>? Actions { get; init; } // Clickable actions (if thinking)

    /// <summary>
    /// Per-sentence breakdown for observation blocks. When set, each sentence's keyword is
    /// highlighted only within that sentence's wrapped lines — not across the whole block text.
    /// Parallel to <see cref="Keywords"/>: Sentences[i].Keyword == Keywords[i].
    /// </summary>
    public List<NarrationSentence>? Sentences { get; init; } = null;
    
    /// <summary>
    /// For observation blocks, indicates if this is an overall or focus observation.
    /// </summary>
    public ObservationType? SourceObservationType { get; init; } = null;

    /// <summary>
    /// The concrete outcome this observation sentence is about.
    /// Each sentence is generated for a specific outcome; clicking its keyword leads directly here.
    /// For merged multi-sentence blocks, prefer KeywordOutcomeMap instead.
    /// </summary>
    public ConcreteOutcome? LinkedOutcome { get; init; } = null;

    /// <summary>
    /// Per-keyword outcome map for merged observation blocks (multiple sentences joined into one block).
    /// Maps each extracted keyword → the ConcreteOutcome that sentence was describing.
    /// Takes precedence over LinkedOutcome during click resolution.
    /// </summary>
    public Dictionary<string, ConcreteOutcome>? KeywordOutcomeMap { get; init; } = null;

    /// <summary>
    /// Maps each extracted bare keyword → its full <see cref="KeywordInContext"/> (context phrase + keyword).
    /// Used to pass the enriched keyword context to the thinking phase when a keyword is clicked.
    /// </summary>
    public Dictionary<string, KeywordInContext>? KeywordContextMap { get; init; } = null;

    /// <summary>
    /// For Speaking blocks: the display name of the character who spoke (e.g., "Protagonist").
    /// Null for all other block types.
    /// </summary>
    public string? SpeakerName { get; init; } = null;

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
        ObservationType? SourceObservationType = null,
        ConcreteOutcome? LinkedOutcome = null,
        Dictionary<string, ConcreteOutcome>? KeywordOutcomeMap = null,
        List<NarrationSentence>? Sentences = null,
        Dictionary<string, KeywordInContext>? KeywordContextMap = null,
        string? SpeakerName = null)
    {
        this.Type = Type;
        this.ModusMentis = ModusMentis;
        this.Text = Text;
        this.Keywords = Keywords;
        this.Actions = Actions;
        this.ChainOrigin = ChainOrigin;
        this.SourceObservationType = SourceObservationType;
        this.LinkedOutcome = LinkedOutcome;
        this.KeywordOutcomeMap = KeywordOutcomeMap;
        this.Sentences = Sentences;
        this.KeywordContextMap = KeywordContextMap;
        this.SpeakerName = SpeakerName;
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
    Outcome,       // Result of action (success/failure)
    Speaking       // Active party member speaks directly to a companion
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
    /// Item combined with this action via right-click item selection.
    /// Null when no item is combined. When set, the action text has been reformulated
    /// by the action modusMentis to incorporate the item, and dice rolls receive a bonus
    /// equal to the item's UsageLevel.
    /// </summary>
    public Item? CombinedItem { get; set; } = null;

    /// <summary>
    /// Difficulty level pre-computed after the thinking phase (1–10 scale).
    /// 0 means not yet evaluated.
    /// </summary>
    public int DifficultyLevel { get; set; } = 0;

    /// <summary>
    /// When set (item-combined actions), acts as the chain leaf instead of ActionModusMentis.
    /// Holds a SyntheticItemModusMentis whose DisplayName = item name and Level = item.UsageLevel,
    /// so that the UI shows the item name as the action button prefix and the chain is:
    ///   observation → thinking → action (reasoning block) → item (this action).
    /// ActionModusMentis is still used for actual execution (slot lookup, organ score).
    /// </summary>
    public ModusMentis? CombinedActionModusMentis { get; set; } = null;

    /// <summary>
    /// Returns the item-level synthetic ModusMentis when this is a combined action,
    /// otherwise the real action ModusMentis. Used for chain traversal and display.
    /// </summary>
    public override ModusMentis ChainModusMentis => CombinedActionModusMentis ?? ActionModusMentis!;
}
