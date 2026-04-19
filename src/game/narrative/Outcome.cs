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
