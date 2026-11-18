using System.Collections.Generic;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// The place a run opens in: the foot of a lone tree on an open plain, where the protagonist sits
/// spent. It is built twice and is one place — first as the ground the childhood is remembered on
/// (<see cref="Reminescence.ReminescenceSceneFactory"/>), then as the scene the protagonist gets up
/// from (<see cref="GetUpSceneFactory"/>) — so both name it alike and stand the same things around
/// the body. While remembering, those things are only there; getting up, they may be looked at.
/// </summary>
public static class RestingPlace
{
    /// <summary>The one area's name, on its card in both phases.</summary>
    public const string AreaName = "Under the Tree";

    /// <summary>The name of the ground around it, on the panel that holds the card.</summary>
    public const string SectionName = "The Plain";

    /// <summary>What the area is, read from where the body sits.</summary>
    public static readonly IReadOnlyList<string> Descriptions = new[]
    {
        "A lone tree on a flat, grey plain. You sit with your back against the bark, " +
        "the road stretching ahead of you in both directions, your body unwilling to move.",
    };

    public static readonly string[] Moods = { "still", "heavy", "silent", "desolate", "open", "vast", "grey" };

    /// <summary>What stands around the body, fresh instances for each scene built.</summary>
    public static IEnumerable<PointOfInterest> Scenery()
    {
        yield return new TreePointOfInterest("Lone Tree", new List<string>
        {
            "a single old tree, its bark rough against my back, the only upright thing for a long way in any direction",
        }, moods: new[] { "old", "gnarled", "patient", "wind-bent" }, isNatural: true);

        yield return new RoadPointOfInterest("Road", new List<string>
        {
            "a pale road of packed earth running past the tree and on, both ways, until the plain swallows it",
        }, moods: new[] { "pale", "empty", "long", "rutted" });

        yield return new HorizonPointOfInterest("Horizon", new List<string>
        {
            "a flat line where the grey plain meets a sky of nearly the same grey, with nothing standing on it",
        }, moods: new[] { "far", "flat", "grey", "bare" }, isNatural: true);
    }
}

/// <summary>A road, seen from beside it.</summary>
public class RoadPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "road";

    public RoadPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>The horizon of open ground.</summary>
public class HorizonPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "horizon";

    public HorizonPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
