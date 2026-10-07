using System.Collections.Generic;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The kinds of area the settled country is made of: great buildings, towns, farmland, stock.
//
//  Generated from a list (see the settlement work) in the same shape as AreaKinds.cs: a kind is a type,
//  its lemma derived from the class name and fixed here, so a lesson condition naming one is checked
//  by the compiler and reflection finds every one of them.
// ─────────────────────────────────────────────────────────────────────────────

// ── great buildings (EdificeFactory) ──────────────────────────────────────

/// <summary>A gatehouse.</summary>
public class GatehouseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gatehouse";

    public GatehouseArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A corridor.</summary>
public class CorridorArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "corridor";

    public CorridorArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A refectory.</summary>
public class RefectoryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "refectory";

    public RefectoryArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bathhouse.</summary>
public class BathhouseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bathhouse";

    public BathhouseArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A latrine.</summary>
public class LatrineArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "latrine";

    public LatrineArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cell.</summary>
public class CellArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cell";

    public CellArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A chapel.</summary>
public class ChapelArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chapel";

    public ChapelArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A library.</summary>
public class LibraryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "library";

    public LibraryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A scriptorium.</summary>
public class ScriptoriumArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "scriptorium";

    public ScriptoriumArea(string displayName, string contextDescription, string transitionDescription,
                           List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An armoury.</summary>
public class ArmouryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "armoury";

    public ArmouryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A guardroom.</summary>
public class GuardroomArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "guardroom";

    public GuardroomArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An infirmary.</summary>
public class InfirmaryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "infirmary";

    public InfirmaryArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cellar.</summary>
public class CellarArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cellar";

    public CellarArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A crypt.</summary>
public class CryptArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crypt";

    public CryptArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A treasury.</summary>
public class TreasuryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "treasury";

    public TreasuryArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A solar.</summary>
public class SolarArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "solar";

    public SolarArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A classroom.</summary>
public class ClassroomArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "classroom";

    public ClassroomArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A brewhouse.</summary>
public class BrewhouseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "brewhouse";

    public BrewhouseArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A laundry.</summary>
public class LaundryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "laundry";

    public LaundryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A dungeon.</summary>
public class DungeonArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dungeon";

    public DungeonArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A battlements.</summary>
public class BattlementsArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "battlements";

    public BattlementsArea(string displayName, string contextDescription, string transitionDescription,
                           List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cloister.</summary>
public class CloisterArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cloister";

    public CloisterArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A sanctum.</summary>
public class SanctumArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sanctum";

    public SanctumArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A countinghouse.</summary>
public class CountinghouseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "countinghouse";

    public CountinghouseArea(string displayName, string contextDescription, string transitionDescription,
                             List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A gatefront.</summary>
public class GatefrontArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gatefront";

    public GatefrontArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A wallfoot.</summary>
public class WallfootArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wallfoot";

    public WallfootArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A chancery.</summary>
public class ChanceryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chancery";

    public ChanceryArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A scullery.</summary>
public class SculleryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "scullery";

    public SculleryArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A wardrobe.</summary>
public class WardrobeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wardrobe";

    public WardrobeArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An archive.</summary>
public class ArchiveArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "archive";

    public ArchiveArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An observatory.</summary>
public class ObservatoryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "observatory";

    public ObservatoryArea(string displayName, string contextDescription, string transitionDescription,
                           List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A lecture.</summary>
public class LectureArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lecture";

    public LectureArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── towns and cities (CityLayout) ─────────────────────────────────────────

/// <summary>A street.</summary>
public class StreetArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "street";

    public StreetArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An alley.</summary>
public class AlleyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "alley";

    public AlleyArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A quay.</summary>
public class QuayArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "quay";

    public QuayArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A dock.</summary>
public class DockArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dock";

    public DockArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shipyard.</summary>
public class ShipyardArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shipyard";

    public ShipyardArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A gateway.</summary>
public class GatewayArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gateway";

    public GatewayArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ropewalk.</summary>
public class RopewalkArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ropewalk";

    public RopewalkArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A fishmarket.</summary>
public class FishmarketArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fishmarket";

    public FishmarketArea(string displayName, string contextDescription, string transitionDescription,
                          List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── farmland (agriculture factories) ──────────────────────────────────────

/// <summary>A strip.</summary>
public class StripArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "strip";

    public StripArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A threshing.</summary>
public class ThreshingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "threshing";

    public ThreshingArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A barn.</summary>
public class BarnArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "barn";

    public BarnArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A terrace.</summary>
public class TerraceArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "terrace";

    public TerraceArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A paddy.</summary>
public class PaddyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "paddy";

    public PaddyArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A plantation.</summary>
public class PlantationArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "plantation";

    public PlantationArea(string displayName, string contextDescription, string transitionDescription,
                          List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A vineyard.</summary>
public class VineyardArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "vineyard";

    public VineyardArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A press.</summary>
public class PressArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "press";

    public PressArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A drying.</summary>
public class DryingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "drying";

    public DryingArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A packing.</summary>
public class PackingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "packing";

    public PackingArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ditchside.</summary>
public class DitchsideArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ditchside";

    public DitchsideArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hedgebank.</summary>
public class HedgebankArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hedgebank";

    public HedgebankArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A windbreak.</summary>
public class WindbreakArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "windbreak";

    public WindbreakArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cellarway.</summary>
public class CellarwayArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cellarway";

    public CellarwayArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A spawnroom.</summary>
public class SpawnroomArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "spawnroom";

    public SpawnroomArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A forcing.</summary>
public class ForcingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "forcing";

    public ForcingArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A teahouse.</summary>
public class TeahouseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "teahouse";

    public TeahouseArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A canal.</summary>
public class CanalArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "canal";

    public CanalArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A seedbed.</summary>
public class SeedbedArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "seedbed";

    public SeedbedArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── stock (livestock factories) ───────────────────────────────────────────

/// <summary>A paddock.</summary>
public class PaddockArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "paddock";

    public PaddockArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A byre.</summary>
public class ByreArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "byre";

    public ByreArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A fold.</summary>
public class FoldArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fold";

    public FoldArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A warren.</summary>
public class WarrenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "warren";

    public WarrenArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A corral.</summary>
public class CorralArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "corral";

    public CorralArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A dairy.</summary>
public class DairyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dairy";

    public DairyArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shearing.</summary>
public class ShearingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shearing";

    public ShearingArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A lambing.</summary>
public class LambingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lambing";

    public LambingArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tackroom.</summary>
public class TackroomArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tackroom";

    public TackroomArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A trough.</summary>
public class TroughArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "trough";

    public TroughArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A fodder.</summary>
public class FodderArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fodder";

    public FodderArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── settlements (village, burg, hamlet, townlet, fort) ────────────────────

/// <summary>A rampart.</summary>
public class RampartArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rampart";

    public RampartArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A parade.</summary>
public class ParadeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "parade";

    public ParadeArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An outworks.</summary>
public class OutworksArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "outworks";

    public OutworksArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

