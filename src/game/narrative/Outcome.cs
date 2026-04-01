using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Uniform interface implemented by NarrationNode, Item, and ObservationObject.
/// Lets the graph be traversed as a list of observations regardless of the concrete type.
/// </summary>
public interface IObservation
{
    /// <summary>Stable identifier for this observation (NodeId / ItemId / ObservationId).</summary>
    string ObservationId { get; }

    /// <summary>Keywords describing this observation (node-level, item-level, or observation-level).</summary>
    List<KeywordInContext> ObservationKeywords { get; }

    /// <summary>Concrete outcomes reachable through this observation.</summary>
    IReadOnlyList<ConcreteOutcome> ObservationOutcomes { get; }
}

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
    /// Used in thinking prompts to bridge "You noticed {keyword/context}" and "Now you want to {outcome}".
    /// When <paramref name="keywordInContext"/> is provided, uses "It" instead of "This {keyword}" to
    /// avoid repeating words already present in the noticed clause.
    /// Subclasses override this to provide outcome-type-appropriate syntax.
    /// </summary>
    public virtual string GetKeywordToOutcomeTransition(string keyword, KeywordInContext? keywordInContext = null)
        => keywordInContext != null
            ? $"It is connected to {DisplayName}."
            : $"This {keyword} is connected to {DisplayName}.";
}
