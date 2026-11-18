using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Narrative.Tree;

/// <summary>
/// What a symbol on the location map says a click will look at, before anything is known of it. An
/// observation is of one kind: the player chooses the kind by the symbol clicked, and the observing
/// modus mentis chooses the element among those of that kind.
/// </summary>
public enum ElementKind
{
    /// <summary>A way on: a neighbouring area, a door, a stair, a path, a climb, a crossing, a view.</summary>
    Way,
    /// <summary>A living thing that does not count the viewer an enemy, shallow wildlife included.</summary>
    Person,
    /// <summary>A living thing that counts the viewer an enemy.</summary>
    Hostile,
    /// <summary>Every other point of interest, and an item one of them holds.</summary>
    Thing,
}

/// <summary>The kind of an observable, its symbol and its words.</summary>
public static class ElementKinds
{
    /// <summary>The kinds in the order a card lists them.</summary>
    public static readonly ElementKind[] All =
        { ElementKind.Way, ElementKind.Person, ElementKind.Hostile, ElementKind.Thing };

    /// <summary>
    /// The kind of <paramref name="anchor"/> as <paramref name="viewer"/> sees it. Decided by type
    /// alone — the same test <see cref="AreaVisits.See"/> makes for a way — and, for the living, by
    /// whether they count the viewer an enemy, which a symbol shows before anything else is known.
    /// </summary>
    public static ElementKind Of(NarrativeAnchor anchor, PartyMember viewer) => anchor switch
    {
        SyntheticAreaObservationObject => ElementKind.Way,
        SyntheticObservationObject { PointOfInterest: ConnectorPointOfInterest } => ElementKind.Way,
        SyntheticNpcObservationObject npc => IsHostile(npc.SceneNpc, viewer) ? ElementKind.Hostile : ElementKind.Person,
        _ => ElementKind.Thing,
    };

    /// <summary>Whether <paramref name="npc"/> counts <paramref name="viewer"/> an enemy. Shallow wildlife never does.</summary>
    public static bool IsHostile(SceneNpc npc, PartyMember viewer)
        => npc.Entity is NpcEntity named && named.AffinityTable.IsEnemy(viewer.AffinityKey);

    /// <summary>The symbol of a kind on the map.</summary>
    public static char Glyph(ElementKind kind) => kind switch
    {
        ElementKind.Way     => Config.Symbols.MapWay,
        ElementKind.Person  => Config.Symbols.MapPerson,
        ElementKind.Hostile => Config.Symbols.MapHostile,
        _                   => Config.Symbols.MapThing,
    };

    /// <summary>The kind in a word, as a script names it and a label says it.</summary>
    public static string Word(ElementKind kind) => kind switch
    {
        ElementKind.Way     => "way",
        ElementKind.Person  => "person",
        ElementKind.Hostile => "hostile",
        _                   => "thing",
    };

    /// <summary>The kind a script's word names, or null.</summary>
    public static ElementKind? Parse(string word) => word.ToLowerInvariant() switch
    {
        "way" or "ways"       => ElementKind.Way,
        "person" or "people"  => ElementKind.Person,
        "hostile" or "enemy"  => ElementKind.Hostile,
        "thing" or "things"   => ElementKind.Thing,
        _                     => null,
    };

    /// <summary>
    /// The scene element an observation is of: what a tree is rooted on, since an observation object
    /// may be made afresh (a bed taken out for a sleeper and put back) while the element stays.
    /// </summary>
    public static Element? ElementOf(NarrativeAnchor anchor) => anchor switch
    {
        SyntheticObservationObject poi      => poi.PointOfInterest,
        SyntheticNpcObservationObject npc   => npc.SceneNpc,
        SyntheticAreaObservationObject area => area.Area,
        SyntheticItemObservationObject item => item.Item,
        _                                   => null,
    };
}
