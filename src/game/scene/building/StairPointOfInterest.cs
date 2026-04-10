using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// A staircase connecting two vertically adjacent areas. Subclass of <see cref="PointOfInterest"/>.
///
/// The StairPointOfInterest should be added to both <see cref="BottomArea"/>.PointsOfInterest and
/// <see cref="TopArea"/>.PointsOfInterest so "go up" and "go down" verbs are available from either end.
/// </summary>
public class StairPointOfInterest : PointOfInterest
{
    /// <summary>The area at the foot of the stairs.</summary>
    public Area BottomArea { get; }

    /// <summary>The area at the top of the stairs.</summary>
    public Area TopArea { get; }

    public StairPointOfInterest(
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
