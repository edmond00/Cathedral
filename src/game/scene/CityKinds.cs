using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The shops and shop furniture of a dense city (CityTradeSubfactory): the cobbler's, tailor's,
//  chandler's, butcher's, tanner's, potter's, apothecary's and barber's. Same shape as
//  SettledAreaKinds.cs and SettledPoiKinds.cs: a kind is a type, its lemma derived from the class name
//  and fixed here.
// ─────────────────────────────────────────────────────────────────────────────

// ── areas ─────────────────────────────────────────────────────────────────

/// <summary>A cobbler's shop.</summary>
public class CobbleryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cobblery";

    public CobbleryArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tailor's shop.</summary>
public class TailoryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tailory";

    public TailoryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A chandler's shop.</summary>
public class ChandleryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chandlery";

    public ChandleryArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A butcher's shop.</summary>
public class ButcheryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "butchery";

    public ButcheryArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tannery yard.</summary>
public class TanneryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tannery";

    public TanneryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A potter's workshop.</summary>
public class PotteryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pottery";

    public PotteryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An apothecary's shop.</summary>
public class DispensaryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dispensary";

    public DispensaryArea(string displayName, string contextDescription, string transitionDescription,
                          List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A barber-surgeon's shop.</summary>
public class BarbershopArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "barbershop";

    public BarbershopArea(string displayName, string contextDescription, string transitionDescription,
                          List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── points of interest ────────────────────────────────────────────────────

/// <summary>A rack of shoemaker's lasts.</summary>
public class LastPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "last";

    public LastPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tailor's dummy.</summary>
public class DummyPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dummy";

    public DummyPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A kiln.</summary>
public class KilnPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "kiln";

    public KilnPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A row of apothecary's jars.</summary>
public class JarPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "jar";

    public JarPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A barber's chair.</summary>
public class ChairPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chair";

    public ChairPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A wash tub.</summary>
public class TubPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tub";

    public TubPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hanging carcass.</summary>
public class CarcassPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "carcass";

    public CarcassPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tanning pit.</summary>
public class PitPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pit";

    public PitPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
