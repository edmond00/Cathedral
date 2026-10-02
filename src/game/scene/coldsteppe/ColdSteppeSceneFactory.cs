using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
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
public class ColdSteppeSceneFactory : ClimateSceneFactory
{
    public ColdSteppeSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Tundra, Steppe, Barrens }

    private Identity _identity;
    private Area? _pingo, _thaw;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        _identity = (Identity)rng.Next(3);

        var open = _identity switch
        {
            Identity.Tundra => new List<Area> { BuildTundra(), BuildTussockSteppe() },
            Identity.Steppe => new List<Area> { BuildTussockSteppe(), BuildEsker() },
            _               => new List<Area> { BuildBarrens(), BuildTundra() },
        };
        if (rng.NextDouble() < 0.5)
            open.Add(_identity == Identity.Steppe ? BuildBarrens() : BuildEsker());

        _thaw = BuildThawLake();
        _pingo = BuildPingo();

        foreach (var area in open.Append(_thaw).Append(_pingo)) Populate(area, rng);

        AddSection(scene, "Open Steppe", "Low ground to the edge of the world under a hard grey sky, the wind never stopping",
            seed => new NoisyGenerator { Seed = seed, Density = 0.86f }, open.Append(_pingo));
        AddSection(scene, "Low Ground", "A thaw lake and the sedge round it, loud in the short summer, dead still in the long winter",
            seed => new RadiantGenerator { Seed = seed, CentreDensity = 0.92f, EdgeDensity = 0.6f }, new[] { _thaw });

        ConnectChain(scene, open.Append(_thaw).ToList(),
            (a, b) => a is ThawArea || b is ThawArea ? "Sedge Path"
                    : a is EskerArea || b is EskerArea ? "Ridge Path"
                    : "Herd Trail",
            "over the steppe", new[] { "trodden", "wind-flattened", "frost-heaved" });

        AddViewpoint(scene, open[0], _pingo, "Pingo Slope",
            "The steep green flank of a pingo, its turf split and slumping where the ice inside it heaves",
            new[] { "steep", "split", "slumping", "frost-heaved" });

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _pingo);

        Console.WriteLine($"ColdSteppeSceneFactory: {_identity}, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

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
            case PingoArea:
                area.PointsOfInterest.Add(BuildSplitCrown());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
        }
    }

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
        TrySpawnCreature(rng, scene, new CraneArchetype(),       0.40, new[] { _thaw! });

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 2, 4);
    }
}
