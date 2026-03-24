using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for all possible outcomes in the narrative system.
/// Outcomes are achieved through successful actions.
/// </summary>
public abstract class OutcomeBase
{
    /// <summary>
    /// Display name for this outcome (for UI and logging).
    /// </summary>
    public abstract string DisplayName { get; }
    
    /// <summary>
    /// Converts the outcome to a natural language string for LLM communication.
    /// </summary>
    public abstract string ToNaturalLanguageString();
}

/// <summary>
/// Base class for outcomes that have associated keywords.
/// Keywords serve as narrative anchors that players can interact with.
/// </summary>
public abstract class ConcreteOutcome : OutcomeBase
{
    /// <summary>
    /// Generic keywords that can serve as narrative anchors for this outcome.
    /// These are simple words like "leaf", "water", "path" that can naturally
    /// appear in observation narration and link to this specific outcome.
    /// </summary>
    public abstract List<string> OutcomeKeywords { get; }
}

/// <summary>
/// Wrapper that adds metadata to an outcome for the thinking phase.
/// Tracks whether the outcome is circuitous (requires going through an intermediate node).
/// </summary>
public class OutcomeWithMetadata
{
    /// <summary>
    /// The actual outcome.
    /// </summary>
    public OutcomeBase Outcome { get; }
    
    /// <summary>
    /// Whether this is a circuitous outcome (requires going through an intermediate node).
    /// </summary>
    public bool IsCircuitous { get; }
    
    /// <summary>
    /// For circuitous outcomes, the intermediate node that must be traversed.
    /// Null for straightforward outcomes.
    /// </summary>
    public NarrationNode? IntermediateNode { get; }
    
    public OutcomeWithMetadata(OutcomeBase outcome, bool isCircuitous = false, NarrationNode? intermediateNode = null)
    {
        Outcome = outcome;
        IsCircuitous = isCircuitous;
        IntermediateNode = intermediateNode;
    }
    
    /// <summary>
    /// Creates a straightforward (non-circuitous) outcome wrapper.
    /// </summary>
    public static OutcomeWithMetadata Straightforward(OutcomeBase outcome) 
        => new(outcome, isCircuitous: false, intermediateNode: null);
    
    /// <summary>
    /// Creates a circuitous outcome wrapper with an intermediate node.
    /// </summary>
    public static OutcomeWithMetadata Circuitous(OutcomeBase outcome, NarrationNode intermediateNode)
        => new(outcome, isCircuitous: true, intermediateNode: intermediateNode);
}
