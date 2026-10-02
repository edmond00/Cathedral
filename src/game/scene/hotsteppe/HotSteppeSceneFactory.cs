using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.HotSteppe;

/// <summary>
/// Builds a hot steppe — what mountain and peak become in hot country (see <c>ClimateRule</c>):
/// the high ground baked into tableland and dry grass, the ranges worn to kopjes.
///
/// Identity: savanna (open grass under acacias), thornveld (thorn scrub) or tableland (bare high
/// rock) — gates the dry-country areas. A waterhole is always present; a burn scar sometimes.
/// Climb: the boulders of a kopje, whose summit sees the whole place.
/// Uninhabited: no settlement stands in hot country yet.
/// </summary>
public class HotSteppeSceneFactory : ClimateSceneFactory
{
    public HotSteppeSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Savanna, Thornveld, Tableland }

    private Identity _identity;
    private Area? _waterhole, _kopje;
    private readonly List<Area> _grass = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        _identity = (Identity)rng.Next(3);
        bool burnt = rng.NextDouble() < 0.30;

        var dry = _identity switch
        {
            Identity.Savanna   => new List<Area> { BuildSavanna(), BuildThornveld() },
            Identity.Thornveld => new List<Area> { BuildThornveld(), BuildSavanna() },
            _                  => new List<Area> { BuildTableland(), BuildSavanna() },
        };
        if (_identity != Identity.Tableland && rng.NextDouble() < 0.4) dry.Add(BuildTableland());
        if (burnt) dry.Add(BuildBurntGround());
        _grass.AddRange(dry.Where(a => a is SavannaArea or ThornbrushArea or BurnArea));

        _waterhole = BuildWaterhole();
        _kopje = BuildKopjeSummit();

        foreach (var area in dry.Append(_waterhole).Append(_kopje))
            Populate(area, rng);

        AddSection(scene, "Dry Country", "Yellow grass and thorn to the horizon under a white sky, the heat standing on it",
            seed => new NoisyGenerator { Seed = seed, Density = 0.82f },
            dry.Append(_kopje));
        AddSection(scene, "Waterhole", "Trampled mud round brown water, every track in the country leading to it",
            seed => new RadiantGenerator { Seed = seed, CentreDensity = 0.92f, EdgeDensity = 0.55f },
            new[] { _waterhole });

        // The waterhole sits in the middle of the chain: every path in the country goes by it.
        var chain = dry.ToList();
        chain.Insert(Math.Min(1, chain.Count), _waterhole);
        ConnectChain(scene, chain,
            (a, b) => a == _waterhole || b == _waterhole ? "Game Trail"
                    : a is TablelandArea || b is TablelandArea ? "Rock Trail"
                    : "Grass Track",
            "through the dry grass", new[] { "trampled", "dusty", "narrow" });

        AddViewpoint(scene, dry[0], _kopje, "Kopje Boulders",
            "A heap of granite boulders piled one on another, each the size of a house, the gaps between them dark",
            new[] { "piled", "sun-hot", "cracked", "lichened" });

        Furnish(rng, scene, FurnitureSubfactory.Setting.Arid, _kopje);

        Console.WriteLine($"HotSteppeSceneFactory: {_identity}, burnt={burnt}, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSavanna() => new SavannaArea(
        displayName: "Savanna",
        contextDescription: "out on the savanna",
        transitionDescription: "wade out into the long grass",
        descriptions: new() { "Grass to the waist, gold and dry, with a flat-topped tree every few hundred paces" },
        moods: new[] { "gold", "rustling", "wide", "watchful" });

    private static Area BuildThornveld() => new ThornbrushArea(
        displayName: "Thornveld",
        contextDescription: "in the thornveld",
        transitionDescription: "pick a way into the thornveld",
        descriptions: new() { "A grey maze of thorn scrub head-high, the ways through it made by animals" },
        moods: new[] { "grey", "hooked", "close", "humming" });

    private static Area BuildTableland() => new TablelandArea(
        displayName: "Tableland",
        contextDescription: "up on the tableland",
        transitionDescription: "climb onto the tableland",
        descriptions: new() { "A shelf of bare red rock and thin grass, flat as a floor, ending at a long drop on one side" },
        moods: new[] { "flat", "red", "exposed", "baking" });

    private static Area BuildBurntGround() => new BurnArea(
        displayName: "Burnt Ground",
        contextDescription: "on the burnt ground",
        transitionDescription: "step onto the burnt ground",
        descriptions: new() { "A black scar where the grass fire went through, ash lifting at each step, green already pricking" },
        moods: new[] { "black", "ashen", "smoking", "renewing" });

    private static Area BuildWaterhole() => new WaterholeArea(
        displayName: "Waterhole",
        contextDescription: "at the waterhole",
        transitionDescription: "come down to the waterhole",
        descriptions: new() { "A pan of brown water in a ring of trampled mud, hoofprints and pawprints laid over each other" },
        moods: new[] { "trampled", "muddy", "crowded", "tense" });

    private static Area BuildKopjeSummit() => new KopjeArea(
        displayName: "Kopje Summit",
        contextDescription: "on top of the kopje",
        transitionDescription: "pull yourself onto the kopje's crown",
        descriptions: new() { "The rounded crown of a granite outcrop, high above the grass, the whole country spread out beneath" },
        moods: new[] { "high", "rounded", "hot-stoned", "commanding" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area.DisplayName)
        {
            case "Savanna":
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildAcacia());
                area.PointsOfInterest.Add(Tussock());
                area.PointsOfInterest.Add(BuildTermiteMound());
                if (rng.NextDouble() < 0.3) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case "Thornveld":
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildThornBush());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildAcacia());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildTermiteMound());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildAloe());
                break;
            case "Tableland":
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBaobab());
                area.PointsOfInterest.Add(BuildRockPavement());
                if (rng.NextDouble() < 0.35) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildPetroglyphs());
                break;
            case "Burnt Ground":
                area.PointsOfInterest.Add(BuildCharredStumps());
                area.PointsOfInterest.Add(Tussock());
                break;
            case "Waterhole":
                area.PointsOfInterest.Add(BuildWaterholePool());
                area.PointsOfInterest.Add(BuildWallow());
                area.PointsOfInterest.Add(rng.NextDouble() < 0.5 ? ClimateTerrainSubfactory.BuildBaobab() : ClimateTerrainSubfactory.BuildAcacia());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case "Kopje Summit":
                area.PointsOfInterest.Add(BuildRainBasin());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildPetroglyphs());
                break;
        }
    }

    private static PointOfInterest Tussock() => ClimateTerrainSubfactory.BuildTussockGrass(
        () => new WildMillet(), () => new Straw(),
        "Tussocks of tall dry grass, their seed heads drooping, rattling together in the hot wind");

    private static PointOfInterest BuildTermiteMound() => new MoundPointOfInterest(
        displayName: "Termite Mound",
        descriptions: new() { "A spire of baked red earth taller than a man, its chimneys breathing warm air" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Ochre()) },
        moods: new[] { "towering", "red", "breathing", "busy" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "architecture", ["listen"] = "swarm_sense" } };

    private static PointOfInterest BuildRockPavement() => new RockPointOfInterest(
        displayName: "Rock Pavement",
        descriptions: new() { "Bare red rock split by the heat into flat slabs, grass growing in the joints between them" },
        items: new() { new ItemElement(new Sandstone()), new ItemElement(new Rock()), new ItemElement(new Flint()) },
        moods: new[] { "flat", "jointed", "red", "baking" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    private static PointOfInterest BuildCharredStumps() => new StumpPointOfInterest(
        displayName: "Charred Stumps",
        descriptions: new() { "Thorn trees burnt down to black stumps, some still smoking at the root" },
        items: new() { new ItemElement(new Twig()), new ItemElement(new Thornwood()) },
        moods: new[] { "black", "smoking", "brittle", "bitter" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft", ["smell"] = "smoke_reading" } };

    private static PointOfInterest BuildWaterholePool() => new PoolPointOfInterest(
        displayName: "Muddy Waterhole",
        descriptions: new() { "Brown warm water shrunk back from its banks, its edges churned to porridge by a thousand feet" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Catfish()) },
        moods: new[] { "brown", "warm", "shrinking", "churned" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "spoor_reading", ["listen"] = "water_voice", ["smell"] = "taint_sense", ["contemplate"] = "fellow_feeling" } };

    private static PointOfInterest BuildWallow() => new MudPointOfInterest(
        displayName: "Mud Wallow",
        descriptions: new() { "A hollow of grey mud polished by the bellies of whatever comes here to roll" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Clay()) },
        moods: new[] { "grey", "polished", "rank", "cool" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading", ["smell"] = "musk_reading" } };

    private static PointOfInterest BuildRainBasin() => new PoolPointOfInterest(
        displayName: "Rain Basin",
        descriptions: new() { "A smooth bowl worn into the granite, holding a little green rainwater and a skin of dust" },
        items: new() { new ItemElement(new Moss()) },
        moods: new[] { "smooth", "still", "green", "precious" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["contemplate"] = "meditation" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new LionArchetype(),    0.35);
        TrySpawnBeast(rng, scene, new HyenaArchetype(),   0.30);
        TrySpawnBeast(rng, scene, new WarthogArchetype(), 0.40);

        TrySpawnCreature(rng, scene, new GazelleArchetype(),    0.60);
        TrySpawnCreature(rng, scene, new ZebraArchetype(),      0.45);
        TrySpawnCreature(rng, scene, new OstrichArchetype(),    0.35);
        TrySpawnCreature(rng, scene, new MeerkatArchetype(),    0.50);
        TrySpawnCreature(rng, scene, new WeaverBirdArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new VultureArchetype(),    0.45);
        TrySpawnCreature(rng, scene, new TortoiseArchetype(),   0.30);
        if (_grass.Count > 0) TrySpawnCreature(rng, scene, new TermiteArchetype(), 0.70, _grass);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Arid, 2, 4);
    }
}
