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
public class CliffPointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "cliff";

    /// <summary>The lower of the two areas.</summary>
    public Area BottomArea => AreaA;

    /// <summary>The higher of the two areas.</summary>
    public Area TopArea => AreaB;

    public bool IcyCliff { get; }

    public CliffPointOfInterest(
        Area bottomArea,
        Area topArea,
        string displayName,
        List<string> descriptions,
        bool icyCliff = false,
        string[]? moods = null)
        : base(bottomArea, topArea, displayName, "cliff", descriptions, moods: moods)
    {
        IcyCliff = icyCliff;
    }
}
