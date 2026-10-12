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

namespace Cathedral.Game.Scene.Livestock;

/// <summary>
/// The stock-keeping places of the settled country other than the farm, which keeps the small stock
/// and has its own factory: the plain's <b>stable</b> of horses, the mountain's <b>sheepfold</b>, the hot
/// steppe's <b>ranch</b> of zebu and horses, and the cold steppe's <b>pasture</b> of yaks and cattle.
///
/// <para>One layout — a yard, the beasts' own ground, the places they are worked and their tack kept —
/// told four ways, because what differs between them is the animals and the people who know them,
/// not the shape of the ground.</para>
/// </summary>
public sealed class LivestockSceneFactory : SettledSceneFactory
{
    public enum Kind { Stable, Sheepfold, Ranch, Pasture }

    private readonly string _key;
    private readonly Kind   _kind;

    private Area? _yard;
    private readonly List<Area> _ground = new();
    private readonly List<Area> _sheds  = new();
    private readonly List<Area> _all    = new();
    private Area? _store;
    private List<NamedNpcArchetype> _roster = new();
    private BuildingResult? _hall, _bunk;
    private List<Area> _beds = new();

    public LivestockSceneFactory(string key, string? biome = null, string? sessionPath = null) : base(biome, sessionPath)
    {
        _key  = key;
        _kind = key switch
        {
            "stable"    => Kind.Stable,
            "sheepfold" => Kind.Sheepfold,
            "ranch"     => Kind.Ranch,
            "pasture"   => Kind.Pasture,
            _ => throw new ArgumentException($"LivestockSceneFactory: '{key}' is not a stock location"),
        };
    }

    protected override string DefaultBiome => FirstBiomeListing(SettlementTable.Livestock, _key);

    protected override void BuildPlace(Random rng, int locationId, Scene scene)
    {
        _yard = Yard();
        switch (_kind)
        {
            case Kind.Stable:
                _ground.Add(Paddock("Horse Paddock", "A railed paddock of cropped grass, hoof-churned by the gate"));
                if (rng.NextDouble() < 0.5) _ground.Add(Paddock("Exercise Ring", "A sanded ring where horses are lunged on a long line"));
                _sheds.Add(Tackroom());
                _sheds.Add(_store = Fodder());
                break;
            case Kind.Sheepfold:
                _ground.Add(FoldArea("Sheepfold", "A drystone fold, sheep packed in it tight"));
                _ground.Add(AnimalPenSubfactory.BuildSheepPen());
                _ground.Add(Paddock("High Pasture", "A slope of thin turf above the fold, cropped close"));
                _sheds.Add(_store = Shearing());
                if (rng.NextDouble() < 0.6) _sheds.Add(Lambing());
                break;
            case Kind.Ranch:
                _ground.Add(Corral("Corral", "A rail corral of grey wood, the ground pounded to dust"));
                _ground.Add(Corral("Branding Pen", "A small pen with a fire-pit and a snubbing post"));
                _sheds.Add(Trough());
                _sheds.Add(Tackroom());
                _sheds.Add(_store = Smokehouse());
                break;
            default:
                _ground.Add(Paddock("Open Pasture", "Grass to the horizon, bitten short and wind-combed"));
                _ground.Add(Byre());
                _sheds.Add(_store = AnimalPenSubfactory.BuildDairyShed());
                if (rng.NextDouble() < 0.5) _sheds.Add(Dairy());
                break;
        }

        var yardSection = new Section(SectionName(), new() { "The yard and the sheds where the stock is worked" },
            seed => new GeometricGenerator { Seed = seed });
        yardSection.Areas.Add(_yard);
        foreach (var a in _sheds) yardSection.Areas.Add(a);
        scene.Sections.Add(yardSection);
        RegisterAll(scene, yardSection);

        var range = new Section(_kind == Kind.Stable ? "Paddocks" : "The Range", new() { "Where the stock is kept and grazed" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.82f });
        foreach (var a in _ground) range.Areas.Add(a);
        scene.Sections.Add(range);
        RegisterAll(scene, range);

        _all.Add(_yard); _all.AddRange(_ground); _all.AddRange(_sheds);
        OutdoorLayout.Connect(scene, _all, OutdoorLayout.RollShape(rng), "Track", rng);

        _roster = BuildRoster(rng);
        (_hall, _bunk, _beds) = HouseCrew(rng, scene, _roster, _yard, _all, HallName(), "longhouse");

        var outdoors = scene.OutdoorAreas;
        FurnitureSubfactory.AddSitSpots(rng, outdoors, FurnitureSubfactory.Setting.Farmland);
        FurnitureSubfactory.AddHidingPlaces(rng, outdoors, FurnitureSubfactory.Setting.Farmland);
        FurnitureSubfactory.AddShortcuts(rng, scene, outdoors, FurnitureSubfactory.Setting.Farmland);
        FurnitureSubfactory.AddExtractionPoints(rng, outdoors, FurnitureSubfactory.Setting.Farmland);
        AddRoofLandscapes(scene);

        Console.WriteLine($"LivestockSceneFactory: Built {_key} — biome={Biome}, areas={_all.Count}, {_roster.Count} hand(s)");
    }

    private string SectionName() => _kind switch
    {
        Kind.Stable    => "Stable Yard",
        Kind.Sheepfold => "Fold Yard",
        Kind.Ranch     => "Ranch Yard",
        _              => "Byre Yard",
    };

    private string HallName() => _kind switch
    {
        Kind.Stable    => "Stable House",
        Kind.Sheepfold => "Shepherds' Hut",
        Kind.Ranch     => "Ranch House",
        _              => "Herders' Lodge",
    };

    // ── Areas ─────────────────────────────────────────────────────────────────

    private Area Yard()
    {
        var a = new YardArea(SectionName(), "in the yard", "walk into the yard",
            new() { "A beaten yard smelling of dung and hay, gates on every side" }, new[] { "muddy", "busy", "open" });
        a.PointsOfInterest.Add(new TroughPointOfInterest("Water Trough", new() { "A long stone trough, green at the waterline" },
            Items(() => new WaterDraught()), new[] { "cold", "mossy" })
            { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "husbandry"), ("listen", "water_voice")) });
        return a;
    }

    private Area Paddock(string name, string text)
    {
        var a = new PaddockArea(name, $"in the {name.ToLowerInvariant()}", $"go into the {name.ToLowerInvariant()}",
            new() { text }, new[] { "open", "grazed", "windy" });
        a.PointsOfInterest.Add(new MangerPointOfInterest("Hay Rack", new() { "A raised rack of hay for the stock to pull at" },
            Items(() => new Hay(), () => new Hay()), new[] { "pulled", "sweet" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "herd_eye"), ("smell", "byre_sense")) });
        return a;
    }

    private Area FoldArea(string name, string text)
    {
        var a = new FoldArea(name, "in the fold", "step into the fold", new() { text }, new[] { "crowded", "woolly", "bleating" });
        a.PointsOfInterest.Add(new PenPointOfInterest("Hurdles", new() { "Wattle hurdles lashed together to pen the flock" },
            Items(() => new Rope(), () => new Wool()), new[] { "woven", "greasy" })
            { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "husbandry"), ("listen", "byre_sense")) });
        return a;
    }

    private Area Corral(string name, string text)
    {
        var a = new CorralArea(name, $"in the {name.ToLowerInvariant()}", $"climb into the {name.ToLowerInvariant()}",
            new() { text }, new[] { "dusty", "loud", "hot" });
        a.PointsOfInterest.Add(name == "Branding Pen"
            ? new BrandingPointOfInterest("Branding Fire", new() { "A pit of embers with irons laid across it" },
                  Items(() => new BrandingIron(), () => new Coal()), new[] { "hot", "acrid" })
              { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "branding"), ("smell", "byre_sense")) }
            : new PostPointOfInterest("Snubbing Post", new() { "A thick post sunk in the middle of the corral, rope-polished" },
                  Items(() => new Lariat()), new[] { "polished", "scarred" })
              { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "roping")) });
        return a;
    }

    private Area Byre()
    {
        var a = new ByreArea("Byre", "in the byre", "step into the byre",
            new() { "A long low byre, the stock tied in stalls along one side" }, new[] { "warm", "dim", "steaming" });
        a.PointsOfInterest.Add(new MangerPointOfInterest("Manger", new() { "A long wooden manger, chewed smooth" },
            Items(() => new Hay(), () => new Oats()), new[] { "chewed", "dusty" })
            { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "herd_eye"), ("smell", "byre_sense")) });
        return a;
    }

    private Area Tackroom()
    {
        var a = new TackroomArea("Tack Room", "in the tack room", "step into the tack room",
            new() { "A small room hung with saddles and bridles, smelling of leather and oil" }, new[] { "leathery", "orderly", "close" });
        a.PointsOfInterest.Add(new SaddlePointOfInterest("Saddle Racks", new() { "Saddles on wooden trees along the wall" },
            Items(() => new Saddle(), () => new Bridle()), new[] { "polished", "heavy" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "saddlery"), ("smell", "byre_sense")) });
        a.PointsOfInterest.Add(new HarnessPointOfInterest("Harness Pegs", new() { "Bridles, halters and a currycomb on pegs" },
            Items(() => new Currycomb(), () => new HoofPick(), () => new Horseshoe()), new[] { "jingling", "worn" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "farriery")) });
        return a;
    }

    private Area Fodder()
    {
        var a = new FodderArea("Fodder Loft", "in the fodder loft", "climb up to the fodder loft",
            new() { "A loft piled with hay and sacks of oats, dust hanging in the light" }, new[] { "dusty", "sweet", "high" });
        a.PointsOfInterest.Add(new HayPointOfInterest("Hay Heap", new() { "Hay heaped to the rafters" },
            Items(() => new Hay(), () => new Hay(), () => new Oats()), new[] { "dry", "golden" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "harvestry"), ("smell", "petrichor")) });
        return a;
    }

    private Area Shearing()
    {
        var a = new ShearingArea("Shearing Shed", "in the shearing shed", "step into the shearing shed",
            new() { "A slatted floor greasy with lanolin, fleeces rolled along the wall" }, new[] { "greasy", "busy", "woolly" });
        a.PointsOfInterest.Add(new FleecePointOfInterest("Rolled Fleeces", new() { "Fleeces rolled and tied, stacked for the wool-buyer" },
            Items(() => new Fleece(), () => new Fleece()), new[] { "greasy", "stacked" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "shearing"), ("smell", "byre_sense")) });
        a.PointsOfInterest.Add(new ShearsPointOfInterest("Shears Bench", new() { "A bench of hand shears laid out by the whetstone" },
            Items(() => new WoolShears(), () => new Whetstone()), new[] { "sharp", "oily" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "shearing")) });
        return a;
    }

    private Area Lambing()
    {
        var a = new LambingArea("Lambing Pens", "in the lambing pens", "go into the lambing pens",
            new() { "Small straw-lined pens under a low roof, a lantern hung from the beam" }, new[] { "warm", "quiet", "straw-smelling" });
        a.PointsOfInterest.Add(new PenPointOfInterest("Lambing Pen", new() { "A straw-lined pen with a ewe and her new lamb" },
            Items(() => new Straw(), () => new Milk()), new[] { "warm", "small" })
            { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "lambing"), ("listen", "byre_sense")) });
        return a;
    }

    private Area Trough()
    {
        var a = new TroughArea("Water Hole", "at the water hole", "go down to the water hole",
            new() { "A muddy dug water hole with a wooden trough fed from it" }, new[] { "muddy", "trampled", "precious" });
        a.PointsOfInterest.Add(new WellPointOfInterest("Sweep Well", new() { "A well with a long counterweighted sweep to lift the bucket" },
            Items(() => new WaterDraught(), () => new MilkPail()), new[] { "creaking", "deep" })
            { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "sluicecraft"), ("listen", "water_voice")) });
        return a;
    }

    /// <summary>The ranch's store: what the herd gives besides itself, hung up and salted down.</summary>
    private Area Smokehouse()
    {
        var a = new StoreArea("Smokehouse", "in the smokehouse", "step into the smokehouse",
            new() { "A low windowless house dark with smoke, strips of meat hung from the rafters and hides stacked by the door" },
            new[] { "smoky", "dark", "salt-smelling" });
        a.PointsOfInterest.Add(new RackPointOfInterest("Meat Rails", new() { "Rails under the roof hung thick with strips of drying meat" },
            Items(() => new DriedMeat(), () => new DriedMeat(), () => new Tallow()), new[] { "smoky", "hanging" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "husbandry"), ("smell", "byre_sense")) });
        a.PointsOfInterest.Add(new CratePointOfInterest("Hide Stack", new() { "Salted hides folded hair-in and stacked for the tanner" },
            Items(() => new Hide(), () => new Hide()), new[] { "salted", "stiff" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "branding")) });
        return a;
    }

    private Area Dairy()
    {
        var a = new DairyArea("Dairy", "in the dairy", "step into the dairy",
            new() { "A cool stone room with pans of milk setting and a churn by the door" }, new[] { "cool", "clean", "sour" });
        a.PointsOfInterest.Add(new ChurnstandPointOfInterest("Churn", new() { "A tall wooden churn with its dasher standing up" },
            Items(() => new Butter(), () => new Curds(), () => new Milk()), new[] { "creamy", "heavy" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "dairycraft"), ("smell", "byre_sense")) });
        return a;
    }

    // ── People and beasts ─────────────────────────────────────────────────────

    private List<NamedNpcArchetype> BuildRoster(Random rng)
    {
        var roles = new List<NamedNpcArchetype>();
        void Add(Func<NamedNpcArchetype> make, int min, int max) { for (int i = 0, n = rng.Next(min, max + 1); i < n; i++) roles.Add(make()); }
        switch (_kind)
        {
            case Kind.Stable:    roles.Add(new GroomArchetype());    Add(() => new GroomArchetype(), 1, 2); Add(() => new FarmhandArchetype(), 0, 1); break;
            case Kind.Sheepfold: roles.Add(new ShepherdArchetype()); Add(() => new ShepherdArchetype(), 0, 1); Add(() => new DroverArchetype(), 1, 2); break;
            case Kind.Ranch:     roles.Add(new DroverArchetype());   Add(() => new DroverArchetype(), 2, 3); Add(() => new GroomArchetype(), 0, 1); break;
            default:             roles.Add(new DroverArchetype());   Add(() => new DroverArchetype(), 1, 2); Add(() => new DairymaidArchetype(), 1, 1); break;
        }
        return roles;
    }

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_hall is null || _yard is null) return;
        var owns = new[] { _hall.Section }.Concat(_bunk != null ? new[] { _bunk.Section } : Array.Empty<Section>()).ToArray();
        SpawnCrew(rng, scene, _roster, _beds, _hall.PublicHall, _all, _key, owns,
            who => who is DairymaidArchetype ? _sheds : _ground.Concat(new[] { _yard }).ToList(),
            store: _store);

        var main = _ground[0];
        switch (_kind)
        {
            case Kind.Stable:
                Keep(rng, scene, () => new HorseArchetype(), main, 3, 6);
                Keep(rng, scene, () => new FoalArchetype(), main, 0, 2);
                Keep(rng, scene, () => rng.NextDouble() < 0.5 ? new DonkeyArchetype() : new MuleArchetype(), _yard, 0, 1);
                TrySpawnShallow(rng, scene, new SwallowArchetype(), _sheds, 0.7);
                break;
            case Kind.Sheepfold:
                Keep(rng, scene, () => new SheepArchetype(), main, 4, 8);
                Keep(rng, scene, () => new RamArchetype(), main, 1, 1);
                Keep(rng, scene, () => new LambArchetype(), _sheds.Count > 1 ? _sheds[1] : main, 1, 3);
                Keep(rng, scene, () => new GoatArchetype(), _ground[^1], 0, 3);
                Keep(rng, scene, () => new HoundArchetype(), _yard, 0, 1);
                break;
            case Kind.Ranch:
                Keep(rng, scene, () => new ZebuArchetype(), main, 3, 6);
                Keep(rng, scene, () => new HorseArchetype(), _ground[1], 2, 3);
                Keep(rng, scene, () => new BullArchetype(), main, 0, 1);
                Keep(rng, scene, () => new DromedaryArchetype(), _yard, 0, 1);
                break;
            default:
                Keep(rng, scene, () => new YakArchetype(), main, 3, 6);
                Keep(rng, scene, () => new CowArchetype(), _ground[1], 1, 3);
                Keep(rng, scene, () => new CalfArchetype(), _ground[1], 0, 2);
                Keep(rng, scene, () => new HorseArchetype(), main, 1, 2);
                break;
        }
        SprinkleSmallLife(rng, scene, _all, SmallLife.Cultivated, 2, 4);
    }
}
