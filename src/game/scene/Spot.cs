using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// A large object or small space within an <see cref="Area"/> that the player can focus on.
/// For example: a tree, a boulder, a patch of flowers, a piece of furniture.
/// Contains <see cref="Narrative.Item"/>s that can be collected by interacting with this spot.
/// </summary>
public class Spot : Element
{
    public override string DisplayName { get; }
    public override List<string> Descriptions { get; }
    public override List<KeywordInContext> Keywords { get; }

    /// <summary>Items that can be collected from this spot.</summary>
    public List<ItemElement> Items { get; } = new();

    /// <summary>Mood adjectives for procedural neutral descriptions.</summary>
    public string[] Moods { get; }

    public Spot(
        string displayName,
        List<string> descriptions,
        List<KeywordInContext> keywords,
        List<ItemElement>? items = null,
        string[]? moods = null)
    {
        DisplayName  = displayName;
        Descriptions = descriptions;
        Keywords     = keywords;
        Moods        = moods ?? System.Array.Empty<string>();
        if (items != null) Items.AddRange(items);
    }
}
