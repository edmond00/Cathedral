using System.Linq;
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
///
/// <para><b>Known is each member's own.</b> The areas, the ways between them and the routines are
/// the party's — one member walks straight to ground another discovered — but which chest is the
/// chest and which man the reeve is known only to whoever looked, or was told: an entry is the
/// member's key and the element's (<see cref="EntryOf"/>). That is what speaking of a thing to a
/// companion is for, beyond the visit it is said in.</para>
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

    /// <summary>The store's entry for <paramref name="key"/> as <paramref name="member"/> knows it.</summary>
    public static string EntryOf(PartyMember member, string key) => $"{member.AffinityKey}>{key}";

    /// <summary>
    /// Whether <paramref name="member"/> remembers the element under <paramref name="key"/>. An
    /// entry with no member, written before known was each member's own, reads as the protagonist's.
    /// </summary>
    public static bool Knows(Scene.Scene scene, PartyMember member, string key)
        => scene.KnownElements.Contains(EntryOf(member, key))
           || (member is Protagonist && scene.KnownElements.Contains(key));

    /// <summary>Whether anybody of the party remembers the element under <paramref name="key"/>: what a menu map counts.</summary>
    public static bool AnybodyKnows(Scene.Scene scene, string key)
        => scene.KnownElements.Contains(key) || scene.KnownElements.Any(e => e.EndsWith(">" + key, System.StringComparison.Ordinal));

    /// <summary>
    /// Whether <paramref name="anchor"/>, seen from <paramref name="here"/>, is remembered by
    /// <paramref name="member"/>. A way is the party's: seen or gone by, it is known to all.
    /// </summary>
    public static bool IsKnown(Scene.Scene scene, Area here, NarrativeAnchor anchor, PartyMember member) => anchor switch
    {
        SyntheticAreaObservationObject { Area: var area }
            => Knows(scene, AreaVisits.LinkKey(here, area)),
        SyntheticObservationObject { PointOfInterest: ConnectorPointOfInterest connector }
            => Knows(scene, connector.StableKey),
        _ => KeyOf(ElementKinds.ElementOf(anchor)) is { } key && Knows(scene, member, key),
    };

    private static bool Knows(Scene.Scene scene, string link)
        => scene.SeenLinks.Contains(link) || scene.TakenLinks.Contains(link);

    /// <summary>
    /// <paramref name="member"/> took <paramref name="anchor"/> in: by an observation of their own,
    /// or told of it by a companion. Ways are recorded by <see cref="AreaVisits.See"/>.
    /// </summary>
    public static void Learn(Scene.Scene scene, NarrativeAnchor anchor, PartyMember member)
        => Learn(scene, ElementKinds.ElementOf(anchor), member);

    public static void Learn(Scene.Scene scene, Element? element, PartyMember member)
    {
        if (KeyOf(element) is { } key) scene.KnownElements.Add(EntryOf(member, key));
    }
}
