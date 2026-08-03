using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>What sort of water a <see cref="WaterCrossingPointOfInterest"/> is.</summary>
public enum WaterKind
{
    /// <summary>Moving water with a pull to it. The current is the difficulty.</summary>
    River,

    /// <summary>A small fast watercourse. Narrow, cold, awkward rather than dangerous.</summary>
    Creek,

    /// <summary>Still deep water. No current, but nothing to hold either.</summary>
    Pond,

    /// <summary>Sea water between two headlands. Cold and swelling.</summary>
    Cove,

    /// <summary>A dug channel feeding a mill. Fast, walled and deeper than it looks.</summary>
    MillLeat,
}

/// <summary>
/// Water lying between two areas, to be swum rather than walked. Targeted by
/// <see cref="Verbs.SwimAcrossVerb"/>, and — once a rod or net is in hand — by
/// <see cref="Verbs.FishVerb"/>, which is the point of putting it in a scene rather than a dry
/// crossing: one object, two quite different things to do with it.
///
/// <para>Gate connector: no <c>AreaGraph</c> edge beside it.</para>
/// </summary>
public class WaterCrossingPointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "water";

    /// <summary>What sort of water this is.</summary>
    public WaterKind Kind { get; }

    /// <summary>
    /// Whether fish are actually in it. Not every stretch of water holds any, and a pond that never
    /// gives a fish up is better than one where fishing always works.
    /// </summary>
    public bool HoldsFish { get; init; } = true;

    public WaterCrossingPointOfInterest(
        Area areaA,
        Area areaB,
        WaterKind kind,
        string displayName,
        List<string> descriptions,
        string[]? moods = null,
        List<ItemElement>? items = null)
        : base(areaA, areaB, displayName, LemmaFor(kind), descriptions, moods: moods, items: items,
               isNatural: true)
    {
        Kind = kind;
    }

    /// <summary>
    /// How hard this is to get across. A creek is a wetting; a cove in a swell is a genuine risk.
    /// </summary>
    public int Difficulty => Kind switch
    {
        WaterKind.Creek    => 3,
        WaterKind.MillLeat => 4,
        WaterKind.Pond     => 5,
        WaterKind.River    => 6,
        WaterKind.Cove     => 7,
        _                  => 5,
    };

    /// <summary>
    /// What a failed swim costs. Skewed away from fractures — you do not break a leg in open water —
    /// and towards being battered, half-drowned and turned back.
    /// </summary>
    public IReadOnlyList<Wound?> FailurePenalties() => Kind switch
    {
        WaterKind.Creek => new Wound?[] { null, null, null, null, null, new ContusionWound() },
        WaterKind.Cove  => new Wound?[]
        {
            null, null,
            new ContusionWound(), new ContusionWound(), new ConcussionsWound(), new BrokenRibsWound(),
        },
        _ => new Wound?[] { null, null, null, new ContusionWound(), new ConcussionsWound() },
    };

    private static string LemmaFor(WaterKind kind) => kind switch
    {
        WaterKind.River    => "river",
        WaterKind.Creek    => "creek",
        WaterKind.Pond     => "pond",
        WaterKind.Cove     => "cove",
        WaterKind.MillLeat => "leat",
        _                  => "water",
    };
}
