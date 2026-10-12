using System.Collections.Generic;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The kinds of area the hot and cold country is made of (see ClimateRule).
//
//  The same move as AreaKinds.cs: a kind is a type, its lemma derived from the class name and fixed
//  here, so a lesson condition naming one is checked by the compiler. Split out only to keep the
//  hot and cold country's vocabulary in one place; reflection finds these exactly as it finds the
//  temperate ones.
// ─────────────────────────────────────────────────────────────────────────────

// ── Desert ────────────────────────────────────────────────────────────

/// <summary>A dune, in the desert.</summary>
public class DuneArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dune";

    public DuneArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A reg, in the desert.</summary>
public class RegArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "reg";

    public RegArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pan, in the desert.</summary>
public class PanArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pan";

    public PanArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A wadi, in the desert.</summary>
public class WadiArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wadi";

    public WadiArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An oasis, in the desert.</summary>
public class OasisArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "oasis";

    public OasisArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Hot steppe ────────────────────────────────────────────────────────

/// <summary>A savanna, in the hot steppe.</summary>
public class SavannaArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "savanna";

    public SavannaArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A thornbrush, in the hot steppe.</summary>
public class ThornbrushArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "thornbrush";

    public ThornbrushArea(string displayName, string contextDescription, string transitionDescription,
                          List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A kopje, in the hot steppe.</summary>
public class KopjeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "kopje";

    public KopjeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A waterhole, in the hot steppe.</summary>
public class WaterholeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "waterhole";

    public WaterholeArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tableland, in the hot steppe.</summary>
public class TablelandArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tableland";

    public TablelandArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A burn, in the hot steppe.</summary>
public class BurnArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "burn";

    public BurnArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Jungle ────────────────────────────────────────────────────────────

/// <summary>An understorey, in the jungle.</summary>
public class UnderstoreyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "understorey";

    public UnderstoreyArea(string displayName, string contextDescription, string transitionDescription,
                           List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A gap, in the jungle.</summary>
public class GapArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gap";

    public GapArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A riverbank, in the jungle.</summary>
public class RiverbankArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "riverbank";

    public RiverbankArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A mangrove, in the jungle.</summary>
public class MangroveArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mangrove";

    public MangroveArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A swamp, in the jungle.</summary>
public class SwampArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "swamp";

    public SwampArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ruin, in the jungle.</summary>
public class RuinArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ruin";

    public RuinArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Canyon ────────────────────────────────────────────────────────────

/// <summary>A rim, in the canyon.</summary>
public class RimArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rim";

    public RimArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A wash, in the canyon.</summary>
public class WashArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wash";

    public WashArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A slot, in the canyon.</summary>
public class SlotArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "slot";

    public SlotArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A mesa, in the canyon.</summary>
public class MesaArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mesa";

    public MesaArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A seep, in the canyon.</summary>
public class SeepArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "seep";

    public SeepArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Sea ice ───────────────────────────────────────────────────────────

/// <summary>A floe, in the sea ice.</summary>
public class FloeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "floe";

    public FloeArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hummock, in the sea ice.</summary>
public class HummockArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hummock";

    public HummockArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A lead, in the sea ice.</summary>
public class LeadArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lead";

    public LeadArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A berg, in the sea ice.</summary>
public class BergArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "berg";

    public BergArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A polynya, in the sea ice.</summary>
public class PolynyaArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "polynya";

    public PolynyaArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Glacier ───────────────────────────────────────────────────────────

/// <summary>A crevasse, in the glacier.</summary>
public class CrevasseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crevasse";

    public CrevasseArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An icefall, in the glacier.</summary>
public class IcefallArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "icefall";

    public IcefallArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A moraine, in the glacier.</summary>
public class MoraineArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "moraine";

    public MoraineArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A nunatak, in the glacier.</summary>
public class NunatakArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "nunatak";

    public NunatakArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A grotto, in the glacier.</summary>
public class GrottoArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "grotto";

    public GrottoArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Snowfield ─────────────────────────────────────────────────────────

/// <summary>A snowpack, in the snowfield.</summary>
public class SnowpackArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "snowpack";

    public SnowpackArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A drift, in the snowfield.</summary>
public class DriftArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "drift";

    public DriftArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cornice, in the snowfield.</summary>
public class CorniceArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cornice";

    public CorniceArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A chute, in the snowfield.</summary>
public class ChuteArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chute";

    public ChuteArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A krummholz, in the snowfield.</summary>
public class KrummholzArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "krummholz";

    public KrummholzArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tarn, in the snowfield.</summary>
public class TarnArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tarn";

    public TarnArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

// ── Cold steppe ───────────────────────────────────────────────────────

/// <summary>A tundra, in the cold steppe.</summary>
public class TundraArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tundra";

    public TundraArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tussock, in the cold steppe.</summary>
public class TussockArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tussock";

    public TussockArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pingo, in the cold steppe.</summary>
public class PingoArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pingo";

    public PingoArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A thaw, in the cold steppe.</summary>
public class ThawArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "thaw";

    public ThawArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A barrens, in the cold steppe.</summary>
public class BarrensArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "barrens";

    public BarrensArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>An esker, in the cold steppe.</summary>
public class EskerArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "esker";

    public EskerArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}
