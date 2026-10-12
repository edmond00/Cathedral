using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The kinds of thing the settled country holds.
//
//  Generated from a list (see the settlement work) in the same shape as PoiKinds.cs: a kind is a type,
//  its lemma derived from the class name and fixed here, so a lesson condition naming one is checked
//  by the compiler and reflection finds every one of them.
// ─────────────────────────────────────────────────────────────────────────────

// ── great buildings ───────────────────────────────────────────────────────

/// <summary>An altar.</summary>
public class AltarPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "altar";

    public AltarPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A lectern.</summary>
public class LecternPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lectern";

    public LecternPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A font.</summary>
public class FontPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "font";

    public FontPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A reliquary.</summary>
public class ReliquaryPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "reliquary";

    public ReliquaryPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A shrine.</summary>
public class ShrinePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shrine";

    public ShrinePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A throne.</summary>
public class ThronePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "throne";

    public ThronePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tapestry.</summary>
public class TapestryPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tapestry";

    public TapestryPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A banner.</summary>
public class BannerPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "banner";

    public BannerPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A weaponrack.</summary>
public class WeaponrackPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "weaponrack";

    public WeaponrackPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>An armourstand.</summary>
public class ArmourstandPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "armourstand";

    public ArmourstandPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                      string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bathtub.</summary>
public class BathtubPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bathtub";

    public BathtubPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A basin.</summary>
public class BasinPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "basin";

    public BasinPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bookcase.</summary>
public class BookcasePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bookcase";

    public BookcasePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A desk.</summary>
public class DeskPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "desk";

    public DeskPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A strongbox.</summary>
public class StrongboxPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "strongbox";

    public StrongboxPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A ledger.</summary>
public class LedgerPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ledger";

    public LedgerPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cot.</summary>
public class CotPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cot";

    public CotPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bunk.</summary>
public class BunkPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bunk";

    public BunkPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A brazier.</summary>
public class BrazierPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "brazier";

    public BrazierPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tomb.</summary>
public class TombPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tomb";

    public TombPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>An ossuary.</summary>
public class OssuaryPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ossuary";

    public OssuaryPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A chain.</summary>
public class ChainPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chain";

    public ChainPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cage.</summary>
public class CagePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cage";

    public CagePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A battlement.</summary>
public class BattlementPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "battlement";

    public BattlementPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A telescope.</summary>
public class TelescopePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "telescope";

    public TelescopePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A chalkboard.</summary>
public class ChalkboardPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chalkboard";

    public ChalkboardPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>An orrery.</summary>
public class OrreryPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "orrery";

    public OrreryPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A map.</summary>
public class MapPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "map";

    public MapPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A censer.</summary>
public class CenserPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "censer";

    public CenserPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>An icon.</summary>
public class IconPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "icon";

    public IconPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── towns and cities ──────────────────────────────────────────────────────

/// <summary>A bollard.</summary>
public class BollardPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bollard";

    public BollardPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crane.</summary>
public class CranePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crane";

    public CranePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hull.</summary>
public class HullPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hull";

    public HullPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A coil.</summary>
public class CoilPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "coil";

    public CoilPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crate.</summary>
public class CratePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crate";

    public CratePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A signboard.</summary>
public class SignboardPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "signboard";

    public SignboardPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A fountain.</summary>
public class FountainPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fountain";

    public FountainPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A statue.</summary>
public class StatuePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "statue";

    public StatuePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A gallows.</summary>
public class GallowsPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gallows";

    public GallowsPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pillory.</summary>
public class PilloryPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pillory";

    public PilloryPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A counter.</summary>
public class CounterPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "counter";

    public CounterPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>An awning.</summary>
public class AwningPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "awning";

    public AwningPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cistern.</summary>
public class CisternPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cistern";

    public CisternPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── farmland ──────────────────────────────────────────────────────────────

/// <summary>A furrow.</summary>
public class FurrowPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "furrow";

    public FurrowPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A sheaf.</summary>
public class SheafPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sheaf";

    public SheafPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stook.</summary>
public class StookPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stook";

    public StookPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A vine.</summary>
public class VinePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "vine";

    public VinePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A trellis.</summary>
public class TrellisPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "trellis";

    public TrellisPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A press.</summary>
public class PressPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "press";

    public PressPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A vat.</summary>
public class VatPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "vat";

    public VatPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A sluice.</summary>
public class SluicePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sluice";

    public SluicePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A seedling.</summary>
public class SeedlingPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "seedling";

    public SeedlingPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mushroombed.</summary>
public class MushroombedPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mushroombed";

    public MushroombedPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                      string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A basket.</summary>
public class BasketPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "basket";

    public BasketPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A ladder.</summary>
public class LadderPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ladder";

    public LadderPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tea.</summary>
public class TeaPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tea";

    public TeaPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cane.</summary>
public class CanePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cane";

    public CanePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A palm.</summary>
public class PalmPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "palm";

    public PalmPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cottonboll.</summary>
public class CottonbollPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cottonboll";

    public CottonbollPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hopbine.</summary>
public class HopbinePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hopbine";

    public HopbinePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A drysheaf.</summary>
public class DrysheafPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "drysheaf";

    public DrysheafPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A clamp.</summary>
public class ClampPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "clamp";

    public ClampPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

// ── stock ─────────────────────────────────────────────────────────────────

/// <summary>A manger.</summary>
public class MangerPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "manger";

    public MangerPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A saddle.</summary>
public class SaddlePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "saddle";

    public SaddlePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A harness.</summary>
public class HarnessPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "harness";

    public HarnessPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A shears.</summary>
public class ShearsPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shears";

    public ShearsPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pen.</summary>
public class PenPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pen";

    public PenPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hutch.</summary>
public class HutchPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hutch";

    public HutchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A coop.</summary>
public class CoopPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "coop";

    public CoopPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A fleece.</summary>
public class FleecePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fleece";

    public FleecePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A churnstand.</summary>
public class ChurnstandPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "churnstand";

    public ChurnstandPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A branding.</summary>
public class BrandingPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "branding";

    public BrandingPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
