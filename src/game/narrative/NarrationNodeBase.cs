using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a discrete narrative context within a location that can be reached as an outcome.
/// Implements IObservation: a node IS its own observation whose outcomes are its items + child NarrationNodes.
/// </summary>
public abstract class NarrationNode : ConcreteOutcome, IObservation
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
    public List<OutcomeBase> PossibleOutcomes { get; set; } = new();

    /// <summary>
    /// Can this node be used as an entry point when entering the location?
    /// </summary>
    public abstract bool IsEntryNode { get; }

    /// <summary>
    /// NPC encounter slots for this node. Used by <see cref="NarrationGraphFactory.BuildNpcs"/>
    /// at graph-construction time to decide whether to include an NPC in this location.
    /// Empty by default (no encounters).
    /// </summary>
    public virtual List<NpcEncounterSlot> PossibleEncounters => new();

    /// <summary>
    /// Returns the items available at this node. Override in subclasses to list items explicitly.
    /// </summary>
    public virtual List<Item> GetItems() => new();

    /// <summary>
    /// Gets all items available at this node. Delegates to <see cref="GetItems"/>.
    /// </summary>
    public List<Item> GetAvailableItems() => GetItems();

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
    /// Generates an enriched context description that includes a mood qualifier.
    /// Default: returns ContextDescription as-is.
    /// </summary>
    public virtual string GenerateEnrichedContextDescription(int locationId = 0)
        => ContextDescription;

    /// <summary>
    /// Builds the two-line location context used at the start of every first LLM call.
    /// </summary>
    public string BuildLocationContext(WorldContext worldContext, int locationId)
        => $"You are in a {worldContext.GenerateContextDescription(locationId)}. You are currently {GenerateEnrichedContextDescription(locationId)}.";

    public override string ToNaturalLanguageString() => TransitionDescription;

    /// <summary>
    /// Gets all concrete outcomes directly available at this node (child nodes + items + spawned NPCs).
    /// Used for sampling which outcomes to generate observation sentences for.
    /// All ConcreteOutcomes are included regardless of keywords — keywords are found dynamically.
    /// </summary>
    public List<ConcreteOutcome> GetAllDirectConcreteOutcomes()
    {
        var outcomes = new List<ConcreteOutcome>();

        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ConcreteOutcome co)
                outcomes.Add(co);
        }

        foreach (var item in GetAvailableItems())
            outcomes.Add(item);

        return outcomes;
    }

    /// <summary>
    /// Gets all observations at this node as IObservation instances:
    /// ObservationObjects, child NarrationNodes, and items (each self-referential).
    /// </summary>
    public List<IObservation> GetObservations()
    {
        var result = new List<IObservation>();
        foreach (var outcome in PossibleOutcomes)
        {
            if (outcome is ObservationObject obs) result.Add(obs);
            else if (outcome is NarrationNode nn) result.Add(nn);
        }
        foreach (var item in GetAvailableItems())
            result.Add(item);
        return result;
    }

    // ── IObservation ──────────────────────────────────────────────────────────
    string IObservation.ObservationId => NodeId;
    IReadOnlyList<ConcreteOutcome> IObservation.ObservationOutcomes
    {
        get
        {
            var result = new List<ConcreteOutcome>();
            result.AddRange(GetAvailableItems());
            result.AddRange(PossibleOutcomes.OfType<NarrationNode>());
            return result.AsReadOnly();
        }
    }
}
