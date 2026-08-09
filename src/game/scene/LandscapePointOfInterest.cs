using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// One distant place, seen from a high one — and the way to it.
///
/// <para>A landscape is a <b>road you can see but not yet be on</b>. It hangs in an area you had to
/// climb to and points at somewhere else in the location, and <c>voyage_toward</c> walks it. That
/// makes it the same kind of thing as a track or a stair: a connector, joining two areas, and the
/// only reason it is not listed at both ends is that a road you can see from a rooftop is not
/// visible from the street below it. It is attached to the viewpoint alone, so the journey runs one
/// way — you come back by the ordinary graph.</para>
///
/// <para><b>Why a scene object and not remembered knowledge.</b> This replaces a horizon that
/// recorded revealed areas onto the <c>PoV</c> and gated its verbs on that set. Nothing else in the
/// game gates on accumulated point-of-view state, and it did not survive contact with the code that
/// re-gates verbs against a freshly built <c>PoV</c> — so the reveal worked, the knowledge was
/// written, and every verb that depended on it read an empty set. As an object in the area, a
/// landscape is refreshed like any other point of interest and there is no second copy to disagree
/// with.</para>
///
/// <para><see cref="AllowsGraphEdge"/> is true, unlike a door or a cliff. Those are <i>gates</i>:
/// crossing them is meant to cost a roll, so an area-graph edge beside one would silently make them
/// decorative. A landscape gates nothing — the mill is walkable by road as well — it is a shortcut
/// earned by the climb, and the destination is expected to be reachable by other means too.</para>
///
/// <para>Factories place these deliberately, naming what can be seen from where. There is no
/// automatic pass: a view over a location is a statement about that location, and the one thing the
/// old automatic placement reliably produced was a cave entrance reporting that "the country opens
/// out below".</para>
/// </summary>
public class LandscapePointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "landscape";

    /// <summary>The high area this is seen from. The only area it is listed in.</summary>
    public Area Viewpoint => AreaA;

    /// <summary>The distant area it is a road to.</summary>
    public Area Destination => AreaB;

    /// <inheritdoc/>
    /// <remarks>Not a gate — see the class summary. The destination is normally walkable anyway.</remarks>
    public override bool AllowsGraphEdge => true;

    public LandscapePointOfInterest(Area viewpoint, Area destination, string displayName,
                                    List<string> descriptions, string[]? moods = null)
        : base(viewpoint, destination, displayName, destination.ReferenceLemma, descriptions,
               moods ?? new[] { "far-off", "hazed", "small with distance" },
               isNatural: true)
    {
        // Worth looking at properly — reading a country at a distance is what topographia is — and
        // worth sitting with, because it is a view. Nothing to smell or hear from this far away.
        Senses = SensoryProfile.Beautiful;
        VerbModiMentis = new Dictionary<string, string>
        {
            ["examine"]        = "topographia",
            ["contemplate"]    = "aesthetic",
            ["voyage_toward"]  = "voyage",
        };
    }

    /// <summary>
    /// Lists this in the <b>viewpoint only</b> and registers it, where
    /// <see cref="ConnectorPointOfInterest.AttachTo"/> would list it at both ends. That asymmetry is
    /// the whole point: the far side cannot see the road back.
    /// </summary>
    public void AttachToViewpoint(Scene scene)
    {
        Viewpoint.PointsOfInterest.Add(this);
        Register(scene);
    }
}
