using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// A focused sub-location within an <see cref="Area"/>.
///
/// Containment rules:
///   • An area can hold both <see cref="PointOfInterest"/>s and <see cref="Spot"/>s.
///   • A spot belongs to exactly one parent area.
///   • A spot contains zero or more <see cref="PointOfInterest"/>s.
///
/// Navigation rules:
///   • The player enters a spot by choosing it in the area view (EnterSpot verb).
///   • While inside a spot the player sees the spot's PoIs, not the surrounding area.
///   • From a spot the player can only leave back to the parent area (Leave verb).
///
/// Usage examples: a corpse on the ground, a merchant's stall, a small altar.
/// </summary>
public class Spot : Element
{
    public override string DisplayName { get; }
    public override List<string> Descriptions { get; }

    /// <summary>The area this spot belongs to.</summary>
    public Area ParentArea { get; }

    /// <summary>Points of interest visible while the player is inside this spot.</summary>
    public List<PointOfInterest> PointsOfInterest { get; } = new();

    public Spot(
        Area parentArea,
        string displayName,
        List<string> descriptions)
    {
        ParentArea   = parentArea;
        DisplayName  = displayName;
        Descriptions = descriptions;
    }
}
