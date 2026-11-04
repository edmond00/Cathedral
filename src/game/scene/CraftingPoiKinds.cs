using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative.Crafting;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  Crafting stations: the kinds of point of interest a crafting modus mentis works at.
//
//  A station is a TYPE carrying [CraftingStation(material)], and it works exactly one material: what is
//  broken down there gives back that material only, and what is made there is paid for in it. Several
//  stations may work one material (the loom and the spinning wheel both work fibre); no station works two.
//
//  The generic kinds a station was before keep working as they did: a spinning wheel is still a wheel and
//  a tanning pit still a vat, so nothing that gated on WheelPointOfInterest or VatPointOfInterest stops
//  finding them. The subclass is what tells a potter's wheel from the pump wheel of a mine.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Marks a point-of-interest kind as a crafting station working one material.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CraftingStationAttribute : Attribute
{
    public CraftMaterial Material { get; }
    public CraftingStationAttribute(CraftMaterial material) => Material = material;
}

/// <summary>Reading the station kinds.</summary>
public static class CraftingStations
{
    private static readonly Dictionary<Type, CraftMaterial?> _material = new();

    /// <summary>The material a station kind works, or null when the type is no station.</summary>
    public static CraftMaterial? MaterialOf(Type poiType)
    {
        if (_material.TryGetValue(poiType, out var m)) return m;
        m = poiType.GetCustomAttribute<CraftingStationAttribute>(inherit: false)?.Material;
        _material[poiType] = m;
        return m;
    }

    public static CraftMaterial? MaterialOf(Element element) => element is PointOfInterest poi ? MaterialOf(poi.GetType()) : null;

    public static bool IsStation(Element element) => MaterialOf(element) != null;

    /// <summary>Every station kind in the assembly.</summary>
    public static IReadOnlyList<Type> All => _all ??= Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && typeof(PointOfInterest).IsAssignableFrom(t)
                 && t.GetCustomAttribute<CraftingStationAttribute>(inherit: false) != null)
        .OrderBy(t => t.Name).ToList();
    private static List<Type>? _all;

    /// <summary>"anvil", "potter's wheel" — a station kind named for sentences, from its type.</summary>
    public static string Noun(Type stationType) => stationType.Name switch
    {
        nameof(AnvilPointOfInterest)        => "anvil",
        nameof(WorkbenchPointOfInterest)    => "workbench",
        nameof(LoomPointOfInterest)         => "loom",
        nameof(ForgeHearthPointOfInterest)  => "forge hearth",
        nameof(SpinningWheelPointOfInterest)=> "spinning wheel",
        nameof(PottersWheelPointOfInterest) => "potter's wheel",
        nameof(TanningPitPointOfInterest)   => "tanning pit",
        nameof(TallowVatPointOfInterest)    => "tallow vat",
        nameof(CuttingTablePointOfInterest) => "cutting table",
        nameof(LeatherBenchPointOfInterest) => "leather bench",
        nameof(GoldsmithsBenchPointOfInterest) => "goldsmith's bench",
        nameof(WritingDeskPointOfInterest)  => "writing desk",
        nameof(CarvingBenchPointOfInterest) => "carving bench",
        nameof(MasonsBankerPointOfInterest) => "mason's banker",
        _                                   => stationType.Name.Replace("PointOfInterest", "").ToLowerInvariant(),
    };
}

/// <summary>The hearth of a forge, where ore is brought to a bloom and run off as bar iron.</summary>
[CraftingStation(CraftMaterial.Ore)]
public class ForgeHearthPointOfInterest : PointOfInterest
{
    public const string Lemma = "hearth";

    public ForgeHearthPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                      string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A spinning wheel: fibre drawn out and twisted into thread.</summary>
[CraftingStation(CraftMaterial.Fibre)]
public class SpinningWheelPointOfInterest : WheelPointOfInterest
{
    public SpinningWheelPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                        string[]? moods = null, bool isNatural = false)
        : base(displayName, descriptions, items, moods, isNatural) { }
}

/// <summary>A potter's wheel: clay thrown into vessels.</summary>
[CraftingStation(CraftMaterial.Clay)]
public class PottersWheelPointOfInterest : WheelPointOfInterest
{
    public PottersWheelPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, descriptions, items, moods, isNatural) { }
}

/// <summary>Tanning pits: hides soaked into leather.</summary>
[CraftingStation(CraftMaterial.Hide)]
public class TanningPitPointOfInterest : VatPointOfInterest
{
    public TanningPitPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                     string[]? moods = null, bool isNatural = false)
        : base(displayName, descriptions, items, moods, isNatural) { }
}

/// <summary>A tallow vat: fat rendered and poured into candles and soap.</summary>
[CraftingStation(CraftMaterial.Fat)]
public class TallowVatPointOfInterest : VatPointOfInterest
{
    public TallowVatPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, descriptions, items, moods, isNatural) { }
}

/// <summary>A cutting table: cloth cut and sewn into garments.</summary>
[CraftingStation(CraftMaterial.Cloth)]
public class CuttingTablePointOfInterest : TablePointOfInterest
{
    public CuttingTablePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, descriptions, items, moods, isNatural) { }
}

/// <summary>A leather bench: leather cut, stitched and shaped.</summary>
[CraftingStation(CraftMaterial.Leather)]
public class LeatherBenchPointOfInterest : PointOfInterest
{
    public const string Lemma = "bench";

    public LeatherBenchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A goldsmith's bench: fine metal cast, chased, engraved and set — rings, vessels, instruments.</summary>
[CraftingStation(CraftMaterial.Metal)]
public class GoldsmithsBenchPointOfInterest : PointOfInterest
{
    public const string Lemma = "bench";

    public GoldsmithsBenchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A writing desk: parchment written on, folded and bound — letters, writs, books.</summary>
[CraftingStation(CraftMaterial.Leather)]
public class WritingDeskPointOfInterest : PointOfInterest
{
    public const string Lemma = "desk";

    public WritingDeskPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A carving bench: bone and horn sawn, carved and polished.</summary>
[CraftingStation(CraftMaterial.Bone)]
public class CarvingBenchPointOfInterest : PointOfInterest
{
    public const string Lemma = "bench";

    public CarvingBenchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mason's banker: the stone bench a mason dresses and carves stone on.</summary>
[CraftingStation(CraftMaterial.Stone)]
public class MasonsBankerPointOfInterest : PointOfInterest
{
    public const string Lemma = "banker";

    public MasonsBankerPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                       string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
