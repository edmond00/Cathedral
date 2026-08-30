using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>Lock state of a <see cref="DoorPointOfInterest"/>.</summary>
public enum DoorState { Locked, Unlocked }

/// <summary>
/// What a door is for. An entry door is the threshold between the outside world and a building's
/// public hall; everything else joins two rooms of the same building.
/// </summary>
public enum DoorRole { Entry, Interior }

/// <summary>
/// A door connecting two areas. Subclass of <see cref="PointOfInterest"/>.
///
/// Lock semantics:
///   - Locked → only the "unlock" verb is available from the front side (unlocks + enters in one action).
///   - Unlocked → "open the door" verb is available from the front side.
///   - From the back side the door is always passable regardless of lock state (one-way escape).
///   - An <see cref="DoorRole.Entry"/> door is additionally shut at <see cref="TimePeriod.Night"/>,
///     whatever its authored state — see <see cref="EffectiveState"/>.
///
/// The DoorPointOfInterest should be added to both <see cref="FrontArea"/>.PointsOfInterest and
/// <see cref="BackArea"/>.PointsOfInterest so it is visible and interactable from either side.
/// </summary>
public class DoorPointOfInterest : ConnectorPointOfInterest, IContextualDescription
{
    protected override string ConnectorKind => "door";

    /// <summary>The area you approach the door from (unlock/open from this side).</summary>
    public Area FrontArea => AreaA;

    /// <summary>The area behind the door (always accessible from this side).</summary>
    public Area BackArea => AreaB;

    /// <summary>Entry threshold or interior door. Drives the night rule and the outside description.</summary>
    public DoorRole Role { get; init; } = DoorRole.Interior;

    /// <summary>
    /// This door's own appearance, rolled at build time and unique within its building, so a player
    /// can navigate a house by remembering which door was which. Carries its own article.
    /// </summary>
    public string DoorDescription { get; init; } = "";

    /// <summary>
    /// The building this door leads into, as seen from outside. Appended to
    /// <see cref="DoorDescription"/> for an <see cref="DoorRole.Entry"/> door viewed from the
    /// <see cref="FrontArea"/> only. Carries its own article.
    /// </summary>
    public string BuildingDescription { get; init; } = "";

    /// <summary>
    /// Set when the player forces this door open. Deliberately a plain field rather than a
    /// <see cref="Element.StateProperties"/> entry, so nothing captures or persists it: scenes are
    /// rebuilt from scratch on every visit and a forced door is expected to be shut again next time.
    /// Within a visit it must stick, or stepping back outside would re-shut the door behind you.
    /// </summary>
    public bool ForcedOpen { get; set; }

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
        DoorState initialState = DoorState.Locked)
        : base(frontArea, backArea, displayName, "door", descriptions)
    {
        StateProperties.Add(initialState);
    
        // A door rewards a close look: it is how the lock, the hinges and the state of the frame are
        // learned, and it is the only route to the lesson about a lock's mechanism.
        Senses = SensoryProfile.Examinable;
}

    /// <summary>
    /// Lock state as the world actually behaves, which is what both door verbs and the description
    /// must agree on. Every building shuts its entry door at night regardless of what it is by day;
    /// a door the player has already forced this visit stays open.
    /// </summary>
    public DoorState EffectiveState(TimePeriod when)
    {
        if (ForcedOpen) return DoorState.Unlocked;
        if (DoorState == DoorState.Locked) return DoorState.Locked;
        if (Role == DoorRole.Entry && when == TimePeriod.Night) return DoorState.Locked;
        return DoorState.Unlocked;
    }

    /// <summary>
    /// Neutral description from one side. Looking at a building's entry door from the street names
    /// the building too ("a plank door of a low soot-stained workshop"); every other view — including
    /// the same door from the hall inside — is the door alone. Both end with a hint at the lock,
    /// which is the only way the player learns a door is shut short of trying it.
    /// </summary>
    public string DescribeFrom(Area viewingArea, TimePeriod when)
    {
        // "set in" rather than "of": a door description usually ends in a clause ("…pale where an
        // older lock was prised away"), and "prised away of a hunched house" is not English.
        var body = Role == DoorRole.Entry
                && viewingArea.Id == FrontArea.Id
                && BuildingDescription.Length > 0
            ? $"{DoorDescription}, set in {BuildingDescription}"
            : DoorDescription;

        if (body.Length == 0)
            body = Descriptions.Count > 0 ? Descriptions[0] : DisplayName.ToLowerInvariant();

        // Em-dash, not a full stop: NeutralNarration.NounPhrase trims trailing .,;:!? and would
        // otherwise eat the hint before the sentence is ever assembled.
        var hint = EffectiveState(when) == DoorState.Locked
            ? "the door seems locked"
            : "the door seems open";

        return $"{body} — {hint}";
    }
}
