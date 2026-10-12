using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Settled;
using Cathedral.Game.Scene.Shared;
using Cathedral.Glyph.Microworld;
using Family = Cathedral.Glyph.Microworld.SettlementTable.Family;

namespace Cathedral.Game.Scene.Agriculture;

/// <summary>
/// Builds every agriculture location of the settled country: the eight families of
/// <see cref="SettlementTable.Family"/> — field, orchard, grove, plantation, cellar, garden, paddy and
/// vineyard — each with its own layout, and each parameterised by the crop the location's name gives
/// it (<see cref="CropCatalog"/>). "Radish field" and "turnip field" are one layout growing different
/// things; "radish field" and "orange grove" are different layouts.
///
/// <para><b>Shape.</b> Two or three areas of the crop itself, one or two areas where it is worked —
/// threshed, pressed, dried, packed or stored — and a margin. The crop areas lead, so the first of them
/// is the arrival point. A cellar is the exception: it is entered by its cellarway and is underground
/// throughout.</para>
///
/// <para><b>Crew.</b> Drawn up first and housed to fit, as everywhere: a master who holds the hall and
/// the place (reeve, orchardist, planter, vintner or farmer, by family) and the hands who work it.</para>
/// </summary>
public sealed class AgricultureSceneFactory : SettledSceneFactory
{
    private readonly string _key;
    private readonly Family _family;
    private readonly Crop   _crop;

    private readonly List<Area> _rows = new();
    private readonly List<Area> _work = new();
    private readonly List<Area> _all  = new();
    private Area? _margin;
    private Area? _store;
    private List<NamedNpcArchetype> _roster = new();
    private BuildingResult? _hall, _bunk;
    private List<Area> _beds = new();

    public AgricultureSceneFactory(string key, string? biome = null, string? sessionPath = null) : base(biome, sessionPath)
    {
        _key    = key;
        _family = SettlementTable.FamilyOf(key);
        _crop   = CropCatalog.Of(key);
    }

    protected override string DefaultBiome => FirstBiomeListing(SettlementTable.Agriculture, _key);

    private bool Underground => _family == Family.Cellar;

    protected override void BuildPlace(Random rng, int locationId, Scene scene)
    {
        // ── 1. The crop, the work, the margin ────────────────────────────────
        _rowRng = rng;
        // The crop areas are told apart by a qualifier drawn without replacement — "Upper", "Old" — so
        // no two of them can share a name, and with it the node slug the narration graph keys on.
        int rowCount = rng.Next(2, 4);
        var qualifiers = SampleUniqueIndices(rng, RowPrefixes.Length - 1, rowCount).Select(i => RowPrefixes[i + 1]).ToList();
        for (int i = 0; i < rowCount; i++) _rows.Add(BuildRows(i == 0 ? "" : qualifiers[i]));
        _work.AddRange(BuildWorkAreas(rng));
        _store = _work.FirstOrDefault(IsStore) ?? AddStore();
        if (!Underground) _margin = BuildMargin(rng);

        var crop = new Section(
            SectionName(),
            new() { _crop.RowText },
            seed => Underground ? new RoomsGenerator { Seed = seed } : new NoisyGenerator { Seed = seed, Density = 0.86f });
        foreach (var a in _rows) crop.Areas.Add(a);
        scene.Sections.Add(crop);
        RegisterAll(scene, crop);

        var yard = new Section(
            Underground ? "Cellar Works" : "Working Yard",
            new() { Underground ? "The dark rooms where the crop is tended and stored" : "Where the crop is brought in and worked" },
            seed => Underground ? new RoomsGenerator { Seed = seed } : new GeometricGenerator { Seed = seed });
        foreach (var a in _work) yard.Areas.Add(a);
        if (_margin != null) yard.Areas.Add(_margin);
        scene.Sections.Add(yard);
        RegisterAll(scene, yard);

        // A cellar is entered by its cellarway, which therefore leads; everywhere else the crop does.
        if (Underground) { _all.AddRange(_work); _all.AddRange(_rows); }
        else             { _all.AddRange(_rows); _all.AddRange(_work); _all.Add(_margin!); }

        var shape = Underground ? LayoutShape.Chain : OutdoorLayout.RollShape(rng);
        OutdoorLayout.Connect(scene, _all, shape, Underground ? "Passage" : TrackWord(), rng);

        // ── 2. The crew, then the roof over it ───────────────────────────────
        _roster = BuildRoster(rng);
        var front = Underground ? _work[0] : _work.FirstOrDefault() ?? _rows[0];
        (_hall, _bunk, _beds) = HouseCrew(rng, scene, _roster, front, _all, HallName(), HallNoun(), BunkName());

        // ── 3. Furnishing ────────────────────────────────────────────────────
        var setting = Underground ? FurnitureSubfactory.Setting.Underground : FurnitureSubfactory.Setting.Farmland;
        var outdoors = scene.OutdoorAreas;
        FurnitureSubfactory.AddSitSpots(rng, outdoors, setting);
        FurnitureSubfactory.AddHidingPlaces(rng, outdoors, setting);
        FurnitureSubfactory.AddShortcuts(rng, scene, outdoors, setting);
        FurnitureSubfactory.AddExtractionPoints(rng, outdoors, setting);
        AddRoofLandscapes(scene);

        Console.WriteLine($"AgricultureSceneFactory: Built {_key} — family={_family}, biome={Biome}, "
                        + $"areas={_all.Count}, {_roster.Count} worker(s)");
    }

    // ── Names ─────────────────────────────────────────────────────────────────

    private string SectionName() => _family switch
    {
        Family.Field      => $"{_crop.Title} Field",
        Family.Orchard    => $"{_crop.Title} Orchard",
        Family.Grove      => $"{_crop.Title} Grove",
        Family.Plantation => $"{_crop.Title} Plantation",
        Family.Cellar     => $"{_crop.Title} Cellar",
        Family.Garden     => $"{_crop.Title} Garden",
        Family.Paddy      => "Rice Paddies",
        _                 => "Vineyard",
    };

    private string TrackWord() => _family switch
    {
        Family.Paddy    => "Bund",
        Family.Garden   => "Terrace Steps",
        Family.Vineyard => "Row End",
        _               => "Track",
    };

    private string HallName() => _family switch
    {
        Family.Field      => "Field Longhouse",
        Family.Orchard    => "Orchard House",
        Family.Grove      => "Grove House",
        Family.Plantation => "Planter's House",
        Family.Cellar     => "Cellar House",
        Family.Garden     => "Garden House",
        Family.Paddy      => "Paddy House",
        _                 => "Vintner's House",
    };

    private string HallNoun() => _family switch
    {
        Family.Plantation or Family.Garden or Family.Paddy => "planter's house",
        Family.Vineyard                                    => "press-house",
        _                                                  => "longhouse",
    };

    private string BunkName() => _family is Family.Plantation or Family.Garden or Family.Paddy ? "Pickers' Barracks" : "Bunkhouse";

    // ── The crop ──────────────────────────────────────────────────────────────

    private Random? _rowRng;

    private static readonly string[] RowPrefixes = { "", "Upper ", "Lower ", "Old ", "New ", "Far " };

    private Area BuildRows(string prefix)
    {
        var rng = _rowRng!;
        var (noun, ctx) = _family switch
        {
            Family.Field      => ("Strip",      "walking the strip"),
            Family.Orchard    => ("Rows",       "among the trees"),
            Family.Grove      => ("Grove",      "under the trees"),
            Family.Plantation => ("Block",      "between the rows"),
            Family.Cellar     => ("Bed Room",   "among the beds"),
            Family.Garden     => ("Terrace",    "on the terrace"),
            Family.Paddy      => ("Paddy",      "knee-deep in the paddy"),
            _                 => ("Rows",       "between the vines"),
        };
        string name = $"{prefix}{_crop.Title} {noun}".Trim();
        var desc  = new List<string> { char.ToUpperInvariant(_crop.RowText[0]) + _crop.RowText[1..] };
        var moods = MoodsFor(_family);
        Area area = _family switch
        {
            Family.Field      => new StripArea(name, ctx, $"walk out onto the {name.ToLowerInvariant()}", desc, moods),
            Family.Orchard    => new OrchardArea(name, ctx, $"walk into the {name.ToLowerInvariant()}", desc, moods),
            Family.Grove      => new GroveArea(name, ctx, $"walk into the {name.ToLowerInvariant()}", desc, moods),
            Family.Plantation => new PlantationArea(name, ctx, $"go into the {name.ToLowerInvariant()}", desc, moods),
            Family.Cellar     => _crop.Word == "mushroom"
                                    ? new SpawnroomArea(name, ctx, $"duck into the {name.ToLowerInvariant()}", desc, moods)
                                    : new ForcingArea(name, ctx, $"duck into the {name.ToLowerInvariant()}", desc, moods),
            Family.Garden     => new TerraceArea(name, ctx, $"climb to the {name.ToLowerInvariant()}", desc, moods),
            Family.Paddy      => new PaddyArea(name, ctx, $"wade into the {name.ToLowerInvariant()}", desc, moods),
            _                 => new VineyardArea(name, ctx, $"walk down the {name.ToLowerInvariant()}", desc, moods),
        };

        // The plant itself: what is gathered, what is learned by looking at it.
        var items = new List<ItemElement> { new(_crop.Yield()), new(_crop.Yield()) };
        if (rng.NextDouble() < 0.5) items.Add(new ItemElement(_crop.Yield()));
        if (_crop.Byproduct != null) items.Add(new ItemElement(_crop.Byproduct()));
        area.PointsOfInterest.Add(_crop.Kind(
            _crop.Plant, new() { _crop.PlantText }, items, new[] { "ripe", "orderly", "green" },
            Underground ? SensoryProfile.Odorous : SensoryProfile.FullyAlive,
            Underground ? Teach(("examine", _crop.Lesson), ("smell", SmellLesson()))
                        : Teach(("examine", _crop.Lesson), ("smell", SmellLesson()), ("contemplate", "aesthetic"))));

        // And one thing a worked row has in it besides the crop.
        area.PointsOfInterest.Add(RowFurniture(rng));
        return area;
    }

    private string SmellLesson() => _family switch
    {
        Family.Vineyard => "bouquet",
        Family.Cellar   => "mycology",
        Family.Garden   => "tea_lore",
        Family.Orchard or Family.Grove => "ripelore",
        _               => "petrichor",
    };

    private static string[] MoodsFor(Family f) => f switch
    {
        Family.Field      => new[] { "open", "long", "rustling", "sun-warmed" },
        Family.Orchard    => new[] { "dappled", "orderly", "sweet", "shaded" },
        Family.Grove      => new[] { "dry", "silver", "shaded", "old" },
        Family.Plantation => new[] { "hot", "endless", "regimented", "humming" },
        Family.Cellar     => new[] { "dark", "damp", "warm", "close" },
        Family.Garden     => new[] { "stepped", "misty", "clipped", "green" },
        Family.Paddy      => new[] { "flooded", "mirror-still", "muddy", "bright" },
        _                 => new[] { "sloping", "ordered", "sun-baked", "dusty" },
    };

    private PointOfInterest RowFurniture(Random rng)
    {
        switch (_family)
        {
            case Family.Field:
                return rng.NextDouble() < 0.5
                    ? new ScarecrowPointOfInterest("Scarecrow", new() { "A straw-stuffed figure on a pole, sleeves flapping" },
                          Items(() => new Straw(), () => new Rope()), new[] { "tattered", "lonely" })
                      { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "peasantry"), ("contemplate", "aesthetic")) }
                    : rng.NextDouble() < 0.5
                    ? new StookPointOfInterest("Stook", new() { "Sheaves stood up against each other to dry, heads together" },
                          Items(() => new Straw(), _crop.Yield), new[] { "golden", "leaning" })
                      { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "harvestry"), ("smell", "petrichor")) }
                    : new SheafPointOfInterest("Bound Sheaf", new() { "A sheaf bound with a twist of its own stalks, left at the row's end" },
                          Items(_crop.Yield, () => new Straw()), new[] { "bound", "dry" })
                      { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "gleaning"), ("smell", "petrichor")) };
            case Family.Orchard:
            case Family.Grove:
                return new LadderPointOfInterest("Picking Ladder", new() { "A tall narrow ladder leaning into the branches, splayed at the foot" },
                           Items(() => new PickingBasket()), new[] { "tall", "rickety" })
                       { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "pruning")) };
            case Family.Plantation:
                return new BasketPointOfInterest("Pickers' Baskets", new() { "A stack of deep baskets waiting at the end of the row" },
                           Items(() => new PickingBasket(), () => new Machete()), new[] { "stacked", "worn" })
                       { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "plantership")) };
            case Family.Cellar:
                return new LanternPointOfInterest("Lamp Niche", new() { "A niche in the wall with a smoking lamp turned low" },
                           Items(() => new Lantern()), new[] { "dim", "smoky" })
                       { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "blanching")) };
            case Family.Garden:
                return new BasketPointOfInterest("Plucking Basket", new() { "A flat basket half full of bright new leaves" },
                           Items(() => new TeaLeaf(), () => new PickingBasket()), new[] { "light", "green" })
                       { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "tea_lore"), ("smell", "tea_lore")) };
            case Family.Paddy:
                return new SluicePointOfInterest("Bund Sluice", new() { "A wooden board set in the bund to let the water through or hold it" },
                           Items(() => new Reed()), new[] { "wet", "trickling" })
                       { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "sluicecraft"), ("listen", "water_voice")) };
            default:
                return new TrellisPointOfInterest("Stake Row", new() { "Stakes and a sagging wire, the vines tied along it with withies" },
                           Items(() => new VineKnife()), new[] { "sagging", "orderly" })
                       { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "viniculture")) };
        }
    }

    // ── Where it is worked ────────────────────────────────────────────────────

    private IEnumerable<Area> BuildWorkAreas(Random rng)
    {
        switch (_family)
        {
            case Family.Field:
                if (_crop.Byproduct?.Invoke() is Straw) yield return Threshing();
                yield return BarnArea();
                break;
            case Family.Orchard:
                if (_crop.Made != null) yield return PressHouse();
                yield return Packing();
                break;
            case Family.Grove:
                yield return _crop.Made != null ? PressHouse() : Drying();
                yield return Packing();
                break;
            case Family.Plantation:
                yield return _crop.Made != null ? PressHouse() : Drying();
                yield return Packing();
                break;
            case Family.Cellar:
                yield return Cellarway();
                yield return Store();
                break;
            case Family.Garden:
                yield return Drying();
                yield return Teahouse();
                break;
            case Family.Paddy:
                yield return Seedbed();
                yield return Threshing();
                break;
            default:
                yield return PressHouse();
                yield return WineCellar();
                break;
        }
    }

    /// <summary>
    /// The place's store: where the crop is kept once it is in, and so where its people trade it. A
    /// barn, a packing shed, a cellar store or a wine cellar when the work areas already hold one;
    /// otherwise <see cref="AddStore"/> builds it.
    /// </summary>
    private static bool IsStore(Area a) => a is Cathedral.Game.Scene.BarnArea or PackingArea or StoreArea or CellarArea;

    /// <summary>A store for the families whose work areas do not keep the crop: the garden's and the paddy's.</summary>
    private Area AddStore()
    {
        var (name, text, poi) = _family == Family.Garden
            ? ("Leaf Store", "A dry dim room of chests and jars where the cured leaf is kept from the damp",
               (PointOfInterest)new ChestPointOfInterest("Tea Chests", new() { "Lined chests of cured leaf, each stencilled with the garden's mark" },
                   Items(() => new TeaBrick(), () => new TeaLeaf(), () => new TeaLeaf()), new[] { "lined", "fragrant" })
                   { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "tea_lore"), ("smell", "tea_lore")) })
            : ("Granary", "A raised granary on stone feet, the grain heaped inside out of reach of the rats",
               (PointOfInterest)new SackPointOfInterest("Grain Heap", new() { "Threshed grain heaped against the boards, sacks waiting beside it" },
                   Items(_crop.Yield, _crop.Yield, () => new Sack()), new[] { "heaped", "dry" })
                   { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "harvestry"), ("smell", "petrichor")) });
        var a = new StoreArea(name, $"in the {name.ToLowerInvariant()}", $"step into the {name.ToLowerInvariant()}",
            new() { text }, new[] { "dry", "dim", "quiet" });
        a.PointsOfInterest.Add(poi);
        _work.Add(a);
        return a;
    }

    private Area Threshing()
    {
        var a = new ThreshingArea("Threshing Floor", "on the threshing floor", "step onto the threshing floor",
            new() { "A beaten-earth floor swept hard, chaff drifting at its edges" }, new[] { "dusty", "open", "hard-trodden" });
        a.PointsOfInterest.Add(new DrysheafPointOfInterest("Sheaves", new() { "Sheaves waiting to be threshed, stacked heads-in" },
            Items(_crop.Yield, _crop.Yield, () => new Straw()), new[] { "dry", "golden" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "threshery"), ("smell", "petrichor")) });
        a.PointsOfInterest.Add(new ToolPointOfInterest("Flail Rack", new() { "Flails and a winnowing basket hung on pegs" },
            Items(() => new Flail(), () => new WinnowingFan()), new[] { "worn", "dusty" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "winnowing")) });
        return a;
    }

    private Area BarnArea()
    {
        var a = new BarnArea("Barn", "inside the barn", "step into the barn",
            new() { "A tall barn smelling of earth and sacking, the crop heaped along one wall" }, new[] { "dim", "dusty", "high-roofed" });
        a.PointsOfInterest.Add(new SackPointOfInterest("Crop Sacks", new() { "Sacks of the crop stacked against the wall" },
            Items(_crop.Yield, _crop.Yield, () => new Sack()), new[] { "heavy", "full" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "harvestry"), ("smell", "petrichor")) });
        a.PointsOfInterest.Add(new ClampPointOfInterest("Root Clamp", new() { "A long heap of the crop buried in straw and earth against the frost" },
            Items(_crop.Yield, _crop.Yield, () => new Straw()), new[] { "earthed", "cold" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "almanac"), ("smell", "petrichor")) });
        a.PointsOfInterest.Add(new ToolPointOfInterest("Tool Rack", new() { "Hoes, a mattock and a rake on iron pegs" },
            Items(() => new Hoe(), () => new Mattock(), () => new Rake()), new[] { "cluttered", "rusty" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "tillage")) });
        return a;
    }

    private Area PressHouse()
    {
        var a = new PressArea("Press-House", "in the press-house", "step into the press-house",
            new() { "A low building round a great wooden press, the floor sticky and stained" }, new[] { "sticky", "sweet", "close" });
        var made = _crop.Made ?? _crop.Yield;
        a.PointsOfInterest.Add(new PressPointOfInterest("Great Press", new() { "A beam press with a screw as thick as a man's waist" },
            Items(_crop.Yield, made), new[] { "massive", "stained" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "oil_pressing"), ("smell", "bouquet")) });
        a.PointsOfInterest.Add(new VatPointOfInterest("Settling Vat", new() { "A broad vat where what comes off the press stands to settle" },
            Items(made, made), new[] { "deep", "dark" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "cellarcraft"), ("smell", "bouquet")) });
        return a;
    }

    private Area Packing()
    {
        var a = new PackingArea("Packing Shed", "in the packing shed", "step into the packing shed",
            new() { "An open-sided shed where the crop is sorted and packed in straw" }, new[] { "busy", "shaded", "orderly" });
        a.PointsOfInterest.Add(new CratePointOfInterest("Packed Crates", new() { "Crates of the crop packed in straw, ready for the road" },
            Items(_crop.Yield, _crop.Yield, () => new Straw()), new[] { "stacked", "full" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "appraisal"), ("smell", "ripelore")) });
        return a;
    }

    private Area Drying()
    {
        var a = new DryingArea("Drying Floor", "on the drying floor", "step onto the drying floor",
            new() { "Racks and mats of the crop laid out to dry, turned by hand" }, new[] { "hot", "fragrant", "open" });
        a.PointsOfInterest.Add(new RackPointOfInterest("Drying Racks", new() { "Slatted racks of the crop drying in the air" },
            Items(_crop.Yield, _crop.Yield), new[] { "fragrant", "shrivelling" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "harvestry"), ("smell", "apothecary_nose")) });
        if (_crop.Made != null)
            a.PointsOfInterest.Add(new SackPointOfInterest("Finished Sacks", new() { "Sacks of the finished goods sewn shut and marked" },
                Items(_crop.Made, _crop.Made), new[] { "heavy", "marked" })
                { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "bookkeeping")) });
        return a;
    }

    private Area Cellarway()
    {
        var a = new CellarwayArea("Cellarway", "at the foot of the cellar steps", "go down the cellar steps",
            new() { "Worn steps going down into warm dark, a smell of earth and spawn coming up them" }, new[] { "dark", "damp", "steep" });
        a.PointsOfInterest.Add(new ToolPointOfInterest("Cellar Tools", new() { "A rake, a riddle and a watering pot by the door" },
            Items(() => new Rake(), () => new WateringCan()), new[] { "damp", "rusty" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "blanching")) });
        return a;
    }

    private Area Store()
    {
        var a = new StoreArea("Cellar Store", "in the cellar store", "step into the store",
            new() { "A cool vaulted store where the crop waits in baskets for market" }, new[] { "cool", "vaulted", "quiet" });
        a.PointsOfInterest.Add(new BasketPointOfInterest("Market Baskets", new() { "Baskets of the crop covered with damp cloth" },
            Items(_crop.Yield, _crop.Yield, () => new WickerBasket()), new[] { "covered", "cool" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "mycology"), ("smell", "mycology")) });
        return a;
    }

    private Area Teahouse()
    {
        var a = new TeahouseArea("Tasting House", "in the tasting house", "step into the tasting house",
            new() { "A small airy room with a brazier, cups set out in rows for tasting" }, new[] { "airy", "quiet", "fragrant" });
        a.PointsOfInterest.Add(new BrazierPointOfInterest("Tasting Brazier", new() { "A charcoal brazier with a kettle and rows of small cups" },
            Items(() => new TeaBrick(), () => new ClayPot()), new[] { "warm", "steaming" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "tea_lore"), ("smell", "tea_lore"), ("contemplate", "meditation")) });
        return a;
    }

    private Area Seedbed()
    {
        var a = new SeedbedArea("Seedbed", "beside the seedbed", "go to the seedbed",
            new() { "A small flooded bed thick with bright green seedlings, waiting to be planted out" }, new[] { "bright", "crowded", "wet" });
        a.PointsOfInterest.Add(new SeedlingPointOfInterest("Seedling Bundles", new() { "Bundles of seedlings tied with straw, roots in the water" },
            Items(_crop.Yield, () => new Straw()), new[] { "green", "dripping" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "paddycraft")) });
        return a;
    }

    private Area WineCellar()
    {
        var a = new CellarArea("Wine Cellar", "in the wine cellar", "go down into the wine cellar",
            new() { "A low cellar of casks in ranks, the air cold and sour-sweet" }, new[] { "cold", "dark", "sour-sweet" });
        a.PointsOfInterest.Add(new BarrelPointOfInterest("Casks", new() { "Ranks of casks chalked with the year" },
            Items(() => new Wine(), () => new Wine(), () => new Raisins()), new[] { "ranked", "chalk-marked" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "cellarcraft"), ("smell", "bouquet")) });
        return a;
    }

    // ── The margin ────────────────────────────────────────────────────────────

    private Area BuildMargin(Random rng)
    {
        bool wet = _family is Family.Paddy or Family.Plantation || rng.NextDouble() < 0.4;
        if (_family == Family.Paddy || (wet && _family == Family.Plantation))
        {
            var c = new CanalArea("Irrigation Canal", "beside the canal", "follow the canal bank",
                new() { "A straight canal bringing water to the fields, a sluice at its head" }, new[] { "wet", "straight", "cool" });
            c.PointsOfInterest.Add(new SluicePointOfInterest("Head Sluice", new() { "A heavy wooden sluice gate on a chain" },
                Items(() => new Reed(), () => new Clay()), new[] { "heavy", "dripping" })
                { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "sluicecraft"), ("listen", "water_voice")) });
            return c;
        }
        if (wet)
        {
            var d = new DitchsideArea("Ditch Side", "at the ditch side", "follow the ditch",
                new() { "A drainage ditch along the edge of the worked ground, reeds in it" }, new[] { "wet", "low", "muddy" });
            d.PointsOfInterest.Add(new DitchPointOfInterest("Ditch Bank", new() { "A muddy bank where the ditch meets the worked soil" },
                Items(() => new Clay(), () => new Reed()), new[] { "muddy", "cool" })
                { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "drainage"), ("smell", "taint_sense")) });
            return d;
        }
        if (_family is Family.Orchard or Family.Grove or Family.Vineyard)
        {
            var w = new WindbreakArea("Windbreak", "under the windbreak", "walk along the windbreak",
                new() { "A line of tall trees planted against the wind along the edge of the ground" }, new[] { "sheltered", "rustling", "tall" });
            w.PointsOfInterest.Add(new TreePointOfInterest("Windbreak Poplars", new() { "Tall poplars shivering in the wind" },
                Items(() => new Branch()), new[] { "tall", "shivering" })
                { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "woodcraft"), ("listen", "weather_ear")) });
            return w;
        }
        if (_family == Family.Field && rng.NextDouble() < 0.35)
        {
            var herbs = new HerbArea("Herb Patch", "in the herb patch", "step into the herb patch",
                new() { "A small fragrant patch of cultivated herbs at the field's quieter end" }, new[] { "fragrant", "small", "tidy" });
            var clumps = new Func<PointOfInterest>[]
            {
                () => new ThymePointOfInterest("Thyme Clump", new() { "A low-clinging clump of thyme, fragrant in the warmth" }, Items(() => new Thyme()), new[] { "low", "woody" })
                      { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) },
                () => new SagePointOfInterest("Sage Clump", new() { "A spreading bush of sage, soft grey-green leaves" }, Items(() => new Sage()), new[] { "spreading", "soft" })
                      { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) },
                () => new MintPointOfInterest("Mint Clump", new() { "A vigorous patch of mint, leaves bright and cool" }, Items(() => new Mint()), new[] { "vigorous", "cool" })
                      { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) },
                () => new ChamomilePointOfInterest("Chamomile Clump", new() { "A scatter of low chamomile, white-petalled and golden-centred" }, Items(() => new Chamomile()), new[] { "low", "white" })
                      { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) },
                () => new WormwoodPointOfInterest("Wormwood Clump", new() { "A stand of wormwood, silvered leaves and bitter scent" }, Items(() => new Wormwood()), new[] { "silvered", "bitter" })
                      { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) },
            };
            foreach (var idx in SampleUniqueIndices(rng, clumps.Length, rng.Next(2, 4))) herbs.PointsOfInterest.Add(clumps[idx]());
            return herbs;
        }
        var h = new HedgebankArea("Hedgebank", "along the hedgebank", "walk the hedgebank",
            new() { "A bank topped with a laid hedge, marking the edge of the ground" }, new[] { "thorny", "green", "tangled" });
        h.PointsOfInterest.Add(new BushPointOfInterest("Laid Hedge", new() { "A hedge laid and woven, thorn and hazel together" },
            Items(() => new Thorn(), () => new WildBerry()), new[] { "thorny", "woven" })
            { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "hedgecraft"), ("listen", "birdsong")) });
        h.PointsOfInterest.Add(new MarkerPointOfInterest("Boundary Stone", new() { "A flat stone set in the bank, marking where this holding ends" },
            Items(() => new Rock()), new[] { "low", "weathered", "deliberate" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "archeology"), ("contemplate", "iconography")) });
        return h;
    }

    // ── People ────────────────────────────────────────────────────────────────

    private List<NamedNpcArchetype> BuildRoster(Random rng)
    {
        var roles = new List<NamedNpcArchetype>();
        void Add(Func<NamedNpcArchetype> make, int min, int max) { for (int i = 0, n = rng.Next(min, max + 1); i < n; i++) roles.Add(make()); }
        switch (_family)
        {
            case Family.Field:
                roles.Add(new ReeveArchetype());
                Add(() => new PlowmanArchetype(), 1, 2);
                Add(() => new ReaperArchetype(), 1, 2);
                Add(() => new HaywardArchetype(), 0, 1);
                Add(() => new BondmanArchetype(), 1, 2);
                break;
            case Family.Orchard:
            case Family.Grove:
                roles.Add(new OrchardistArchetype());
                Add(() => new PickerArchetype(), 2, 3);
                Add(() => new FarmhandArchetype(), 0, 1);
                break;
            case Family.Cellar:
                roles.Add(new FarmerArchetype());
                Add(() => new PickerArchetype(), 1, 2);   // gathers the crop and keeps the store
                Add(() => new FarmhandArchetype(), 0, 2);
                break;
            case Family.Vineyard:
                roles.Add(new VintnerArchetype());
                Add(() => new PickerArchetype(), 2, 3);
                Add(() => new FarmhandArchetype(), 0, 1);
                break;
            default:
                roles.Add(new PlanterArchetype());
                Add(() => new PickerArchetype(), 3, 5);
                break;
        }
        return roles;
    }

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_hall is null) return;
        var owns = new[] { _hall.Section }.Concat(_bunk != null ? new[] { _bunk.Section } : Array.Empty<Section>()).ToArray();
        var work = _rows.Concat(_work).ToList();
        SpawnCrew(rng, scene, _roster, _beds, _hall.PublicHall, work, SectionName().ToLowerInvariant(), owns,
            who => who is PlowmanArchetype or ReaperArchetype or PickerArchetype ? _rows
                 : who is HaywardArchetype && _margin != null ? new[] { _margin, _rows[0] }
                 : null,
            store: _store);

        SprinkleSmallLife(rng, scene, _all, _family switch
        {
            Family.Cellar                                  => SmallLife.Subterranean,
            Family.Orchard or Family.Grove or Family.Vineyard => SmallLife.Orchard,
            Family.Plantation or Family.Paddy when Biome == BiomeDatabase.Jungle => SmallLife.Tropical,
            _                                              => SmallLife.Cultivated,
        }, 2, 5);

        // The birds and beasts that live off a crop.
        var outdoors = Underground ? new List<Area>() : _rows;
        switch (_family)
        {
            case Family.Field:      TrySpawnShallow(rng, scene, new FieldMouseArchetype(), outdoors, 0.6);
                                    TrySpawnShallow(rng, scene, rng.NextDouble() < 0.5 ? new PartridgeArchetype() : new QuailArchetype(), outdoors, 0.5); break;
            case Family.Orchard:    TrySpawnShallow(rng, scene, new BlackbirdArchetype(), outdoors, 0.6); break;
            case Family.Vineyard:   TrySpawnShallow(rng, scene, new ThrushArchetype(), outdoors, 0.6); break;
            case Family.Grove:      TrySpawnShallow(rng, scene, new MagpieArchetype(), outdoors, 0.4); break;
            case Family.Plantation: TrySpawnShallow(rng, scene, new MongooseArchetype(), outdoors, 0.4);
                                    TrySpawnShallow(rng, scene, new FruitBatArchetype(), outdoors, 0.4); break;
            case Family.Garden:     TrySpawnShallow(rng, scene, new SunbirdArchetype(), outdoors, 0.5); break;
            case Family.Paddy:      Keep(rng, scene, () => new WaterBuffaloArchetype(), _rows[0], 1, 2);
                                    TrySpawnShallow(rng, scene, new DuckArchetype(), outdoors, 0.6); break;
            case Family.Cellar:     TrySpawnShallow(rng, scene, new VoleArchetype(), _all, 0.3); break;
        }
    }
}
