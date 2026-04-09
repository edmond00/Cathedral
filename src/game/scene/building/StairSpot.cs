using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// A staircase connecting two vertically adjacent areas. Subclass of <see cref="Spot"/>.
///
/// The StairSpot should be added to both <see cref="BottomArea"/>.Spots and
/// <see cref="TopArea"/>.Spots so "go up" and "go down" verbs are available from either end.
/// </summary>
public class StairSpot : Spot
{
    /// <summary>The area at the foot of the stairs.</summary>
    public Area BottomArea { get; }

    /// <summary>The area at the top of the stairs.</summary>
    public Area TopArea { get; }

    public StairSpot(
        Area bottomArea,
        Area topArea,
        string displayName,
        List<string> descriptions,
        List<KeywordInContext> keywords)
        : base(displayName, descriptions, keywords)
    {
        BottomArea = bottomArea;
        TopArea    = topArea;
    }
}
