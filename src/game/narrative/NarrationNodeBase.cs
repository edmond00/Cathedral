using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a discrete narrative context within a location that can be reached as an outcome.
/// Implements IObservation: a node IS its own observation whose outcomes are its items + child NarrationNodes.
/// </summary>
public abstract class NarrationNode : IObservation
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
    public List<INarratable> PossibleOutcomes { get; set; } = new();

    /// <summary>
    /// Can this node be used as an entry point when entering the location?
    /// </summary>
    public abstract bool IsEntryNode { get; }



    /// <summary>
    /// Display name is just the node type without qualifiers (e.g., "clearing" not "sun-dappled clearing").
    /// </summary>
    public string DisplayName => NodeId;

    /// <summary>
    /// Generates a neutral description with random qualifiers for variety.
    /// Override this to provide node-specific description generation.
    /// </summary>
    /// <param name="locationId">Location ID used as RNG seed for consistency</param>
    public abstract string GenerateNeutralDescription(int locationId = 0);

    /// <summary>
    /// Generates an enriched context description that includes a mood qualifier.
    /// Default: returns ContextDescription as-is.
    /// </summary>
    public virtual string GenerateEnrichedContextDescription(int locationId = 0)
        => ContextDescription;

    /// <summary>
    /// Builds the two-line location context used at the start of every first LLM call.
    /// Override in special-phase nodes (e.g. childhood reminescence) to substitute a
    /// non-location prompt frame.
    /// </summary>
    public virtual string BuildLocationContext(WorldContext worldContext, int locationId)
        => $"You are in a {worldContext.GenerateContextDescription(locationId)}. You are currently {GenerateEnrichedContextDescription(locationId)}.";

    public string ToNaturalLanguageString() => TransitionDescription;

    /// <summary>
    /// Gets all concrete outcomes directly available at this node (child nodes + items + spawned NPCs).
    /// Used for sampling which outcomes to generate observation sentences for.
    /// All ConcreteOutcomes are included regardless of keywords — keywords are found dynamically.
    /// </summary>
    public List<NarrativeAnchor> GetAllDirectConcreteOutcomes()
    {
        var outcomes = new List<NarrativeAnchor>();

        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is NarrativeAnchor co)
                outcomes.Add(co);
        }


        return outcomes;
    }

    /// <summary>
    /// Gets all observations at this node as IObservation instances:
    /// ObservationObjects, child NarrationNodes, and items (each self-referential).
    /// </summary>
    public List<IObservation> GetObservations()
    {
        // Observation objects only. Nodes used to be reachable from one another through
        // PossibleOutcomes, but nothing has built such an edge since the scene graph replaced the
        // hand-authored one.
        return PossibleOutcomes.OfType<ObservationObject>().Cast<IObservation>().ToList();
    }

    // ── IObservation ──────────────────────────────────────────────────────────
    string IObservation.ObservationId => NodeId;
    IReadOnlyList<NarrativeAnchor> IObservation.ObservationOutcomes =>
        PossibleOutcomes.OfType<NarrativeAnchor>().ToList().AsReadOnly();
}
