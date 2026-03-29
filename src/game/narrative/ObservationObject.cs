using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// An intermediate narrative layer between a NarrationNode and its outcomes.
/// Groups a set of related ConcreteOutcomes under shared observation keywords,
/// allowing multiple items to share the same visual "spot" in the narrative graph.
///
/// When a keyword on an ObservationObject is clicked:
/// - If SubOutcomes.Count == 1  → proceeds directly to WHY/HOW/WHAT (no extra LLM call).
/// - If SubOutcomes.Count  > 1  → fires a GOAL LLM call first so the thinking modusMentis
///   chooses which sub-outcome it wants to pursue, then continues with WHY/HOW/WHAT.
///
/// Items are nested inside their ObservationObject class exactly as they were nested inside
/// NarrationNode — the validator and reflection-based item discovery are updated accordingly.
/// </summary>
public abstract class ObservationObject : ConcreteOutcome, IObservation
{
    /// <summary>
    /// Unique identifier for this observation (e.g., "fox_den", "owl_pellet_site").
    /// </summary>
    public abstract string ObservationId { get; }

    /// <summary>
    /// Keywords with surrounding context that describe this observation as a whole.
    /// These are the keywords that appear in the parent node's observation text and
    /// route the player into this observation.
    /// </summary>
    public abstract List<KeywordInContext> ObservationKeywordsInContext { get; }

    /// <summary>
    /// All concrete sub-outcomes reachable through this observation (items, node transitions).
    /// Populated in the subclass constructor.
    /// </summary>
    public List<ConcreteOutcome> SubOutcomes { get; protected set; } = new();

    /// <summary>
    /// Generates a mood-qualified neutral description of this observation.
    /// Example: "musky fox den", "dry owl pellet site".
    /// </summary>
    public abstract string GenerateNeutralDescription(int locationId = 0);

    /// <summary>
    /// Optional NPC encounter slots associated with this observation.
    /// Override to propagate encounters to the parent area node via ForestGraphFactory.
    /// </summary>
    public virtual List<NpcEncounterSlot> AssociatedEncounters => new();

    // ── ConcreteOutcome overrides ─────────────────────────────────────────────

    /// <inheritdoc/>
    /// Aggregates own ObservationKeywordsInContext plus keywords from all SubOutcomes,
    /// de-duplicated by bare keyword (case-insensitive, first occurrence wins).
    public override List<KeywordInContext> OutcomeKeywordsInContext
    {
        get
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<KeywordInContext>();

            void Add(KeywordInContext kic)
            {
                if (seen.Add(kic.Keyword)) result.Add(kic);
            }

            foreach (var kic in ObservationKeywordsInContext) Add(kic);
            foreach (var sub in SubOutcomes)
                foreach (var kic in sub.OutcomeKeywordsInContext) Add(kic);

            return result;
        }
    }

    /// <inheritdoc/>
    public override string DisplayName => ObservationId;

    /// <inheritdoc/>
    public override string ToNaturalLanguageString() => GenerateNeutralDescription(0);

    /// <inheritdoc/>
    /// Example: "This musk is part of a musky fox den."
    public override string GetKeywordToOutcomeTransition(string keyword)
        => $"This {keyword} is part of {GenerateNeutralDescription(0)}.";

    // ── Routing helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a map from lowercase bare keyword to the sub-outcome that owns it.
    /// Observation-level keywords (ObservationKeywordsInContext) are NOT in this map —
    /// they route to the observation itself, not to a sub-outcome.
    /// </summary>
    public Dictionary<string, ConcreteOutcome> GetKeywordToSubOutcomeMap()
    {
        var map = new Dictionary<string, ConcreteOutcome>(StringComparer.OrdinalIgnoreCase);
        foreach (var sub in SubOutcomes)
            foreach (var kic in sub.OutcomeKeywordsInContext)
                map.TryAdd(kic.Keyword, sub);
        return map;
    }

    // ── IObservation ──────────────────────────────────────────────────────────
    // ObservationId is already declared as abstract above — satisfies the interface.
    List<KeywordInContext> IObservation.ObservationKeywords => ObservationKeywordsInContext;
    IReadOnlyList<ConcreteOutcome> IObservation.ObservationOutcomes =>
        SubOutcomes.AsReadOnly();
}
