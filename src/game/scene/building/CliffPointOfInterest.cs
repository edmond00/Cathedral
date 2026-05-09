using System.Collections.Generic;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// A vertical cliff/ladder/ascent connecting a lower area to a higher area.
/// Climbed via <see cref="Verbs.ClimbUpVerb"/> (from bottom) or <see cref="Verbs.ClimbDownVerb"/>
/// (from top). Set <see cref="IcyCliff"/> to raise the difficulty.
///
/// Should be added to both <see cref="BottomArea"/>.PointsOfInterest and
/// <see cref="TopArea"/>.PointsOfInterest.
/// </summary>
public class CliffPointOfInterest : PointOfInterest
{
    public Area BottomArea { get; }
    public Area TopArea    { get; }
    public bool IcyCliff   { get; }

    public CliffPointOfInterest(
        Area bottomArea,
        Area topArea,
        string displayName,
        List<string> descriptions,
        bool icyCliff = false,
        string[]? moods = null)
        : base(displayName, descriptions, items: null, moods: moods)
    {
        BottomArea = bottomArea;
        TopArea    = topArea;
        IcyCliff   = icyCliff;
    }
}
