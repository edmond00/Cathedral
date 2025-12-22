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
    public abstract List<string> Keywords { get; }
}
