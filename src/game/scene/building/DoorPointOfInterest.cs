using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>Lock state of a <see cref="DoorPointOfInterest"/>.</summary>
public enum DoorState { Locked, Unlocked }

/// <summary>
/// A door connecting two areas. Subclass of <see cref="PointOfInterest"/>.
///
/// Lock semantics:
///   - Locked → only the "unlock" verb is available from the front side (unlocks + enters in one action).
///   - Unlocked → "open the door" verb is available from the front side.
///   - From the back side the door is always passable regardless of lock state (one-way escape).
///
/// The DoorPointOfInterest should be added to both <see cref="FrontArea"/>.PointsOfInterest and
/// <see cref="BackArea"/>.PointsOfInterest so it is visible and interactable from either side.
/// </summary>
public class DoorPointOfInterest : PointOfInterest
{
    /// <summary>The area you approach the door from (unlock/open from this side).</summary>
    public Area FrontArea { get; }

    /// <summary>The area behind the door (always accessible from this side).</summary>
    public Area BackArea { get; }

    /// <summary>Current lock state. Persisted via <see cref="Element.StateProperties"/>.</summary>
    public DoorState DoorState
    {
        get => StateProperties.OfType<DoorState>().FirstOrDefault();
        set
        {
            StateProperties.RemoveAll(p => p is DoorState);
            StateProperties.Add(value);
        }
    }

    public DoorPointOfInterest(
        Area frontArea,
        Area backArea,
        string displayName,
        List<string> descriptions,
        List<KeywordInContext> keywords,
        DoorState initialState = DoorState.Locked)
        : base(displayName, descriptions, keywords)
    {
        FrontArea = frontArea;
        BackArea  = backArea;
        StateProperties.Add(initialState);
    }
}
