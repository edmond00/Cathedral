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

namespace Cathedral.Game.Scene.Snowfield;

/// <summary>
/// Builds a snowfield — what a mountain becomes in cold country (see <c>ClimateRule</c>).
///
/// Sections: Snowfield (the open snow, its drifts, an avalanche chute and the cornice above it) and
/// Sheltered Ground (when rolled, a stand of wind-crippled trees and a frozen tarn).
/// Climb: up the chute's side onto the cornice ridge, which sees the whole place.
/// Uninhabited: no settlement stands in cold country yet.
/// </summary>
public class SnowfieldSceneFactory : WildSceneFactory
{
    public SnowfieldSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _chute, _cornice, _col, _snowCave;
    private readonly List<Area> _sheltered = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // A pyramid: sheltered ground at the foot, the open snowfield above it, the ridge on top.
        Profile = LayoutProfile.Pyramid;
        int identity = rng.Next(3); // 0 tree line, 1 open snows, 2 gullied
        double W(double trees, double open, double gullied) => identity switch { 0 => trees, 1 => open, _ => gullied };

        _sheltered.AddRange(Draw(rng, new Option[]
        {
            new(BuildKrummholz,   W(3.0, 1.0, 1.0)),
            new(BuildLarchWood,   W(2.5, 0.3, 0.6)),
            new(BuildTarn,        W(0.8, 1.8, 0.8)),
            new(BuildRunout,      W(1.0, 0.6, 2.5)),
            new(BuildSnowHollow,  W(0.6, 2.0, 0.8)),
            new(BuildFrozenFalls, W(0.6, 0.4, 2.0)),
            new(BuildBuriedBoulders, W(0.6, 1.5, 1.0)),
        }, Between(rng, 3, 4)));

        bool hasChute = rng.NextDouble() < W(0.5, 0.4, 0.95);
        _chute = hasChute ? BuildChute() : null;
        var open = Draw(rng, new Option[]
        {
            new(BuildSnowpack, 1.5),
            new(BuildDrifts, 1.5),
            new(BuildWindSlab, W(0.6, 1.5, 1.0)),
            new(BuildBlueIce, W(0.3, 1.2, 0.6)),
        }, Between(rng, 1, Math.Min(hasChute ? 2 : 3, _sheltered.Count - (hasChute ? 1 : 0))));
        if (_chute != null) open.Insert(0, _chute);
        var drifts = open.OfType<DriftArea>().FirstOrDefault();
        _snowCave = drifts != null && open.Count < _sheltered.Count && rng.NextDouble() < 0.45 ? BuildSnowCave() : null;

        _cornice = BuildCornice();
        var ridge = new List<Area> { _cornice };
        _col = open.Count >= 2 && rng.NextDouble() < 0.45 ? (rng.NextDouble() < 0.6 ? BuildCol() : BuildRockTower()) : null;
        if (_col != null) ridge.Add(_col);

        foreach (var area in _sheltered.Concat(open).Concat(ridge)) Populate(area, rng);
        if (_snowCave != null) Populate(_snowCave, rng);

        AddSection(scene, W(0, 1, 2) switch { 0 => "Tree Line", 1 => "Sheltered Ground", _ => "Gully Foot" },
            "A dip out of the wind where something manages to live",
            seed => new NoisyGenerator { Seed = seed, Density = 0.68f }, _sheltered, tier: 0);
        var openSection = AddSection(scene, identity == 2 ? "Avalanche Slopes" : "Snowfield", "Snow to the sky in every direction, the light coming from everywhere at once",
            seed => new NoisyGenerator { Seed = seed, Density = 0.90f }, open, tier: 1);
        AddSection(scene, identity == 1 ? "The Crest" : "The Ridge", "The crest above the snowfield, wind-scoured, the cornice curling out over nothing",
            seed => new WaveGenerator { Seed = seed }, ridge, tier: 2);

        string[] moods = { "deep", "crusted", "blue-shadowed" };
        static string Way(Area a, Area b) =>
            a is ChuteArea || b is ChuteArea ? "Traverse"
            : a is DriftArea || b is DriftArea ? "Drift Line"
            : "Snow Track";

        WeaveSection(rng, scene, _sheltered, RollWeave(rng), Way, "across the snow", moods);
        WeaveSection(rng, scene, open, Weave.Line, Way, "across the snow", moods);
        JoinSections(rng, scene, _sheltered, open, rng.NextDouble() < 0.4 ? 2 : 1, Way, "up the snow", moods);

        if (_chute != null)
            AddViewpoint(scene, _chute, _cornice, "Chute Wall",
                "The steep side of the avalanche chute, snow over rock, kicked steps going up to where the cornice overhangs",
                new[] { "steep", "kicked", "overhung", "loaded" }, icy: true);
        else
            AddViewpoint(scene, open[rng.Next(open.Count)], _cornice, "Snow Ramp",
                "A long steepening ramp of hard snow running up under the cornice, every step kicked in",
                new[] { "steepening", "hard", "kicked" }, icy: true);
        if (_col != null)
            OutdoorLayout.Link(scene, _cornice, _col, "Ridge Walk", moods, "along the ridge");
        if (_snowCave != null)
        {
            AddToSection(scene, openSection, _snowCave);
            SlipInto(scene, drifts!, _snowCave, SlipKind.Hollow, "Snow Hole",
                "A hole dug into the lee of the deepest drift, just big enough to crawl into, blue light inside",
                new[] { "dug", "blue-lit", "low" }, lesson: "potholing");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _cornice, _col, _snowCave);

        Console.WriteLine($"SnowfieldSceneFactory: {_sheltered.Count}/{open.Count}/{ridge.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildLarchWood() => new ForestArea(
        displayName: "Larch Wood",
        contextDescription: "in the larch wood",
        transitionDescription: "walk in under the larches",
        descriptions: new() { "The last trees below the snow, larches standing far apart, their needles gold, the snow between them pocked with tracks" },
        moods: new[] { "gold-needled", "sparse", "tracked", "quiet" });

    private static Area BuildBuriedBoulders() => new BoulderArea(
        displayName: "Buried Boulders",
        contextDescription: "among the buried boulders",
        transitionDescription: "pick a way among the buried boulders",
        descriptions: new() { "Great boulders humped under the snow like sleeping beasts, dark wind-scoops on their lee sides" },
        moods: new[] { "humped", "sleeping", "wind-scooped", "still" });

    private static Area BuildWindSlab() => new SnowpackArea(
        displayName: "Wind Slab",
        contextDescription: "on the wind slab",
        transitionDescription: "step out onto the wind slab",
        descriptions: new() { "A slope of chalky wind-packed snow, smooth and hard, that sounds like a drum underfoot" },
        moods: new[] { "chalky", "drumming", "smooth", "loaded" });

    private static Area BuildBlueIce() => new IceArea(
        displayName: "Blue Ice",
        contextDescription: "on the blue ice",
        transitionDescription: "step out onto the blue ice",
        descriptions: new() { "A patch where the wind has stripped the snow off old ice, blue and bare and slick as glass" },
        moods: new[] { "blue", "bare", "slick", "glassy" });

    private static Area BuildRockTower() => new CragArea(
        displayName: "Rock Tower",
        contextDescription: "on the rock tower",
        transitionDescription: "scramble up onto the rock tower",
        descriptions: new() { "A tower of frost-shattered rock standing out of the ridge, rime feathered thick on its windward side" },
        moods: new[] { "shattered", "rimed", "standing", "airy" });

    private static Area BuildRunout() => new RunoutArea(
        displayName: "Avalanche Runout",
        contextDescription: "on the avalanche runout",
        transitionDescription: "climb onto the avalanche debris",
        descriptions: new() { "A tumbled field of hard snow blocks and snapped trees where an avalanche came to rest, the scar of its track above" },
        moods: new[] { "tumbled", "snapped", "hard-packed", "violent" });

    private static Area BuildSnowHollow() => new BasinArea(
        displayName: "Snow Hollow",
        contextDescription: "in the snow hollow",
        transitionDescription: "drop down into the snow hollow",
        descriptions: new() { "A bowl among the drifts where the wind does not reach, the snow soft and the sky a round of blue overhead" },
        moods: new[] { "still", "bowled", "soft", "quiet" });

    private static Area BuildFrozenFalls() => new WaterfallArea(
        displayName: "Frozen Falls",
        contextDescription: "at the foot of the frozen falls",
        transitionDescription: "come under the frozen falls",
        descriptions: new() { "A waterfall frozen in mid-fall, a curtain of blue and white columns, water still running somewhere behind the ice" },
        moods: new[] { "frozen", "columned", "blue", "trickling" });

    private static Area BuildSnowCave() => new GrottoArea(
        displayName: "Snow Cave",
        contextDescription: "in the snow cave",
        transitionDescription: "crawl into the snow cave",
        descriptions: new() { "A hollow inside the drift, its walls smoothed by somebody's hands, the light blue and the wind silent" },
        moods: new[] { "blue", "silent", "smoothed", "close" });

    private static Area BuildCol() => new ColArea(
        displayName: "Snow Col",
        contextDescription: "on the snow col",
        transitionDescription: "walk along onto the snow col",
        descriptions: new() { "A saddle in the ridge, the wind driving snow through it in long streamers, the far valley showing grey below" },
        moods: new[] { "streaming", "saddled", "howling", "white" });


    private static Area BuildSnowpack() => new SnowpackArea(
        displayName: "Open Snowfield",
        contextDescription: "out on the snowfield",
        transitionDescription: "wade out into the snow",
        descriptions: new() { "A sweep of untrodden snow, thigh-deep and crusted, without a mark on it from edge to edge" },
        moods: new[] { "untrodden", "blinding", "silent", "deep" });

    private static Area BuildDrifts() => new DriftArea(
        displayName: "Wind Drifts",
        contextDescription: "among the wind drifts",
        transitionDescription: "push in among the drifts",
        descriptions: new() { "Snow heaped by the wind into dunes and wave-crests, hard as board on top, bottomless underneath" },
        moods: new[] { "heaped", "wave-crested", "hard-crusted", "hissing" });

    private static Area BuildChute() => new ChuteArea(
        displayName: "Avalanche Chute",
        contextDescription: "in the avalanche chute",
        transitionDescription: "cross into the chute",
        descriptions: new() { "A trough swept clean down the mountainside, trees snapped off along its edges, the snow in it loaded and quiet" },
        moods: new[] { "swept", "loaded", "quiet", "dangerous" });

    private static Area BuildKrummholz() => new KrummholzArea(
        displayName: "Krummholz",
        contextDescription: "among the krummholz",
        transitionDescription: "crouch into the krummholz",
        descriptions: new() { "Trees crippled by the wind into a waist-high mat, so dense the snow lies on it like a roof" },
        moods: new[] { "crippled", "dense", "sheltered", "dark" });

    private static Area BuildTarn() => new TarnArea(
        displayName: "Frozen Tarn",
        contextDescription: "on the frozen tarn",
        transitionDescription: "step out onto the frozen tarn",
        descriptions: new() { "A small lake in a hollow, frozen and snowed over, its shape only a flatness in the snow" },
        moods: new[] { "flat", "hidden", "booming", "cold" });

    private static Area BuildCornice() => new CorniceArea(
        displayName: "Cornice Ridge",
        contextDescription: "on the cornice ridge",
        transitionDescription: "climb out onto the ridge",
        descriptions: new() { "A ridge-line whose snow curls out over the drop in a frozen wave, the whole snowfield below" },
        moods: new[] { "curling", "overhanging", "high", "precarious" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case SnowpackArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockOutcrop());
                break;
            case DriftArea:
                area.PointsOfInterest.Add(BuildSastrugi());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case ChuteArea:
                area.PointsOfInterest.Add(BuildAvalancheDebris());
                area.PointsOfInterest.Add(BuildSnappedTrees());
                break;
            case KrummholzArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStuntedFir());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildCrowberryMat());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case TarnArea:
                area.PointsOfInterest.Add(BuildTarnIce());
                break;
            case ForestArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStuntedFir());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildPineTree());
                break;
            case BoulderArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildBoulder());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case IceArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Wind-Polished Ice",
                    "Ice polished by the wind until it shines, bubbles frozen deep in it like embers"));
                break;
            case CragArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case RunoutArea:
                area.PointsOfInterest.Add(BuildAvalancheDebris());
                area.PointsOfInterest.Add(BuildSnappedTrees());
                break;
            case BasinArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case WaterfallArea:
                area.PointsOfInterest.Add(BuildIceColumns());
                break;
            case GrottoArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case ColArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildCairn());
                break;
            case CorniceArea:
                area.PointsOfInterest.Add(BuildCorniceLip());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
        }
    }

    private static PointOfInterest BuildIceColumns() => new FallsPointOfInterest(
        displayName: "Ice Columns",
        descriptions: new() { "The fall frozen into columns and curtains of blue-white ice, water still trickling somewhere behind them" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new IceShard()) },
        moods: new[] { "columned", "blue-white", "trickling", "still" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "icecraft", ["contemplate"] = "awe", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildSastrugi() => new DriftPointOfInterest(
        displayName: "Sastrugi",
        descriptions: new() { "Ridges carved in the hard snow by the wind, sharp-prowed, all pointing the way the gales blow" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "carved", "prowed", "aligned", "hard" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "wind_reading", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildAvalancheDebris() => new RubblePointOfInterest(
        displayName: "Avalanche Debris",
        descriptions: new() { "A tumbled fan of snow blocks set hard as stone, rocks and broken branches frozen into it" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new IceShard()), new ItemElement(new Branch()) },
        moods: new[] { "tumbled", "set-hard", "chaotic", "recent" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "snowcraft", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildSnappedTrees() => new DeadfallPointOfInterest(
        displayName: "Snapped Trees",
        descriptions: new() { "Firs broken off at head height along the chute's edge, all fallen the same way" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Twig()), new ItemElement(new PineNeedle()) },
        moods: new[] { "snapped", "aligned", "splintered", "resinous" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "petrichor" } };

    private static PointOfInterest BuildTarnIce() => new IcePointOfInterest(
        displayName: "Tarn Ice",
        descriptions: new() { "Black ice where the wind has swept the snow off, and under it, very slow, the shapes of fish" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new ArcticChar()) },
        moods: new[] { "black", "booming", "clear", "deep" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "anglery", ["listen"] = "hollow_ear", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildCorniceLip() => new DriftPointOfInterest(
        displayName: "Cornice Lip",
        descriptions: new() { "The lip of the cornice curling out over nothing, icicled underneath, its edge impossible to see from above" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "curling", "icicled", "deceptive", "vertiginous" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "snowcraft", ["contemplate"] = "awe" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new SnowLeopardArchetype(), 0.30);
        TrySpawnBeast(rng, scene, new WhiteWolfArchetype(),   0.15);

        TrySpawnCreature(rng, scene, new MountainGoatArchetype(), 0.40);
        TrySpawnCreature(rng, scene, new SnowHareArchetype(),     0.45);
        TrySpawnCreature(rng, scene, new PtarmiganArchetype(),    0.45);
        TrySpawnCreature(rng, scene, new ErmineArchetype(),       0.35);
        TrySpawnCreature(rng, scene, new PikaArchetype(),         0.45);
        TrySpawnCreature(rng, scene, new RavenArchetype(),        0.40);
        TrySpawnCreature(rng, scene, new SnowyOwlArchetype(),     0.20);
        TrySpawnCreatureIn<RunoutArea>(rng, scene, new WolverineArchetype(), 0.40);
        TrySpawnCreatureIn<KrummholzArea>(rng, scene, new WolverineArchetype(), 0.20);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 1, 3);
    }
}
