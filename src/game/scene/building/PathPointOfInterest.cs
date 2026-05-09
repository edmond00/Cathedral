using System.Collections.Generic;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// A named open path connecting two areas (road, track, lane, stream path).
/// Always passable from either end. Targeted by <see cref="Verbs.FollowPathVerb"/>.
///
/// Should be added to both <see cref="AreaA"/>.PointsOfInterest and
/// <see cref="AreaB"/>.PointsOfInterest so it is interactable from either side.
/// </summary>
public class PathPointOfInterest : PointOfInterest
{
    public Area AreaA { get; }
    public Area AreaB { get; }

    public PathPointOfInterest(
        Area areaA,
        Area areaB,
        string displayName,
        List<string> descriptions,
        string[]? moods = null)
        : base(displayName, descriptions, items: null, moods: moods)
    {
        AreaA = areaA;
        AreaB = areaB;
    }

    /// <summary>Returns the area on the far side of the path relative to the given area.</summary>
    public Area Other(Area from) => from.Id == AreaA.Id ? AreaB : AreaA;
}
