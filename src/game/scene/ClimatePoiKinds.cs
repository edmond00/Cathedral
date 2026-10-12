using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The kinds of thing the hot and cold country holds (see ClimateRule).
//
//  The same move as PoiKinds.cs: a kind is a type, its lemma derived from the class name and fixed
//  here, so a lesson condition naming one is checked by the compiler. Split out only to keep the
//  hot and cold country's vocabulary in one place; reflection finds these exactly as it finds the
//  temperate ones.
// ─────────────────────────────────────────────────────────────────────────────

// ── Desert ────────────────────────────────────────────────────────────

/// <summary>A cactus.</summary>
public class CactusPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cactus";

    public CactusPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A spring.</summary>
public class SpringPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "spring";

    public SpringPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crust.</summary>
public class CrustPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crust";

    public CrustPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bones.</summary>
public class BonesPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bones";

    public BonesPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crystal.</summary>
public class CrystalPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crystal";

    public CrystalPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A burrow.</summary>
public class BurrowPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "burrow";

    public BurrowPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mirage.</summary>
public class MiragePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mirage";

    public MiragePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stele.</summary>
public class StelePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stele";

    public StelePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── Hot steppe ────────────────────────────────────────────────────────

/// <summary>A mound.</summary>
public class MoundPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mound";

    public MoundPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tussock.</summary>
public class TussockPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tussock";

    public TussockPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── Jungle ────────────────────────────────────────────────────────────

/// <summary>A liana.</summary>
public class LianaPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "liana";

    public LianaPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bamboo.</summary>
public class BambooPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bamboo";

    public BambooPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bromeliad.</summary>
public class BromeliadPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bromeliad";

    public BromeliadPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pitcher.</summary>
public class PitcherPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pitcher";

    public PitcherPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── Canyon ────────────────────────────────────────────────────────────

/// <summary>A petroglyph.</summary>
public class PetroglyphPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "petroglyph";

    public PetroglyphPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hoodoo.</summary>
public class HoodooPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hoodoo";

    public HoodooPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A seep.</summary>
public class SeepPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "seep";

    public SeepPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>An arch.</summary>
public class ArchPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "arch";

    public ArchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A yucca.</summary>
public class YuccaPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "yucca";

    public YuccaPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── Sea ice ───────────────────────────────────────────────────────────

/// <summary>A hole.</summary>
public class HolePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hole";

    public HolePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crack.</summary>
public class CrackPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crack";

    public CrackPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A drift.</summary>
public class DriftPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "drift";

    public DriftPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── Glacier ───────────────────────────────────────────────────────────

/// <summary>A serac.</summary>
public class SeracPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "serac";

    public SeracPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A moulin.</summary>
public class MoulinPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "moulin";

    public MoulinPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crevasse.</summary>
public class CrevassePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crevasse";

    public CrevassePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── Cold steppe ───────────────────────────────────────────────────────

/// <summary>A polygon.</summary>
public class PolygonPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "polygon";

    public PolygonPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
