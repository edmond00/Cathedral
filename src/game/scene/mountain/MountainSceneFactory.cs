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

namespace Cathedral.Game.Scene.Mountain;

/// <summary>
/// Builds a mountainside, laid out as the land is: a pyramid.
///
/// <para><b>Three bands.</b> The foot (tier 0) is the widest — three or four areas of wood, scree,
/// stream, gorge or falling water. The middle band (tier 1) is narrower, two or three of ledge,
/// boulder field, alpine meadow, tarn, col, couloir and shieling, and meets the foot by one or two
/// ways up. The heights (tier 2) are the high crag and sometimes an eyrie above it, and no path goes
/// there: the cliff from the middle band is the only way, and the crag sees the whole place.</para>
///
/// <para><b>Three mountains.</b> The identity roll weights the draw: a <i>wooded</i> mountain is pine
/// to the shoulder, a <i>pastoral</i> one is grazing and summer huts, a <i>rugged</i> one is scree,
/// gorge and gully. The bands take their names from it too.</para>
///
/// <para><b>Ways that are not a walk.</b> A waterfall's plunge pool may hide a grotto behind the
/// fall, reached only by swimming; a ledge or a boulder field may hide a cleft in the rock, reached only
/// by squeezing in; a rugged mountain's way up may be a scree traverse instead of a path; and the
/// heights are a climb.</para>
/// </summary>
public class MountainSceneFactory : WildSceneFactory
{
    public MountainSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Wooded, Pastoral, Rugged }

    private Identity _identity;
    private Area? _crag;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Profile = LayoutProfile.Pyramid;
        _identity = (Identity)rng.Next(3);
        double W(double wooded, double pastoral, double rugged)
            => _identity switch { Identity.Wooded => wooded, Identity.Pastoral => pastoral, _ => rugged };

        // ── The foot ─────────────────────────────────────────────────────────
        var foot = Draw(rng, new Option[]
        {
            new(BuildSlopeForest, W(3.0, 1.0, 0.8)),
            new(BuildScreeField,  W(0.8, 0.8, 2.5)),
            new(BuildStreamSource, 1.0),
            new(BuildGorge,       W(0.6, 0.4, 2.0)),
            new(BuildWaterfall,   W(1.2, 1.0, 1.4)),
            new(BuildLowerMeadow, W(0.6, 2.5, 0.3)),
        }, Between(rng, 3, 4));

        // ── The middle band: never wider than the foot ───────────────────────
        var middle = Draw(rng, new Option[]
        {
            new(BuildRockLedge,    1.6),
            new(BuildBoulderField, W(1.0, 0.8, 1.8)),
            new(BuildAlpineMeadow, W(1.0, 2.5, 0.4)),
            new(BuildTarn,         1.0),
            new(BuildCol,          1.2),
            new(BuildCouloir,      W(0.6, 0.3, 2.0)),
            new(BuildShieling,     W(0.4, 3.0, 0.2)),
        }, Math.Min(foot.Count - 1, Between(rng, 2, 3)));

        // ── The heights ──────────────────────────────────────────────────────
        _crag = BuildHighCrag();
        var heights = new List<Area> { _crag };
        Area? eyrie = rng.NextDouble() < 0.45 ? BuildEyrie() : null;
        if (eyrie != null) heights.Add(eyrie);

        // Hidden places, each reached by something other than a walk.
        var waterfall = foot.OfType<WaterfallArea>().FirstOrDefault();
        Area? grotto = waterfall != null && rng.NextDouble() < 0.55 ? BuildGrottoBehindFalls() : null;
        if (grotto != null) foot.Add(grotto);
        var rockFoot = middle.FirstOrDefault(a => a is LedgeArea or BoulderArea);
        Area? cleft = rockFoot != null && rng.NextDouble() < 0.45 ? BuildCleft() : null;
        if (cleft != null) middle.Add(cleft);

        foreach (var area in foot.Concat(middle).Concat(heights)) Populate(area, rng);

        var (footName, middleName) = _identity switch
        {
            Identity.Wooded   => ("Pine Belt",   "Rocky Midslope"),
            Identity.Pastoral => ("Lower Slope", "High Pasture"),
            _                 => ("Foothills",   "The Shoulder"),
        };
        AddSection(scene, footName, "The lower mountain, where the slope still carries trees, water and soil",
            seed => new NoisyGenerator { Seed = seed, Density = 0.7f }, foot, tier: 0);
        AddSection(scene, middleName, "Open, exposed ground above the trees, rock showing through everywhere",
            seed => new WaveGenerator { Seed = seed }, middle, tier: 1);
        AddSection(scene, "The Heights", "Bare rock at the top of the climb, the whole country laid out below",
            seed => new WaveGenerator { Seed = seed }, heights, tier: 2);

        // ── Ways ─────────────────────────────────────────────────────────────
        string[] moods = { "rough", "exposed", "windswept" };
        static string Way(Area a, Area b) =>
            a is GorgeArea || b is GorgeArea ? "Gorge Passage"
            : a is StreamArea || b is StreamArea || a is WaterfallArea || b is WaterfallArea ? "Stream Track"
            : a is ForestArea || b is ForestArea ? "Forest Path"
            : "Slope Path";

        // A hidden place is reached by its own way only, so it stays out of the weave.
        var footWalk   = foot.Where(a => a != grotto).ToList();
        var middleWalk = middle.Where(a => a != cleft).ToList();
        WeaveSection(rng, scene, footWalk, RollWeave(rng), Way, "up the mountainside", moods);
        WeaveSection(rng, scene, middleWalk, RollWeave(rng, Weave.Line, Weave.Hub, Weave.Branching), Way,
            "across the slope", moods);

        // The way up from the foot: a zigzag path, or on a rugged mountain sometimes a scree traverse.
        JoinSections(rng, scene, footWalk, middleWalk, rng.NextDouble() < 0.35 ? 2 : 1,
            (_, _) => "Zigzag Path", "up the mountainside", moods);
        if (_identity == Identity.Rugged && rng.NextDouble() < 0.6)
        {
            var low  = footWalk[rng.Next(footWalk.Count)];
            var high = middleWalk[rng.Next(middleWalk.Count)];
            if (!scene.AreaGraph.TryGetValue(low.Id, out var r) || !r.Contains(high.Id))
                Cross(scene, low, high, CrossingKind.Scree, "Scree Traverse",
                    "A long diagonal across a slope of loose stone, every step setting the slope above it moving",
                    new[] { "loose", "diagonal", "grey" }, "stonework");
        }

        // The heights: a climb from the middle band, and from the crag a sight of everything.
        var foothold = middleWalk.FirstOrDefault(a => a is LedgeArea) ?? middleWalk[rng.Next(middleWalk.Count)];
        AddViewpoint(scene, foothold, _crag, "Cliff Ascent",
            "A sheer cliff rising toward the crag above, hand-and-foot holds in the rock",
            new[] { "sheer", "exposed", "vertiginous" });
        if (eyrie != null)
            Climb(scene, _crag, eyrie, "Eyrie Ledge",
                "A narrow ledge running out along the crag's face to a heap of sticks where a great bird nests",
                new[] { "narrow", "airy", "guano-streaked" });

        if (grotto != null)
            Swim(scene, waterfall!, grotto, WaterKind.Pond, "Plunge Pool",
                "The deep green pool under the fall, churning white where the water strikes it; behind the curtain of water there is a dark gap",
                new[] { "churning", "icy", "deep" }, lesson: "plunge");
        if (cleft != null)
            SlipInto(scene, rockFoot!, cleft, SlipKind.Crevice, "Rock Cleft",
                "A crack in the rock a body's width, going back into darkness and opening out somewhere inside",
                new[] { "narrow", "dark", "cold-breathing" }, lesson: "potholing");

        Furnish(rng, scene, FurnitureSubfactory.Setting.Highland, _crag, eyrie, grotto, cleft);

        Console.WriteLine($"MountainSceneFactory: {_identity}, {foot.Count}/{middle.Count}/{heights.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildScreeField() => new ScreeArea(
        displayName: "Scree Field",
        contextDescription: "crossing the scree field",
        transitionDescription: "step onto the scree field",
        descriptions: new() { "A long slope of loose broken stone, treacherous underfoot" },
        moods: new[] { "loose", "grey", "treacherous", "exposed" });

    private static Area BuildRockLedge() => new LedgeArea(
        displayName: "Rock Ledge",
        contextDescription: "on the rock ledge",
        transitionDescription: "step onto the rock ledge",
        descriptions: new() { "A flat shelf of rock with a wide view across the country below" },
        moods: new[] { "exposed", "windswept", "wide-open", "high" });

    /// <summary>The top of the cliff. What it is for is the view, not what is lying about on it.</summary>
    private static Area BuildHighCrag() => new CragArea(
        displayName: "High Crag",
        contextDescription: "on the high crag",
        transitionDescription: "pull yourself onto the high crag",
        descriptions: new() { "A wind-scoured spur of rock standing clear of the slope, with nothing above it but sky" },
        moods: new[] { "wind-scoured", "airy", "bare", "commanding" });

    private static Area BuildAlpineMeadow() => new MeadowArea(
        displayName: "Alpine Meadow",
        contextDescription: "in the alpine meadow",
        transitionDescription: "step into the alpine meadow",
        descriptions: new() { "A sheltered hollow of unexpected greenery, herbs growing in soft drifts" },
        moods: new[] { "sheltered", "fragrant", "green", "still" });

    private static Area BuildLowerMeadow() => new MeadowArea(
        displayName: "Hay Meadow",
        contextDescription: "in the hay meadow",
        transitionDescription: "wade into the hay meadow",
        descriptions: new() { "A steep meadow of long grass and flowers, scythed once a summer by somebody who carries it down on their back" },
        moods: new[] { "steep", "flowered", "humming", "sunlit" });

    private static Area BuildGorge() => new GorgeArea(
        displayName: "Gorge",
        contextDescription: "in the narrow gorge",
        transitionDescription: "enter the narrow gorge",
        descriptions: new() { "A narrow cut between rock walls, a stream running at its base" },
        moods: new[] { "narrow", "echoing", "wet", "cool" });

    private static Area BuildBoulderField() => new BoulderArea(
        displayName: "Boulder Field",
        contextDescription: "in the boulder field",
        transitionDescription: "pick a way through the boulder field",
        descriptions: new() { "A jumble of massive rocks the size of houses, paths winding between" },
        moods: new[] { "massive", "stilled", "exposed", "monumental" });

    private static Area BuildStreamSource() => new StreamArea(
        displayName: "Stream Source",
        contextDescription: "at the stream source",
        transitionDescription: "approach the stream source",
        descriptions: new() { "A spring breaks from the rock, water cold and bright" },
        moods: new[] { "cold", "bright", "running", "fresh" });

    private static Area BuildSlopeForest() => new ForestArea(
        displayName: "Slope Forest",
        contextDescription: "in the slope forest",
        transitionDescription: "step into the slope forest",
        descriptions: new() { "Dense wood climbs the lower slope, pine giving way to scrub above" },
        moods: new[] { "tilted", "rooted", "shaded", "tall" });

    private static Area BuildWaterfall() => new WaterfallArea(
        displayName: "Waterfall",
        contextDescription: "at the foot of the waterfall",
        transitionDescription: "follow the roar to the waterfall",
        descriptions: new() { "A white fall of water dropping the height of a church tower into a deep pool, spray drifting over everything" },
        moods: new[] { "roaring", "spray-soaked", "green", "cold" });

    private static Area BuildGrottoBehindFalls() => new GrottoArea(
        displayName: "Behind the Falls",
        contextDescription: "in the hollow behind the falls",
        transitionDescription: "haul yourself out behind the curtain of water",
        descriptions: new() { "A dry hollow in the rock behind the falling water, the world outside a roaring green-white blur" },
        moods: new[] { "hidden", "roaring", "dim", "dripping" });

    private static Area BuildTarn() => new TarnArea(
        displayName: "Tarn",
        contextDescription: "on the shore of the tarn",
        transitionDescription: "come down to the tarn",
        descriptions: new() { "A small black lake in a hollow of the mountain, so still the crags stand upside down in it" },
        moods: new[] { "black", "still", "mirroring", "cold" });

    private static Area BuildCol() => new ColArea(
        displayName: "Col",
        contextDescription: "on the col",
        transitionDescription: "climb up onto the col",
        descriptions: new() { "A saddle of turf and stone between two heights, the wind pouring through it from the far side of the mountain" },
        moods: new[] { "windy", "open", "between", "cold" });

    private static Area BuildCouloir() => new CouloirArea(
        displayName: "Couloir",
        contextDescription: "in the couloir",
        transitionDescription: "step into the mouth of the couloir",
        descriptions: new() { "A steep gully running down the mountain, choked with old grey snow and the stones that fall onto it" },
        moods: new[] { "steep", "funnelling", "rattling", "shadowed" });

    private static Area BuildShieling() => new ShielingArea(
        displayName: "Shieling",
        contextDescription: "at the shieling",
        transitionDescription: "walk up to the shieling",
        descriptions: new() { "A shelf of good grazing high on the mountain, with a low stone hut and the trodden rings where the herds were folded" },
        moods: new[] { "grassy", "deserted", "sheltered", "trodden" });

    private static Area BuildEyrie() => new EyrieArea(
        displayName: "Eyrie",
        contextDescription: "on the eyrie ledge",
        transitionDescription: "edge out onto the eyrie ledge",
        descriptions: new() { "A ledge on the crag's face heaped with sticks and bones, the drop below it so long the wind has a voice in it" },
        moods: new[] { "airy", "bone-strewn", "giddy", "wild" });

    private static Area BuildCleft() => new CleftArea(
        displayName: "Cleft",
        contextDescription: "inside the cleft",
        transitionDescription: "squeeze into the cleft",
        descriptions: new() { "A tall narrow space inside the rock, a slot of sky far above, the air still and cold as a cellar" },
        moods: new[] { "narrow", "still", "dim", "secret" });

    // ── Contents ─────────────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        switch (area)
        {
            case ScreeArea:
                p.Add(TerrainSubfactory.BuildFallenRocks());
                p.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.3) p.Add(TerrainSubfactory.BuildCairn());
                break;
            case LedgeArea:
                p.Add(TerrainSubfactory.BuildRockOutcrop());
                if (_identity != Identity.Rugged && rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildAlpineHerbPatch());
                break;
            case MeadowArea:
                p.Add(TerrainSubfactory.BuildFlowerPatch());
                p.Add(TerrainSubfactory.BuildAlpineHerbPatch());
                if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildBerryBush());
                if (rng.NextDouble() < 0.4) p.Add(BuildGentianPatch());
                break;
            case GorgeArea:
                p.Add(TerrainSubfactory.BuildGorgePool());
                p.Add(TerrainSubfactory.BuildRockFace());
                break;
            case BoulderArea:
                p.Add(TerrainSubfactory.BuildBoulder());
                p.Add(TerrainSubfactory.BuildBoulder());
                p.Add(TerrainSubfactory.BuildCrevice());
                break;
            case StreamArea:
                p.Add(TerrainSubfactory.BuildStreamBank());
                if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildMossBank());
                break;
            case ForestArea:
                p.Add(TerrainSubfactory.BuildPineTree());
                p.Add(TerrainSubfactory.BuildPineTree());
                p.Add(TerrainSubfactory.BuildMossBank());
                if (rng.NextDouble() < 0.3) p.Add(TerrainSubfactory.BuildMushroomCluster());
                break;
            case WaterfallArea:
                p.Add(BuildFalls());
                p.Add(TerrainSubfactory.BuildMossBank());
                break;
            case GrottoArea:
                p.Add(BuildDryLedge());
                break;
            case TarnArea:
                p.Add(BuildTarnShore());
                if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildReedBed());
                break;
            case ColArea:
                p.Add(TerrainSubfactory.BuildCairn());
                p.Add(BuildWindGap());
                break;
            case CouloirArea:
                p.Add(TerrainSubfactory.BuildFallenRocks());
                p.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case ShielingArea:
                p.Add(BuildBothy());
                p.Add(BuildFoldRings());
                p.Add(TerrainSubfactory.BuildAlpineHerbPatch());
                break;
            case EyrieArea:
                p.Add(BuildEyrieNest());
                break;
            case CleftArea:
                p.Add(BuildStoatLarder());
                break;
        }
    }

    private static PointOfInterest BuildFalls() => new FallsPointOfInterest(
        displayName: "Falling Water",
        descriptions: new() { "The fall itself, a rope of white water unravelling into spray before it reaches the pool" },
        items: new() { new ItemElement(new Moss()), new ItemElement(new Fern()) },
        moods: new[] { "roaring", "white", "unravelling", "endless" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "plunge", ["listen"] = "water_voice", ["smell"] = "petrichor", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildDryLedge() => new HollowPointOfInterest(
        displayName: "Dry Ledge",
        descriptions: new() { "A ledge at the back of the hollow where the spray never reaches, a scatter of old bones and feathers on it" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Feather()), new ItemElement(new Moss()) },
        moods: new[] { "dry", "hidden", "littered", "quiet" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildTarnShore() => new PoolPointOfInterest(
        displayName: "Tarn Water",
        descriptions: new() { "The edge of the tarn, clear for a stride and then black, the cold coming off it like a breath" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Moss()) },
        moods: new[] { "clear", "black", "breathless", "deep" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "plunge", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildWindGap() => new RockPointOfInterest(
        displayName: "Wind Gap",
        descriptions: new() { "The low point of the saddle, where the wind comes through hard enough to lean on" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Lichen()) },
        moods: new[] { "howling", "low", "cold", "funnelled" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "topographia", ["listen"] = "wind_reading" } };

    private static PointOfInterest BuildBothy() => new BothyPointOfInterest(
        displayName: "Stone Bothy",
        descriptions: new() { "A low hut of dry stone with a turf roof gone to grass, a hearthstone inside and the door no more than a gap" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Wool()) },
        moods: new[] { "low", "turf-roofed", "empty", "smoke-darkened" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "transhumance", ["contemplate"] = "hearthlonging" } };

    private static PointOfInterest BuildFoldRings() => new CairnPointOfInterest(
        displayName: "Fold Rings",
        descriptions: new() { "Rings of piled stone in the turf where the herds were penned at night, the grass inside them greener than outside" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "ringed", "green", "trodden", "old" },
        isNatural: false
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "transhumance" } };

    private static PointOfInterest BuildGentianPatch() => new HerbPointOfInterest(
        displayName: "Gentian Patch",
        descriptions: new() { "A scatter of gentians among the turf, blue so deep it looks wet" },
        items: new() { new ItemElement(new Gentian()), new ItemElement(new Gentian()) },
        moods: new[] { "blue", "low", "rare", "bright" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildEyrieNest() => new NestPointOfInterest(
        displayName: "Eagle's Eyrie",
        descriptions: new() { "A platform of sticks as wide as a cart, lined with wool and fur and littered with the bones of hares" },
        items: new() { new ItemElement(new Feather()), new ItemElement(new Bone()), new ItemElement(new Wool()) },
        moods: new[] { "vast", "bone-littered", "reeking", "airy" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "creature_lore", ["contemplate"] = "awe", ["smell"] = "carrion_sense" } };

    private static PointOfInterest BuildStoatLarder() => new HollowPointOfInterest(
        displayName: "Stoat's Larder",
        descriptions: new() { "A dry niche in the cleft packed with small bones, a vole's tail and a few eggshells — something's winter store" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Egg()) },
        moods: new[] { "packed", "dry", "secret", "musky" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["smell"] = "musk_reading" } };

    // ── Life ─────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new WolfArchetype(), _identity == Identity.Wooded ? 0.30 : 0.20);
        TrySpawnBeast(rng, scene, new BoarArchetype(), _identity == Identity.Wooded ? 0.30 : 0.12);

        TrySpawnCreature(rng, scene, new EagleArchetype(), 0.80);
        TrySpawnCreature(rng, scene, new MountainGoatArchetype(), _identity == Identity.Rugged ? 0.70 : 0.45);
        TrySpawnCreature(rng, scene, new MarmotArchetype(), _identity == Identity.Pastoral ? 0.65 : 0.35);
        TrySpawnCreature(rng, scene, new RavenArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new AdderArchetype(), 0.25);
        TrySpawnCreature(rng, scene, new LynxArchetype(), _identity == Identity.Wooded ? 0.25 : 0.10);
        TrySpawnCreatureIn<WaterfallArea>(rng, scene, new DipperArchetype(), 0.75);
        TrySpawnCreatureIn<StreamArea>(rng, scene, new DipperArchetype(), 0.40);
        TrySpawnCreatureIn<CragArea>(rng, scene, new ChoughArchetype(), 0.55);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Barren, 1, 3);
    }
}
