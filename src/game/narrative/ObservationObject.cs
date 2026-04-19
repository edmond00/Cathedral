using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// An intermediate narrative layer between a NarrationNode and its outcomes.
/// Groups a set of related ConcreteOutcomes under a shared observation, allowing
/// multiple items to share the same visual "spot" in the narrative graph.
///
/// When an ObservationObject keyword is clicked, the thinking modusMentis always runs
/// REFLECT + GOAL before WHY/HOW/WHAT. The GOAL choices include all SubOutcomes plus the
/// "ignore and move on" sentinel.
///
/// Keywords are found dynamically in LLM-generated text by <see cref="KeywordFallbackService"/>.
/// The description returned by <see cref="GenerateNeutralDescription"/> is used by the LLM
/// as context when selecting the best keyword; make it rich and noun-phrase.
/// </summary>
public abstract class ObservationObject : ConcreteOutcome, IObservation
{
    /// <summary>
    /// Unique identifier for this observation (e.g., "fox_den", "owl_pellet_site").
    /// </summary>
    public abstract string ObservationId { get; }

    /// <summary>
    /// All concrete sub-outcomes reachable through this observation (items, node transitions).
    /// Populated in the subclass constructor.
    /// </summary>
    public List<ConcreteOutcome> SubOutcomes { get; protected set; } = new();

    /// <summary>
    /// Generates a rich noun-phrase description of this observation used both in LLM prompts
    /// and as context for dynamic keyword selection.
    /// Example: "musky fox den", "dry owl pellet site".
    /// Make this description vivid — the richer it is, the better keyword matching works.
    /// </summary>
    public abstract string GenerateNeutralDescription(int locationId = 0);

    /// <summary>
    /// Optional NPC encounter slots associated with this observation.
    /// </summary>
    public virtual List<NpcEncounterSlot> AssociatedEncounters => new();

    // ── ConcreteOutcome overrides ─────────────────────────────────────────────

    /// <inheritdoc/>
    public override string DisplayName => ObservationId;

    /// <inheritdoc/>
    public override string ToNaturalLanguageString() => GenerateNeutralDescription(0);

    // ── IObservation ──────────────────────────────────────────────────────────
    string IObservation.ObservationId => ObservationId;
    IReadOnlyList<ConcreteOutcome> IObservation.ObservationOutcomes =>
        SubOutcomes.AsReadOnly();
}
