using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// A seam of metal in the rock. Yields ore to <see cref="Verbs.MineVerb"/> and nothing to bare
/// hands.
///
/// <para>Unlike a <see cref="PointOfInterest"/> with items in it — which GATHER or GRAB empties for
/// free — an extraction point holds its items behind a tool. The items are still declared the normal
/// way and still deplete and regenerate the normal way; the only difference is that the verb which
/// takes them declares <c>ReferenceToolIds</c>, so <c>RequiredToolRule</c> refuses the attempt
/// bare-handed and the item-use critic judges whatever the player did bring.</para>
/// </summary>
public class OreVeinPointOfInterest : PointOfInterest
{
    public OreVeinPointOfInterest(string displayName, string referenceLemma, List<string> descriptions,
                                  List<ItemElement>? items = null, string[]? moods = null)
        : base(displayName, referenceLemma, descriptions, items, moods, isNatural: true)
    {
        Senses = SensoryProfile.Beautiful;
        VerbModiMentis = new Dictionary<string, string>
        {
            ["examine"]     = "stonework",
            ["contemplate"] = "aesthetic",
        };
    }
}

/// <summary>Ground worth turning over with a shovel: a bank, a spoil heap, a peat cut, a sand flat.</summary>
public class DiggableGroundPointOfInterest : PointOfInterest
{
    public DiggableGroundPointOfInterest(string displayName, string referenceLemma, List<string> descriptions,
                                         List<ItemElement>? items = null, string[]? moods = null)
        : base(displayName, referenceLemma, descriptions, items, moods, isNatural: true)
    {
        Senses = SensoryProfile.Odorous;
        VerbModiMentis = new Dictionary<string, string>
        {
            ["examine"] = "tillage",
            ["smell"]   = "scenting",
        };
    }
}

/// <summary>
/// Something inside a building that can be broken up on purpose: a chest, a table, a loom, a barrel.
///
/// <para>Breaking one replaces it with its wrecked self, which holds the salvage. That replacement is
/// the point — the room afterwards shows what was done to it, and the salvage is only reachable
/// through the wreckage, so the crime leaves evidence you have to stand next to.</para>
/// </summary>
public class BreakablePointOfInterest : PointOfInterest
{
    /// <summary>The wreck this becomes, carrying whatever can be picked out of it.</summary>
    public PointOfInterest BrokenVariant { get; }

    public BreakablePointOfInterest(string displayName, string referenceLemma, List<string> descriptions,
                                    PointOfInterest brokenVariant, string[]? moods = null,
                                    IReadOnlyDictionary<string, string>? verbModiMentis = null)
        : base(displayName, referenceLemma, descriptions, items: null, moods: moods)
    {
        BrokenVariant  = brokenVariant;
        Senses         = SensoryProfile.Examinable;
        if (verbModiMentis != null) VerbModiMentis = verbModiMentis;
    }
}
