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
/// Base class for outcomes that are concrete narrative anchors players can interact with.
/// Keywords are extracted dynamically from generated observation text by KeywordFallbackService.
/// </summary>
public abstract class ConcreteOutcome : OutcomeBase
{
}

/// <summary>
/// Lightweight inline outcome whose display name and natural-language string are given at
/// construction time. Used by the childhood reminescence path so that the concrete memory text
/// is visible to the outcome narrator's LLM prompt without needing a full registered type.
/// </summary>
public sealed class InlineOutcome : ConcreteOutcome
{
    private readonly string _displayName;
    private readonly string _naturalLanguage;

    public InlineOutcome(string displayName, string naturalLanguage)
    {
        _displayName    = displayName;
        _naturalLanguage = naturalLanguage;
    }

    public override string DisplayName           => _displayName;
    public override string ToNaturalLanguageString() => _naturalLanguage;
}
