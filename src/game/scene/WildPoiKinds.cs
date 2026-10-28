using System.Collections.Generic;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The point-of-interest kinds the wilderness overhaul added, on the contract of PoiKinds.cs: the
//  lemma is derived from the class name and fixed here, and content gates on the type.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Falling water: a fall, a cascade, a spout off a ledge.</summary>
public class FallsPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "falls";

    public FallsPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A herdsman's hut of dry stone, roofed with turf, used for a summer and left.</summary>
public class BothyPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bothy";

    public BothyPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A standing stone, alone or one of a ring, raised by people nobody remembers.</summary>
public class MonolithPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "monolith";

    public MonolithPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
