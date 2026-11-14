using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Narrative.Tree;

/// <summary>
/// What is remembered of the things and people of a place: the third state an element can be in,
/// between unknown (an anonymous symbol of its kind, and the observing mind chooses) and observed
/// (the root of a tree). A known element is named on the map and a click attends to it and no other —
/// but it must still be observed to be thought about, since a tree lasts one visit and the mind that
/// observed is part of every roll made on it.
///
/// <para>Kept in <see cref="Scene.KnownElements"/>, which is the mental map's store, so it is
/// forgotten with the place. A way has no entry here: a way seen or gone by is already in the map's
/// own stores (<see cref="AreaVisits"/>).</para>
/// </summary>
public static class ElementMemory
{
    /// <summary>
    /// The key an element is remembered under, or null for what is one visit's business: a corpse, a
    /// sleeper, an item lying in a container.
    /// </summary>
    public static string? KeyOf(Element? element) => element switch
    {
        SceneNpc npc => $"npc|{npc.Entity.PersistentId}",
        PointOfInterest { SpawnedDuringVisit: false, StableKey.Length: > 0 } poi => $"poi|{poi.StableKey}",
        _ => null,
    };

    /// <summary>Whether <paramref name="anchor"/>, seen from <paramref name="here"/>, is remembered.</summary>
    public static bool IsKnown(Scene.Scene scene, Area here, NarrativeAnchor anchor) => anchor switch
    {
        SyntheticAreaObservationObject { Area: var area }
            => Knows(scene, AreaVisits.LinkKey(here, area)),
        SyntheticObservationObject { PointOfInterest: ConnectorPointOfInterest connector }
            => Knows(scene, connector.StableKey),
        _ => KeyOf(ElementKinds.ElementOf(anchor)) is { } key && scene.KnownElements.Contains(key),
    };

    private static bool Knows(Scene.Scene scene, string link)
        => scene.SeenLinks.Contains(link) || scene.TakenLinks.Contains(link);

    /// <summary>An observation took <paramref name="anchor"/> in. Ways are recorded by <see cref="AreaVisits.See"/>.</summary>
    public static void Learn(Scene.Scene scene, NarrativeAnchor anchor)
    {
        if (KeyOf(ElementKinds.ElementOf(anchor)) is { } key) scene.KnownElements.Add(key);
    }
}
