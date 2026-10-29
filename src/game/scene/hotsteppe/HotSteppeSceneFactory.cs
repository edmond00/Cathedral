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
public class HotSteppeSceneFactory : WildSceneFactory
{
    public HotSteppeSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Savanna, Thornveld, Tableland }

    private Identity _identity;
    private Area? _waterhole, _kopje, _kopjeFoot, _shelter, _river, _farBank;
    private readonly List<Area> _grass = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // Radial: every track in dry country leads to the water.
        Profile = LayoutProfile.Radial;
        _identity = (Identity)rng.Next(3);
        double W(double savanna, double thorn, double table)
            => _identity switch { Identity.Savanna => savanna, Identity.Thornveld => thorn, _ => table };

        // ── The hub ──────────────────────────────────────────────────────────
        _waterhole = BuildWaterhole();
        var hub = new List<Area> { _waterhole };
        if (rng.NextDouble() < 0.5) hub.Add(BuildMudWallow());
        if (rng.NextDouble() < 0.35) hub.Add(BuildSaltLick());

        // ── What hangs off it: dry country always, and one or two more ───────
        bool hasKopje = rng.NextDouble() < W(0.5, 0.4, 0.8);
        bool hasRiver = !hasKopje || rng.NextDouble() < 0.4;
        var dry = Draw(rng, new Option[]
        {
            new(BuildSavanna,     W(3.0, 1.0, 1.0)),
            new(BuildThornveld,   W(1.0, 3.0, 0.8)),
            new(BuildTableland,   W(0.6, 0.4, 3.0)),
            new(BuildBurntGround, 0.6),
            new(BuildTermitary,   W(1.2, 1.0, 0.6)),
        }, Math.Clamp(Between(rng, 6, 8) - hub.Count - (hasKopje ? 2 : 0) - (hasRiver ? 2 : 0), hub.Count == 1 ? 3 : 2, 3));
        _grass.AddRange(dry.Where(a => a is SavannaArea or ThornbrushArea or BurnArea or TermitaryArea));

        var kopje = new List<Area>();
        if (hasKopje)
        {
            kopje.Add(_kopjeFoot = BuildKopjeFoot());
            if (rng.NextDouble() < 0.7) kopje.Add(_kopje = BuildKopjeSummit());
            if (rng.NextDouble() < 0.45) kopje.Add(_shelter = BuildRockShelter());
        }
        var river = new List<Area>();
        if (hasRiver)
        {
            river.Add(_river = BuildSandRiver());
            if (rng.NextDouble() < 0.65) river.Add(BuildRiverwood());
            if (rng.NextDouble() < 0.5) river.Add(_farBank = BuildFarBank());
        }

        foreach (var area in hub.Concat(dry).Concat(kopje).Concat(river)) Populate(area, rng);

        AddSection(scene, "Waterhole", "Trampled mud round brown water, every track in the country leading to it",
            seed => new RadiantGenerator { Seed = seed, CentreDensity = 0.92f, EdgeDensity = 0.55f }, hub, tier: 0);
        AddSection(scene, _identity switch { Identity.Savanna => "Grass Plains", Identity.Thornveld => "Thornveld", _ => "High Veld" },
            "Yellow grass and thorn to the horizon under a white sky, the heat standing on it",
            seed => new NoisyGenerator { Seed = seed, Density = 0.82f }, dry, tier: 1);
        if (kopje.Count > 0)
            AddSection(scene, "Kopje", "A heap of granite boulders standing out of the grass, the only high ground for miles",
                seed => new WaveGenerator { Seed = seed }, kopje, tier: 1);
        if (river.Count > 0)
            AddSection(scene, "River Forest", "A ribbon of tall green trees along a brown river, in country that is otherwise dry",
                seed => new NoisyGenerator { Seed = seed, Density = 0.6f }, river, tier: 1);

        string[] moods = { "trampled", "dusty", "narrow" };
        string Way(Area a, Area b) =>
            a == _waterhole || b == _waterhole ? "Game Trail"
            : a is TablelandArea || b is TablelandArea || a is BoulderArea || b is BoulderArea ? "Rock Trail"
            : a is RiverArea || b is RiverArea || a is RiverwoodArea || b is RiverwoodArea ? "River Path"
            : "Grass Track";

        WeaveSection(rng, scene, hub, Weave.Hub, Way, "round the water", moods);
        WeaveSection(rng, scene, dry, RollWeave(rng), Way, "through the dry grass", moods);
        JoinSections(rng, scene, hub, dry, rng.NextDouble() < 0.5 ? 2 : 1, Way, "through the dry grass", moods);

        if (kopje.Count > 0)
        {
            OutdoorLayout.Link(scene, hub[rng.Next(hub.Count)], _kopjeFoot!, "Game Trail", moods, "through the dry grass");
            if (_kopje != null)
                AddViewpoint(scene, _kopjeFoot!, _kopje, "Kopje Boulders",
                    "A heap of granite boulders piled one on another, each the size of a house, the gaps between them dark",
                    new[] { "piled", "sun-hot", "cracked", "lichened" });
            if (_shelter != null)
                SlipInto(scene, _kopjeFoot!, _shelter, SlipKind.Crevice, "Boulder Gap",
                    "A dark gap between two of the great boulders, cool air coming out of it and the smell of old smoke",
                    new[] { "dark", "cool", "smoky" }, lesson: "potholing");
        }
        if (river.Count > 0)
        {
            var riverwood = river.OfType<RiverwoodArea>().FirstOrDefault();
            if (riverwood != null) OutdoorLayout.Link(scene, _river!, riverwood, "River Path", moods, "along the river");
            OutdoorLayout.Link(scene, hub[rng.Next(hub.Count)], riverwood != null && rng.NextDouble() < 0.5 ? riverwood : _river!,
                "Game Trail", moods, "down to the river");
            if (_farBank != null)
                Swim(scene, _river!, _farBank, WaterKind.River, "Brown River",
                    "The river, wide and slow and brown, something watching from the reeds on the far side",
                    new[] { "brown", "slow", "watched" }, lesson: "plunge");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Arid, _kopje, _shelter, _farBank);

        Console.WriteLine($"HotSteppeSceneFactory: {_identity}, {hub.Count}/{dry.Count}/{kopje.Count}/{river.Count} areas");
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

    private static Area BuildTermitary() => new TermitaryArea(
        displayName: "Termite Country",
        contextDescription: "among the termite mounds",
        transitionDescription: "walk out among the termite mounds",
        descriptions: new() { "Ground crowded with termite mounds taller than a man, red clay towers fluted like churches, the grass short between them" },
        moods: new[] { "towered", "red", "crowded", "strange" });

    private static Area BuildWaterhole() => new WaterholeArea(
        displayName: "Waterhole",
        contextDescription: "at the waterhole",
        transitionDescription: "come down to the waterhole",
        descriptions: new() { "A pan of brown water in a ring of trampled mud, hoofprints and pawprints laid over each other" },
        moods: new[] { "trampled", "muddy", "crowded", "tense" });

    private static Area BuildMudWallow() => new WallowArea(
        displayName: "Mud Wallow",
        contextDescription: "at the mud wallow",
        transitionDescription: "come to the mud wallow",
        descriptions: new() { "A hollow of churned grey mud beside the water, polished rubbing-trees at its edge, the smell of pig and buffalo" },
        moods: new[] { "churned", "grey", "rank", "polished" });

    private static Area BuildSaltLick() => new LickArea(
        displayName: "Salt Lick",
        contextDescription: "at the salt lick",
        transitionDescription: "walk over to the salt lick",
        descriptions: new() { "A bare patch of whitish earth gouged into hollows by the tongues and tusks of every animal for miles" },
        moods: new[] { "bare", "gouged", "whitish", "much-visited" });

    private static Area BuildKopjeFoot() => new BoulderArea(
        displayName: "Kopje Foot",
        contextDescription: "at the foot of the kopje",
        transitionDescription: "walk in under the kopje",
        descriptions: new() { "The foot of a granite outcrop, boulders as big as houses piled above, a fig tree growing out of a crack" },
        moods: new[] { "boulder-piled", "shaded", "watchful", "hot" });

    private static Area BuildKopjeSummit() => new KopjeArea(
        displayName: "Kopje Summit",
        contextDescription: "on top of the kopje",
        transitionDescription: "pull yourself onto the kopje's crown",
        descriptions: new() { "The rounded crown of a granite outcrop, high above the grass, the whole country spread out beneath" },
        moods: new[] { "high", "rounded", "hot-stoned", "commanding" });

    private static Area BuildRockShelter() => new OverhangArea(
        displayName: "Painted Shelter",
        contextDescription: "in the painted shelter",
        transitionDescription: "squeeze into the shelter between the boulders",
        descriptions: new() { "A cool space under a leaning boulder, its underside painted with running animals in red and white" },
        moods: new[] { "cool", "painted", "hidden", "smoky" });

    private static Area BuildSandRiver() => new RiverArea(
        displayName: "Brown River",
        contextDescription: "on the river bank",
        transitionDescription: "come down to the river",
        descriptions: new() { "A wide slow river, brown with mud, sandbanks showing, reeds along the edges" },
        moods: new[] { "brown", "slow", "reedy", "watched" });

    private static Area BuildRiverwood() => new RiverwoodArea(
        displayName: "River Forest",
        contextDescription: "in the river forest",
        transitionDescription: "walk in under the river trees",
        descriptions: new() { "Tall fever trees and figs along the water, green-shaded and loud with birds after the glare outside" },
        moods: new[] { "green", "shaded", "loud", "cool" });

    private static Area BuildFarBank() => new SavannaArea(
        displayName: "Far Bank",
        contextDescription: "on the far bank",
        transitionDescription: "drag yourself up the far bank",
        descriptions: new() { "The other side of the river, grass and thorn going away unwalked, the tracks of big animals in the mud" },
        moods: new[] { "unwalked", "muddy", "open", "far" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        if (area == _farBank)
        {
            p.Add(Tussock());
            p.Add(ClimateTerrainSubfactory.BuildAcacia());
            return;
        }
        switch (area)
        {
            case SavannaArea:
                p.Add(ClimateTerrainSubfactory.BuildAcacia());
                p.Add(Tussock());
                p.Add(BuildTermiteMound());
                if (rng.NextDouble() < 0.3) p.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case ThornbrushArea:
                p.Add(ClimateTerrainSubfactory.BuildThornBush());
                p.Add(ClimateTerrainSubfactory.BuildAcacia());
                if (rng.NextDouble() < 0.5) p.Add(BuildTermiteMound());
                if (rng.NextDouble() < 0.4) p.Add(ClimateTerrainSubfactory.BuildAloe());
                break;
            case TablelandArea:
                p.Add(ClimateTerrainSubfactory.BuildBaobab());
                p.Add(BuildRockPavement());
                if (rng.NextDouble() < 0.4) p.Add(BuildWildHive());
                if (rng.NextDouble() < 0.35) p.Add(ClimateTerrainSubfactory.BuildPetroglyphs());
                break;
            case BurnArea:
                p.Add(BuildCharredStumps());
                p.Add(Tussock());
                break;
            case TermitaryArea:
                p.Add(BuildTermiteCathedrals());
                p.Add(Tussock());
                break;
            case WaterholeArea:
                p.Add(BuildWaterholePool());
                p.Add(rng.NextDouble() < 0.5 ? ClimateTerrainSubfactory.BuildBaobab() : ClimateTerrainSubfactory.BuildAcacia());
                if (rng.NextDouble() < 0.4) p.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case WallowArea:
                p.Add(BuildWallow());
                break;
            case LickArea:
                p.Add(BuildLick());
                break;
            case BoulderArea:
                p.Add(TerrainSubfactory.BuildBoulder());
                if (rng.NextDouble() < 0.5) p.Add(BuildWildHive());
                break;
            case KopjeArea:
                p.Add(BuildRainBasin());
                p.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.5) p.Add(ClimateTerrainSubfactory.BuildPetroglyphs());
                break;
            case OverhangArea:
                p.Add(BuildRockPaintings());
                break;
            case RiverArea:
                p.Add(BuildRiverEdge());
                p.Add(TerrainSubfactory.BuildReedBed());
                break;
            case RiverwoodArea:
                p.Add(BuildFeverTrees());
                p.Add(BuildWildHive());
                break;
        }
    }

    private static PointOfInterest BuildLick() => new CrustPointOfInterest(
        displayName: "Lick Earth",
        descriptions: new() { "The salty earth itself, licked smooth in hollows, tusk-gouged in others, salt crystals glittering in the cracks" },
        items: new() { new ItemElement(new Salt()), new ItemElement(new Clay()) },
        moods: new[] { "licked", "gouged", "glittering", "trodden" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading" } };

    private static PointOfInterest BuildTermiteCathedrals() => new MoundPointOfInterest(
        displayName: "Termite Cathedrals",
        descriptions: new() { "Mounds of red clay twice a man's height, fluted and spired, warm to the hand and humming faintly inside" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Clay()) },
        moods: new[] { "spired", "red", "humming", "warm" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["contemplate"] = "awe", ["listen"] = "insect_chorus" } };

    private static PointOfInterest BuildRockPaintings() => new PetroglyphPointOfInterest(
        displayName: "Rock Paintings",
        descriptions: new() { "Eland, giraffe and little running hunters painted in ochre and white on the rock overhead, some of them very old" },
        items: new() { new ItemElement(new Ochre()) },
        moods: new[] { "running", "ochre", "faded", "alive" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildRiverEdge() => new StreamPointOfInterest(
        displayName: "River Edge",
        descriptions: new() { "The brown water sliding past the bank, a slide in the mud where something heavy goes in and out" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Reed()) },
        moods: new[] { "sliding", "brown", "slick", "uneasy" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildFeverTrees() => new TreePointOfInterest(
        displayName: "Fever Trees",
        descriptions: new() { "Tall yellow-barked thorn trees standing along the water, their bark powdery, weaver nests hanging from them" },
        items: new() { new ItemElement(new Bark()), new ItemElement(new Branch()) },
        moods: new[] { "yellow-barked", "tall", "nest-hung", "powdery" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildWildHive() => new HivePointOfInterest(
        displayName: "Wild Hive",
        descriptions: new() { "A crack in a tree or a rock with bees pouring in and out of it, combs showing dark and heavy inside" },
        items: new() { new ItemElement(new WildHoney()), new ItemElement(new Beeswax()) },
        moods: new[] { "pouring", "humming", "dark-combed", "dangerous" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["gather"] = "honey_hunting", ["grab"] = "honey_hunting" } };

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
    ) { Play = new MudPuddlePlay(), Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading", ["smell"] = "musk_reading" } };

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
        TrySpawnCreatureIn<RiverwoodArea>(rng, scene, new HoneyguideArchetype(), 0.65);
        TrySpawnCreatureIn<TablelandArea>(rng, scene, new HoneyguideArchetype(), 0.30);
        TrySpawnCreatureIn<RiverwoodArea>(rng, scene, new KingfisherArchetype(), 0.40);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Arid, 2, 4);
    }
}
