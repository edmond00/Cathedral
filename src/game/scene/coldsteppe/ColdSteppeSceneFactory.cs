using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.ColdSteppe;

/// <summary>
/// Builds a cold steppe — what plain and field become in cold country (see <c>ClimateRule</c>).
///
/// Identity: tundra (moss, lichen and thaw water), steppe (dry grass under a hard sky) or barrens
/// (bare stone and lichen) — gates the open areas.
/// Sections: Open Steppe and Low Ground (the thaw lake and its margins).
/// Climb: the steep side of a pingo — a hill with a core of ice — whose top sees the whole place.
/// Uninhabited: no settlement stands in cold country yet.
/// </summary>
public class ColdSteppeSceneFactory : WildSceneFactory
{
    public ColdSteppeSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Tundra, Steppe, Barrens }

    private Identity _identity;
    private Area? _pingo, _thaw, _farBank, _iceCore, _lake, _islet;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // A mosaic: open steppe, wet low ground, a river terrace and a pingo field, meeting in a ring.
        Profile = LayoutProfile.Mosaic;
        _identity = (Identity)rng.Next(3);
        double W(double tundra, double steppe, double barrens)
            => _identity switch { Identity.Tundra => tundra, Identity.Steppe => steppe, _ => barrens };

        var open = Draw(rng, new Option[]
        {
            new(BuildTundra,        W(3.0, 1.0, 1.2)),
            new(BuildTussockSteppe, W(1.2, 3.0, 0.6)),
            new(BuildBarrens,       W(0.6, 0.8, 3.0)),
            new(BuildFrostfield,    W(1.2, 0.6, 1.5)),
            new(BuildWillowScrub,   W(1.5, 0.8, 0.4)),
            new(BuildErratics,      W(0.6, 0.6, 2.0)),
        }, Between(rng, 2, 3));

        var pool = new List<(int Kind, double Weight)> { (0, 2.0), (1, W(1.5, 2.0, 1.0)), (2, 1.2), (3, W(1.5, 0.8, 1.0)) };
        var kinds = new List<int>();
        for (int want = Between(rng, 2, 3); kinds.Count < want;)
        {
            double total = pool.Sum(p => p.Weight), roll = rng.NextDouble() * total;
            int i = 0;
            for (double acc = pool[0].Weight; acc < roll && i < pool.Count - 1; acc += pool[++i].Weight) { }
            kinds.Add(pool[i].Kind);
            pool.RemoveAt(i);
        }

        var sections = new List<(string Name, string Description, List<Area> Areas)>
        {
            ("Open Steppe", "Low ground to the edge of the world under a hard grey sky, the wind never stopping", open),
        };
        foreach (var kind in kinds)
            sections.Add(kind switch
            {
                0 => ("Low Ground", "A thaw lake and the sedge round it, loud in the short summer, dead still in the long winter",
                    Draw(rng, new Option[] { new(BuildThawLake, 2.5), new(BuildMuskeg, 1.5) }, Between(rng, 1, 2))),
                1 => ("River Terrace", "A grey river winding over the flats, its banks cut sheer where it swings, an esker running beside it",
                    Draw(rng, new Option[] { new(BuildTundraRiver, 3.0), new(BuildEsker, 1.5), new(BuildCutBank, 1.2) }, Between(rng, 1, 2))),
                2 => ("Pingo Field", "Green mounds standing out of the flat, each with a core of ice, frost-split on their crowns",
                    new List<Area> { BuildFrostfield() }),
                _ => ("Lake Country", "A wide grey lake under a wide grey sky, the far shore a line, an islet of rock out in it",
                    new List<Area> { (_lake = BuildTundraLake()) }),
            });

        // The pingo is a climb's top, the far bank a swim's.
        _pingo = rng.NextDouble() < 0.8 ? BuildPingo() : null;
        _iceCore = _pingo != null && rng.NextDouble() < 0.4 ? BuildIceCore() : null;
        _islet = _lake != null && rng.NextDouble() < 0.7 ? BuildLakeIslet() : null;
        var river = sections.SelectMany(s => s.Areas).OfType<RiverArea>().FirstOrDefault();
        _farBank = river != null && rng.NextDouble() < 0.55 ? BuildFarBank() : null;
        _thaw = sections.SelectMany(s => s.Areas).OfType<ThawArea>().FirstOrDefault();

        foreach (var area in sections.SelectMany(s => s.Areas)) Populate(area, rng);
        foreach (var area in new[] { _pingo, _farBank, _iceCore, _islet }) if (area != null) Populate(area, rng);

        var registered = sections.Select(s => AddSection(scene, s.Name, s.Description,
            seed => new NoisyGenerator { Seed = seed, Density = 0.86f }, s.Areas)).ToList();

        string[] moods = { "trodden", "wind-flattened", "frost-heaved" };
        static string Way(Area a, Area b) =>
            a is ThawArea or MuskegArea || b is ThawArea or MuskegArea ? "Sedge Path"
            : a is EskerArea || b is EskerArea ? "Ridge Path"
            : a is RiverArea || b is RiverArea ? "Bank Path"
            : "Herd Trail";

        foreach (var (_, _, areas) in sections)
            WeaveSection(rng, scene, areas, RollWeave(rng), Way, "over the steppe", moods);
        for (int i = 0, joins = sections.Count == 2 ? 1 : sections.Count; i < joins; i++)
        {
            var a = sections[i].Areas;
            var b = sections[(i + 1) % sections.Count].Areas;
            var (x, y) = (a[rng.Next(a.Count)], b[rng.Next(b.Count)]);
            OutdoorLayout.Link(scene, x, y, Way(x, y), moods, "over the steppe");
        }

        if (_pingo != null)
        {
            var host = registered.Last(s => s.Areas.Any(a => a is FrostfieldArea or TundraArea or BarrensArea or TussockArea));
            var foot = host.Areas[rng.Next(host.Areas.Count)];
            AddToSection(scene, host, _pingo);
            AddViewpoint(scene, foot, _pingo, "Pingo Slope",
                "The steep green flank of a pingo, its turf split and slumping where the ice inside it heaves",
                new[] { "steep", "split", "slumping", "frost-heaved" });
        }
        if (_farBank != null)
        {
            AddToSection(scene, registered.First(s => s.Areas.Contains(river!)), _farBank);
            Swim(scene, river!, _farBank, WaterKind.River, "Tundra River",
                "The river, grey with silt and fast, cold enough to stop the heart, the far bank a cut of black frozen earth",
                new[] { "grey", "fast", "heart-stopping" }, lesson: "plunge");
        }

        if (_iceCore != null)
        {
            AddToSection(scene, scene.Sections.First(s => s.Areas.Contains(_pingo!)), _iceCore);
            SlipInto(scene, _pingo!, _iceCore, SlipKind.Crevice, "Split Crown",
                "The pingo's crown split open by the ice inside, a dark crack going down into it, cold breathing out",
                new[] { "split", "dark", "cold-breathing" }, lesson: "potholing");
        }
        if (_islet != null)
        {
            AddToSection(scene, registered.First(s => s.Areas.Contains(_lake!)), _islet);
            Swim(scene, _lake!, _islet, WaterKind.Pond, "Tundra Lake",
                "Grey lake water, shallow and then suddenly deep, so cold the breath stops at the first stroke",
                new[] { "grey", "shelving", "breath-stopping" }, lesson: "plunge");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _pingo, _farBank, _iceCore, _islet);

        Console.WriteLine($"ColdSteppeSceneFactory: {_identity}, {string.Join("+", sections.Select(s => s.Areas.Count))} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildWillowScrub() => new ThicketArea(
        displayName: "Willow Scrub",
        contextDescription: "in the willow scrub",
        transitionDescription: "push into the willow scrub",
        descriptions: new() { "Dwarf willow knee-high and thick as a hedge along a hollow, ptarmigan bursting out of it at every step" },
        moods: new[] { "knee-high", "tangled", "bursting", "low" });

    private static Area BuildErratics() => new BoulderArea(
        displayName: "Erratic Field",
        contextDescription: "among the erratics",
        transitionDescription: "walk out among the erratics",
        descriptions: new() { "Boulders dropped by ice long gone, scattered over the flat like the toys of something huge, lichen orange on every one" },
        moods: new[] { "scattered", "huge", "lichened", "lonely" });

    private static Area BuildTundraLake() => new LakeArea(
        displayName: "Tundra Lake",
        contextDescription: "on the shore of the tundra lake",
        transitionDescription: "come down to the lake shore",
        descriptions: new() { "A wide shallow lake reaching to the horizon, its shore of stones and sedge, loons calling somewhere out on it" },
        moods: new[] { "wide", "grey", "calling", "still" });

    private static Area BuildLakeIslet() => new IsletArea(
        displayName: "Lake Islet",
        contextDescription: "on the lake islet",
        transitionDescription: "wade out onto the islet",
        descriptions: new() { "A hump of rock and moss out in the lake, nests of down among the stones, the shore a long cold swim back" },
        moods: new[] { "mossy", "nested", "isolated", "cold" });

    private static Area BuildIceCore() => new GrottoArea(
        displayName: "Ice Core",
        contextDescription: "inside the pingo",
        transitionDescription: "squeeze down into the pingo",
        descriptions: new() { "The heart of the pingo: a lens of clear ice under the turf, walls glowing faintly, the cold absolute" },
        moods: new[] { "glowing", "clear", "absolute", "hushed" });

    private static Area BuildFrostfield() => new FrostfieldArea(
        displayName: "Patterned Ground",
        contextDescription: "on the patterned ground",
        transitionDescription: "walk out onto the patterned ground",
        descriptions: new() { "Flat ground the frost has sorted into rings and stripes of stone, mile on mile, as if laid out by a mason" },
        moods: new[] { "sorted", "ringed", "orderly", "uncanny" });

    private static Area BuildMuskeg() => new MuskegArea(
        displayName: "Muskeg",
        contextDescription: "on the muskeg",
        transitionDescription: "squelch out onto the muskeg",
        descriptions: new() { "Sphagnum moss over frozen ground, quaking at every step, black spruce leaning drunk on it" },
        moods: new[] { "quaking", "mossy", "leaning", "sodden" });

    private static Area BuildTundraRiver() => new RiverArea(
        displayName: "Tundra River",
        contextDescription: "by the tundra river",
        transitionDescription: "come down to the river",
        descriptions: new() { "A wide grey river braiding over gravel bars, cold enough to ache the hand dipped in it" },
        moods: new[] { "grey", "braided", "aching", "loud" });

    private static Area BuildCutBank() => new CliffArea(
        displayName: "Cut Bank",
        contextDescription: "under the cut bank",
        transitionDescription: "walk along under the cut bank",
        descriptions: new() { "A sheer bank of black frozen earth where the river swings, thawing and slumping, old bones and tusks showing in it" },
        moods: new[] { "black", "slumping", "thawing", "bone-studded" });

    private static Area BuildFarBank() => new TundraArea(
        displayName: "Far Bank",
        contextDescription: "on the far bank",
        transitionDescription: "drag yourself out on the far bank",
        descriptions: new() { "The tundra on the other side of the river, unwalked, the herds' trails going away into it" },
        moods: new[] { "unwalked", "open", "far", "wind-flattened" });


    private static Area BuildTundra() => new TundraArea(
        displayName: "Tundra",
        contextDescription: "out on the tundra",
        transitionDescription: "step out onto the tundra",
        descriptions: new() { "Moss and lichen and finger-high scrub over ground frozen a spade's depth down, pools in every hollow" },
        moods: new[] { "spongy", "low", "pooled", "vast" });

    private static Area BuildTussockSteppe() => new TussockArea(
        displayName: "Tussock Steppe",
        contextDescription: "on the tussock steppe",
        transitionDescription: "wade into the tussocks",
        descriptions: new() { "Tussocks of pale grass knee-high and ankle-turning, the gaps between them hidden, combed flat by the wind" },
        moods: new[] { "pale", "combed", "ankle-turning", "rustling" });

    private static Area BuildBarrens() => new BarrensArea(
        displayName: "Lichen Barrens",
        contextDescription: "on the lichen barrens",
        transitionDescription: "cross onto the barrens",
        descriptions: new() { "Bare stone frost-split into plates, crusted with lichen in grey and orange and black" },
        moods: new[] { "bare", "frost-split", "lichened", "silent" });

    private static Area BuildEsker() => new EskerArea(
        displayName: "Esker",
        contextDescription: "on the esker",
        transitionDescription: "climb onto the esker",
        descriptions: new() { "A long sinuous ridge of gravel laid by a river that once ran inside the ice, dry and firm underfoot" },
        moods: new[] { "sinuous", "dry", "firm", "raised" });

    private static Area BuildThawLake() => new ThawArea(
        displayName: "Thaw Lake",
        contextDescription: "by the thaw lake",
        transitionDescription: "come down to the thaw lake",
        descriptions: new() { "A shallow lake of meltwater with sedge and cotton grass round it, birds standing in the shallows" },
        moods: new[] { "shallow", "sedged", "bird-loud", "brief" });

    private static Area BuildPingo() => new PingoArea(
        displayName: "Pingo Top",
        contextDescription: "on top of the pingo",
        transitionDescription: "pull yourself onto the pingo's crown",
        descriptions: new() { "The split crown of an ice-cored hill, the only height for a day's walk, the steppe flat below" },
        moods: new[] { "split", "lonely", "high", "commanding" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case TundraArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildReindeerMoss());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildCloudberryPatch());
                area.PointsOfInterest.Add(BuildFrostPolygons());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildDwarfBirch());
                break;
            case TussockArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildTussockGrass(
                    () => new CottonGrass(), () => new Straw(),
                    "Tussocks of wiry pale grass, white cotton-grass heads nodding between them in the wind"));
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildLabradorTea());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildShedAntlers());
                if (rng.NextDouble() < 0.3) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case BarrensArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildReindeerMoss());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildErratic());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildCairn());
                break;
            case EskerArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildDwarfBirch());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildCrowberryMat());
                area.PointsOfInterest.Add(BuildLemmingRuns());
                break;
            case ThawArea:
                area.PointsOfInterest.Add(BuildThawPool());
                area.PointsOfInterest.Add(BuildSedgeMargin());
                break;
            case ThicketArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildDwarfBirch());
                area.PointsOfInterest.Add(BuildLingonberries());
                break;
            case BoulderArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildErratic());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case LakeArea:
                area.PointsOfInterest.Add(BuildSedgeMargin());
                break;
            case IsletArea:
                area.PointsOfInterest.Add(BuildDownNests());
                break;
            case GrottoArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Ice Lens",
                    "Clear ice old as the ground, layered like glass, stones frozen in it as if dropped in water"));
                break;
            case FrostfieldArea:
                area.PointsOfInterest.Add(BuildFrostPolygons());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildErratic());
                break;
            case MuskegArea:
                area.PointsOfInterest.Add(BuildSedgeMargin());
                area.PointsOfInterest.Add(BuildLingonberries());
                break;
            case RiverArea:
                area.PointsOfInterest.Add(BuildGravelBar());
                break;
            case CliffArea:
                area.PointsOfInterest.Add(BuildThawingBank());
                break;
            case PingoArea:
                area.PointsOfInterest.Add(BuildSplitCrown());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
        }
    }

    private static PointOfInterest BuildDownNests() => new NestPointOfInterest(
        displayName: "Down Nests",
        descriptions: new() { "Hollows among the stones lined thick with grey down, a clutch of olive eggs in one" },
        items: new() { new ItemElement(new Egg()), new ItemElement(new Feather()) },
        moods: new[] { "downy", "hidden", "olive-egged", "warm" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fowling" } };

    private static PointOfInterest BuildLingonberries() => new BushPointOfInterest(
        displayName: "Lingonberry Mat",
        descriptions: new() { "A low mat of glossy evergreen leaves over the moss, hard red berries scattered through it" },
        items: new() { new ItemElement(new Lingonberry()), new ItemElement(new Lingonberry()) },
        moods: new[] { "glossy", "low", "red-berried", "evergreen" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "berrying" } };

    private static PointOfInterest BuildGravelBar() => new StreamPointOfInterest(
        displayName: "Gravel Bar",
        descriptions: new() { "A bar of grey stones in the river, the water racing round it, a char hanging in the slack behind" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Flint()) },
        moods: new[] { "grey", "racing", "stony", "cold" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fording", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildThawingBank() => new BonesPointOfInterest(
        displayName: "Thawing Bank",
        descriptions: new() { "The face of the cut bank, black earth and ice thawing in the sun, a curve of old yellow ivory sticking out of it among bones" },
        items: new() { new ItemElement(new FossilIvory()), new ItemElement(new Bone()), new ItemElement(new Bone()) },
        moods: new[] { "thawing", "black", "bone-studded", "ancient" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tusk_hunting", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildFrostPolygons() => new PolygonPointOfInterest(
        displayName: "Frost Polygons",
        descriptions: new() { "The ground cracked into a honeycomb of polygons by the frost, each rimmed with stones the ice pushed up" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Flint()) },
        moods: new[] { "patterned", "rimmed", "orderly", "strange" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["contemplate"] = "geometric_scheme" } };

    private static PointOfInterest BuildLemmingRuns() => new BurrowPointOfInterest(
        displayName: "Lemming Runs",
        descriptions: new() { "Little tunnels worn through the moss between holes, stitched with droppings and a squeaking somewhere below" },
        items: new() { new ItemElement(new Moss()) },
        moods: new[] { "tunnelled", "squeaking", "busy", "small" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading", ["listen"] = "keen_ear" } };

    private static PointOfInterest BuildThawPool() => new PoolPointOfInterest(
        displayName: "Thaw Pool",
        descriptions: new() { "Clear meltwater over a bed of moss, the bottom still ice a hand down, char hanging in the cold" },
        items: new() { new ItemElement(new ArcticChar()), new ItemElement(new IceShard()) },
        moods: new[] { "clear", "cold", "mossy-floored", "still" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "anglery", ["listen"] = "water_voice", ["smell"] = "petrichor", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildSedgeMargin() => new ReedPointOfInterest(
        displayName: "Sedge Margin",
        descriptions: new() { "A band of sedge and cotton grass round the water, the white heads blowing loose in the wind" },
        items: new() { new ItemElement(new Reed()), new ItemElement(new CottonGrass()) },
        moods: new[] { "white-headed", "blowing", "wet", "rustling" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork", ["listen"] = "birdsong", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildSplitCrown() => new CrackPointOfInterest(
        displayName: "Split Crown",
        descriptions: new() { "The pingo's top cracked open like a loaf, and in the crack, under the turf, clean white ice" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new IceShard()) },
        moods: new[] { "split", "white-cored", "heaving", "strange" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["contemplate"] = "awe" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new WhiteWolfArchetype(), 0.35);

        TrySpawnCreature(rng, scene, new ReindeerArchetype(),    0.60);
        TrySpawnCreature(rng, scene, new MuskOxArchetype(),      0.40);
        TrySpawnCreature(rng, scene, new SaigaArchetype(),       0.30);
        TrySpawnCreature(rng, scene, new ArcticFoxArchetype(),   0.45);
        TrySpawnCreature(rng, scene, new LemmingArchetype(),     0.60);
        TrySpawnCreature(rng, scene, new SnowBuntingArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new PtarmiganArchetype(),   0.35);
        if (_thaw != null) TrySpawnCreature(rng, scene, new CraneArchetype(), 0.40, new[] { _thaw });
        TrySpawnCreatureIn<LakeArea>(rng, scene, new CraneArchetype(), 0.40);
        TrySpawnCreatureIn<ThicketArea>(rng, scene, new PtarmiganArchetype(), 0.60);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 2, 4);
    }
}
