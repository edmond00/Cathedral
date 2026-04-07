using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// A specific space within a <see cref="Section"/>.
/// Contains <see cref="Spot"/>s the player can focus on.
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

    /// <summary>Spots (large objects, features) within this area.</summary>
    public List<Spot> Spots { get; } = new();

    /// <summary>Mood adjectives for procedural neutral descriptions.</summary>
    public string[] Moods { get; }

    public Area(
        string displayName,
        string contextDescription,
        string transitionDescription,
        List<string> descriptions,
        List<KeywordInContext> keywords,
        string[]? moods = null)
    {
        DisplayName           = displayName;
        ContextDescription    = contextDescription;
        TransitionDescription = transitionDescription;
        Descriptions          = descriptions;
        Keywords              = keywords;
        Moods                 = moods ?? System.Array.Empty<string>();
    }
}
