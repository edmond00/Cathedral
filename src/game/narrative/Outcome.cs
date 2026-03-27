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
    /// Keywords with surrounding context that serve as narrative anchors for this outcome.
    /// Each entry contains a contextual phrase (e.g. "a rough bark of the beech") with the
    /// actual keyword word marked by &lt;...&gt; in the raw source (e.g. "a rough &lt;bark&gt; of the beech").
    /// The bare <see cref="KeywordInContext.Keyword"/> is used for UI display and text matching;
    /// the full <see cref="KeywordInContext.Context"/> is used in LLM prompts.
    /// </summary>
    public abstract List<KeywordInContext> OutcomeKeywordsInContext { get; }

    /// <summary>
    /// Returns a single sentence that contextualises a clicked keyword in relation to this outcome.
    /// Used in thinking prompts to bridge "You noticed {keyword}" and "Now you want to {outcome}".
    /// Example: "This stream leads to a mossy forest clearing."
    /// Subclasses override this to provide outcome-type-appropriate syntax.
    /// </summary>
    public virtual string GetKeywordToOutcomeTransition(string keyword)
        => $"This {keyword} is connected to {DisplayName}.";
}
