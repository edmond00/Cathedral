using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// A specific space within a <see cref="Section"/>.
/// Contains <see cref="PointOfInterest"/>s and <see cref="Spot"/>s the player can focus on.
/// Connected to other areas via directed edges in <see cref="Scene.AreaGraph"/>.
/// </summary>
public class Area : Element
{
    public override string DisplayName { get; }
    public override List<string> Descriptions { get; }

    /// <summary>
    /// Core noun used as the keyword-similarity anchor when this area becomes an observation.
    /// Defined explicitly at every construction site (no inference) — e.g. "grassland".
    /// </summary>
    public string ReferenceLemma { get; }

    /// <summary>Context description for LLM prompts (e.g. "crossing the open grassland").</summary>
    public string ContextDescription { get; }

    /// <summary>Transition description for LLM prompts (e.g. "move into the grassland").</summary>
    public string TransitionDescription { get; }

    /// <summary>Points of interest (large objects, features) directly within this area.</summary>
    public List<PointOfInterest> PointsOfInterest { get; } = new();

    /// <summary>
    /// Sub-locations within this area that the player can enter.
    /// Each spot has its own PoIs and is navigated separately (Enter/Leave verbs).
    /// </summary>
    public List<Spot> Spots { get; } = new();

    /// <summary>Mood adjectives for procedural neutral descriptions.</summary>
    public string[] Moods { get; }

    /// <summary>
    /// When true, entering or taking items from this area without permission is an illegal action.
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// A place worth naming from a distance: the mill, the cliff top, the standing stone. Landmarks
    /// are what a horizon observation from a high place lists, and what <c>GoTowardVerb</c> then
    /// walks to directly.
    ///
    /// <para>Every factory is expected to mark <b>at least two</b> — one landmark gives a survey
    /// nothing to compare against — and <c>--verb-audit</c> reports any location that does not.
    /// Mark the places that read as destinations, not every area with a good description.</para>
    /// </summary>
    public bool IsLandmark { get; set; }

    public Area(
        string displayName,
        string referenceLemma,
        string contextDescription,
        string transitionDescription,
        List<string> descriptions,
        string[]? moods = null,
        bool isPrivate = false)
    {
        DisplayName           = displayName;
        ReferenceLemma        = referenceLemma;
        ContextDescription    = contextDescription;
        TransitionDescription = transitionDescription;
        Descriptions          = descriptions;
        Moods      = moods ?? System.Array.Empty<string>();
        IsPrivate  = isPrivate;
    }
}
