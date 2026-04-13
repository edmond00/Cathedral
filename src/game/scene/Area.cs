using System.Collections.Generic;
using Cathedral.Game.Narrative;

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
    public override List<KeywordInContext> Keywords { get; }

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
    /// Triggers witness detection and the "caught red-handed" dialogue on failure.
    /// </summary>
    public bool IsPrivate { get; set; }

    public Area(
        string displayName,
        string contextDescription,
        string transitionDescription,
        List<string> descriptions,
        List<KeywordInContext> keywords,
        string[]? moods = null,
        bool isPrivate = false)
    {
        DisplayName           = displayName;
        ContextDescription    = contextDescription;
        TransitionDescription = transitionDescription;
        Descriptions          = descriptions;
        Keywords              = keywords;
        Moods      = moods ?? System.Array.Empty<string>();
        IsPrivate  = isPrivate;
    }
}
