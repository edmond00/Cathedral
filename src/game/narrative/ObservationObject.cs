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
/// The observation persona rewrite selects the clickable keyword from its styled text; the
/// description returned by <see cref="GenerateNeutralDescription"/> is the neutral meaning the
/// rewrite re-expresses, so make it rich and noun-phrase.
/// </summary>
public abstract class ObservationObject : ConcreteOutcome, IObservation
{
    /// <summary>
    /// Unique identifier for this observation (e.g., "fox_den", "owl_pellet_site").
    /// </summary>
    public abstract string ObservationId { get; }

    /// <summary>
    /// Short, human-readable name in a few words (e.g. "fox den", "old well", "Hugh Furrow").
    /// Used to build the choice enum when the observation Modus Mentis picks which object to observe.
    /// Distinct from <see cref="GenerateNeutralDescription"/>, which is the longer description.
    /// </summary>
    public abstract string NeutralName { get; }

    /// <summary>
    /// Natural, articled noun phrase used to fill neutral sentence templates — e.g. "a beech tree",
    /// "an old well". Distinct from <see cref="NeutralName"/> ("Beech Tree"), which is a title and
    /// would read unnaturally mid-sentence ("...shifts to Beech Tree"). The default lower-cases
    /// <see cref="NeutralName"/> and prepends an article; NPC observation objects override this to
    /// keep the proper name verbatim ("Hugh Furrow").
    /// </summary>
    public virtual string NeutralPhrase => NeutralNarration.NounPhrase(NeutralName.ToLowerInvariant());

    /// <summary>
    /// One word naming the object's core noun (e.g. "tree", "well", "scarecrow"), used as the
    /// anchor when picking the clickable keyword: the noun in the generated text most semantically
    /// related to this word becomes the keyword. The keyword extractor lemmatizes this value, so a
    /// plural head word ("stones") still anchors on its lemma ("stone"). Defined explicitly by every
    /// concrete observation object — never inferred from the display name.
    /// </summary>
    public abstract string ReferenceLemma { get; }

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
