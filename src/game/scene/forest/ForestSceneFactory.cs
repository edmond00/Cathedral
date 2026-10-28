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
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Forest;

/// <summary>
/// Builds a wood, laid out as woods are: a mosaic.
///
/// <para><b>Patches that meet on every side.</b> Three or four sections are drawn — the forest edge
/// always, then deep wood, wet wood, a ravine, a coppice — and joined in a ring, so the wood closes
/// loops and a walker can come back by another way. Each patch is woven in its own rolled shape.</para>
///
/// <para><b>Two rolls.</b> The kind of tree (deciduous, coniferous, mixed) furnishes every patch; the
/// character of the wood (<i>wildwood</i>, <i>worked</i>, <i>wet</i>) weights which patches it has —
/// old growth and deadwood, coppice and a woodcutter, carr and beaver pond.</para>
///
/// <para><b>Ways that are not a walk.</b> A ravine climbed down into; a beaver pond swum; a hollow oak
/// squeezed into; a giant tree scaled to its crown, which sees the whole wood; brambles and fallen
/// trunks crossed.</para>
/// </summary>
public class ForestSceneFactory : WildSceneFactory
{
    public ForestSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum ForestType { Deciduous, Coniferous, Mixed }
    private enum Character { Wildwood, Worked, Wet }
    private enum Patch { Deep, WetWood, Ravine, Coppice }

    private ForestType _type;
    private Character _character;
    private bool _hasWoodcutter, _hasCharcoalBurner;
    private Area? _clearing, _woodshed, _hollowOak;
    private readonly HashSet<Area> _brambles = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Profile = LayoutProfile.Mosaic;
        _type = (ForestType)rng.Next(3);
        _character = (Character)rng.Next(3);
        double W(double wild, double worked, double wet)
            => _character switch { Character.Wildwood => wild, Character.Worked => worked, _ => wet };
        _hasWoodcutter     = rng.NextDouble() < W(0.15, 0.6, 0.15);
        _hasCharcoalBurner = rng.NextDouble() < W(0.05, 0.35, 0.05);

        // ── Which patches ────────────────────────────────────────────────────
        var pool = new List<(Patch Kind, double Weight)>
        {
            (Patch.Deep,    W(4.0, 1.2, 1.0)),
            (Patch.WetWood, W(1.0, 0.8, 4.0)),
            (Patch.Ravine,  W(1.5, 0.8, 1.2)),
            (Patch.Coppice, W(0.3, 4.0, 0.5)),
        };
        var patches = new List<Patch>();
        for (int want = Between(rng, 2, 3); patches.Count < want;)
        {
            double total = pool.Sum(p => p.Weight), roll = rng.NextDouble() * total;
            int i = 0;
            for (double acc = pool[0].Weight; acc < roll && i < pool.Count - 1; acc += pool[++i].Weight) { }
            patches.Add(pool[i].Kind);
            pool.RemoveAt(i);
        }

        // ── How big each patch is: five to seven areas in all, shared out ───
        int edgeSize = Between(rng, 2, 3);
        var sizes = patches.ToDictionary(p => p, p => p == Patch.WetWood ? 2 : 1);
        for (int left = Between(rng, 5, 7) - edgeSize - sizes.Values.Sum(); left > 0; left--)
        {
            var open = patches.Where(p => sizes[p] < (p is Patch.Ravine or Patch.Coppice ? 2 : 3)).ToList();
            if (open.Count == 0) break;
            sizes[open[rng.Next(open.Count)]]++;
        }

        // ── The edge, always ─────────────────────────────────────────────────
        _clearing = BuildClearing();
        var edge = new List<Area> { _clearing };
        edge.AddRange(Draw(rng, new Option[]
        {
            new(BuildSlope, 1.0),
            new(BuildHolloway, W(1.2, 1.5, 0.6)),
            new(BuildBramble, 0.8),
        }, edgeSize - 1));

        var sections = new List<(string Name, List<Area> Areas, Patch? Kind)> { ("Forest Edge", edge, null) };
        foreach (var patch in patches)
            sections.Add(patch switch
            {
                Patch.Deep => ("Deep Wood", Draw(rng, new Option[]
                {
                    new(BuildOldGrowth, W(3.0, 0.6, 1.0)),
                    new(BuildThicket, 1.0),
                    new(BuildDeadwood, W(1.5, 0.6, 1.0)),
                }, sizes[patch]), patch),
                Patch.WetWood => ("Wet Wood", Draw(rng, new Option[]
                {
                    new(BuildCarr, 2.0),
                    new(BuildBeaverPond, W(1.5, 0.8, 2.5)),
                    new(BuildStreamside, 1.5),
                    new(BuildWellspring, 1.0),
                }, sizes[patch]), patch),
                Patch.Ravine => ("Ravine", Draw(rng, new Option[]
                {
                    new(BuildRavine, 3.0),
                    new(BuildDrippingSpring, 1.0),
                    new(BuildGill, 1.0),
                }, sizes[patch]), patch),
                _ => ("Coppice", Draw(rng, new Option[]
                {
                    new(BuildCoppice, 3.0),
                    new(BuildRide, 0.6),
                    new(BuildSawpit, 0.6),
                }, sizes[patch]), patch),
            });

        // The hollow oak hides in the oldest wood there is.
        var host = sections.Where(s => s.Kind == Patch.Deep).SelectMany(s => s.Areas).FirstOrDefault(a => a is GroveArea)
                   ?? edge[0];
        if (rng.NextDouble() < W(0.55, 0.2, 0.3)) _hollowOak = BuildHollowOak();

        foreach (var area in sections.SelectMany(s => s.Areas)) Populate(area, rng);
        if (_hollowOak != null) Populate(_hollowOak, rng);
        if (_hasWoodcutter || _hasCharcoalBurner)
            foreach (var poi in CampSubfactory.BuildForestCamp()) _clearing.PointsOfInterest.Add(poi);

        var registered = new List<Section>();
        foreach (var (name, areas, kind) in sections)
            registered.Add(AddSection(scene, name, SectionDescription(kind),
                seed => new NoisyGenerator { Seed = seed, Density = kind == Patch.Deep ? 0.55f : 0.7f }, areas));

        // ── Ways ─────────────────────────────────────────────────────────────
        string[] moods = { "narrow", "leaf-strewn", "winding" };
        static string Way(Area a, Area b) =>
            a is StreamArea || b is StreamArea || a is CarrArea || b is CarrArea ? "Stream Path"
            : a is ThicketArea || b is ThicketArea ? "Narrow Path"
            : a is HollowayArea || b is HollowayArea ? "Sunken Lane"
            : "Forest Track";

        foreach (var (_, areas, kind) in sections)
        {
            if (kind == Patch.WetWood && areas.Count >= 2 && areas.OfType<PondArea>().Any() && rng.NextDouble() < 0.75)
            {
                // The pond lies between: one side of the wet wood is reached across the water.
                var pond = areas.OfType<PondArea>().First();
                var rest = areas.Where(a => a != pond).ToList();
                WeaveSection(rng, scene, rest, RollWeave(rng), Way, "through the wood", moods);
                if (rest.Count >= 2)
                    OutdoorLayout.Link(scene, rest[0], pond, Way(rest[0], pond), moods, "through the wood");
                Swim(scene, pond, rest[^1], WaterKind.Pond, "Beaver Pond",
                    "Still brown water backed up behind the dam, drowned trees standing grey in it, the far side a short swim",
                    new[] { "still", "brown", "drowned" }, lesson: "plunge");
            }
            else
                WeaveSection(rng, scene, areas, RollWeave(rng), Way, "through the wood", moods);
        }

        // The patches meet in a ring, so each borders two others and the wood closes a loop. A ravine
        // is gone down into by its wall on one side and walked out of on the other.
        bool ravineClimbed = false;
        for (int i = 0, joins = sections.Count == 2 ? 1 : sections.Count; i < joins; i++)
        {
            var (_, a, kindA) = sections[i];
            var (_, b, kindB) = sections[(i + 1) % sections.Count];
            var x = a[rng.Next(a.Count)];
            var y = b[rng.Next(b.Count)];
            var ravine = kindA == Patch.Ravine ? x : kindB == Patch.Ravine ? y : null;
            if (ravine != null && !ravineClimbed)
            {
                Climb(scene, ravine, ravine == x ? y : x, "Ravine Wall",
                    "A steep earth bank going down between roots into the ravine, the stream loud at the bottom",
                    new[] { "steep", "rooty", "crumbling" });
                ravineClimbed = true;
            }
            else
                OutdoorLayout.Link(scene, x, y, Way(x, y), moods, "through the wood");
        }

        if (_hollowOak != null)
        {
            AddToSection(scene, registered.First(s => s.Areas.Contains(host)), _hollowOak);
            SlipInto(scene, host, _hollowOak, SlipKind.Hollow, "Hollow Oak",
                "A vast old oak split open at the foot, the gap black inside and big enough for a body",
                new[] { "split", "vast", "dark", "ancient" }, lesson: "potholing");
        }

        if (_hasWoodcutter || _hasCharcoalBurner)
        {
            _woodshed = CampSubfactory.BuildWoodshed();
            AddToSection(scene, registered[0], _woodshed);
            OutdoorLayout.Link(scene, _clearing, _woodshed, "Track");
            scene.Stores.Add(_woodshed);
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Woodland, _hollowOak, _woodshed);

        // A giant tree, and from its crown a road to every other part of the wood. On the ground a
        // forest is the least legible place in the game, so height buys more here than anywhere.
        var outdoors = scene.OutdoorAreas;
        var crown = FurnitureSubfactory.AddGiantTree(rng, scene, outdoors[0]);
        if (crown != null)
        {
            AddToSection(scene, scene.Sections.First(s => s.Areas.Contains(outdoors[0])), crown);
            AddLandscapes(scene, crown, scene.AllAreas);
        }

        Console.WriteLine($"ForestSceneFactory: {_type}/{_character}, {string.Join("+", sections.Select(s => s.Areas.Count))} areas, woodcutter={_hasWoodcutter}, charcoal={_hasCharcoalBurner}");
    }

    private static string SectionDescription(Patch? kind) => kind switch
    {
        null          => "Lighter canopy, more undergrowth, sky visible between trunks",
        Patch.Deep    => "Dense canopy, darker, quieter; the great trees stand close together",
        Patch.WetWood => "Alder and willow standing in black water, the ground giving underfoot",
        Patch.Ravine  => "A steep cleft in the wood, ferned and dripping, a stream loud at the bottom",
        _             => "Woodland cut back to the stool and grown again in straight rods",
    };

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildClearing() => new ClearingArea(
        displayName: "Clearing",
        contextDescription: "in the forest clearing",
        transitionDescription: "step into the clearing",
        descriptions: new() { "An open patch among the trees, sunlight reaching the forest floor" },
        moods: new[] { "open", "sunlit", "quiet", "wide" });

    private static Area BuildThicket() => new ThicketArea(
        displayName: "Tangle",
        contextDescription: "pushing through the thicket",
        transitionDescription: "force into the thicket",
        descriptions: new() { "A dense tangle of saplings, brambles, and low growth" },
        moods: new[] { "dense", "snarled", "shadowed", "low" });

    private Area BuildBramble() => Note(_brambles, new ThicketArea(
        displayName: "Bramble Brake",
        contextDescription: "in the bramble brake",
        transitionDescription: "push into the bramble brake",
        descriptions: new() { "A brake of bramble and bracken at the wood's edge, blackberries in it and thorns everywhere" },
        moods: new[] { "thorned", "tangled", "berried", "sunny" }));

    private static Area Note(HashSet<Area> set, Area area) { set.Add(area); return area; }

    private static Area BuildOldGrowth() => new GroveArea(
        displayName: "Old Growth",
        contextDescription: "among the great trees of the old growth",
        transitionDescription: "step into the old growth",
        descriptions: new() { "Massive ancient trees stand close together, undergrowth sparse beneath" },
        moods: new[] { "ancient", "still", "vaulted", "dark", "spacious" });

    private static Area BuildStreamside() => new StreamArea(
        displayName: "Streamside",
        contextDescription: "beside a forest stream",
        transitionDescription: "follow the stream's edge",
        descriptions: new() { "A creek winds through the wood, mud-banked and clear-running" },
        moods: new[] { "wet", "cool", "running", "muddy" });

    private static Area BuildDeadwood() => new DeadwoodArea(
        displayName: "Deadwood Patch",
        contextDescription: "in the deadwood patch",
        transitionDescription: "step into the deadwood patch",
        descriptions: new() { "Fallen and rotting trees lie heaped where storms felled them" },
        moods: new[] { "rotting", "fungal", "still", "tangled" });

    private static Area BuildSlope() => new SlopeArea(
        displayName: "Wooded Slope",
        contextDescription: "on the wooded slope",
        transitionDescription: "climb the wooded slope",
        descriptions: new() { "The forest climbs a hillside, roots breaking through the loose soil" },
        moods: new[] { "tilted", "uneven", "rooted", "shaded" });

    private static Area BuildHolloway() => new HollowayArea(
        displayName: "Holloway",
        contextDescription: "down in the holloway",
        transitionDescription: "step down into the holloway",
        descriptions: new() { "A lane worn down by centuries of feet until its banks stand head-high, roots hanging out of them and the trees meeting overhead" },
        moods: new[] { "sunken", "green-roofed", "rooty", "old" });

    private static Area BuildDrippingSpring() => new WellspringArea(
        displayName: "Dripping Spring",
        contextDescription: "at the dripping spring",
        transitionDescription: "climb down to the dripping spring",
        descriptions: new() { "Water oozing out of the ravine wall over cushions of moss and dripping into a stone basin it has worn for itself" },
        moods: new[] { "oozing", "mossy", "dripping", "hushed" });

    private static Area BuildGill() => new StreamArea(
        displayName: "Gill",
        contextDescription: "down by the gill",
        transitionDescription: "slither down to the gill",
        descriptions: new() { "A white rush of water down the ravine floor in steps and pools, too loud to talk over" },
        moods: new[] { "rushing", "stepped", "loud", "cold" });

    private static Area BuildRide() => new SlopeArea(
        displayName: "Woodland Ride",
        contextDescription: "in the woodland ride",
        transitionDescription: "walk down the woodland ride",
        descriptions: new() { "A broad grassy way cut through the coppice for the carts, sunny and full of butterflies" },
        moods: new[] { "grassy", "sunny", "rutted", "broad" });

    private static Area BuildSawpit() => new ClearingArea(
        displayName: "Sawpit",
        contextDescription: "at the sawpit",
        transitionDescription: "come out at the sawpit",
        descriptions: new() { "A trodden space in the coppice round a pit for sawing logs, stacks of cord-wood and bundles of rods beside it" },
        moods: new[] { "trodden", "stacked", "sawdusty", "worked" });

    private static Area BuildCarr() => new CarrArea(
        displayName: "Alder Carr",
        contextDescription: "in the alder carr",
        transitionDescription: "squelch into the alder carr",
        descriptions: new() { "Alders and willows standing on their own roots in black water, tussocks of sedge between, everything dripping" },
        moods: new[] { "black-watered", "dripping", "tangled", "humming" });

    private static Area BuildBeaverPond() => new PondArea(
        displayName: "Beaver Pond",
        contextDescription: "by the beaver pond",
        transitionDescription: "come out of the trees at the beaver pond",
        descriptions: new() { "A pond backed up behind a dam of gnawed sticks, drowned trees standing grey in it" },
        moods: new[] { "still", "drowned", "gnawed", "brown" });

    private static Area BuildWellspring() => new WellspringArea(
        displayName: "Wellspring",
        contextDescription: "at the wellspring",
        transitionDescription: "follow the wet ground up to the wellspring",
        descriptions: new() { "A place where water wells up clear out of the ground among mossy stones and runs off as a stream" },
        moods: new[] { "welling", "clear", "mossy", "hushed" });

    private static Area BuildRavine() => new RavineArea(
        displayName: "Ravine",
        contextDescription: "at the bottom of the ravine",
        transitionDescription: "slither down into the ravine",
        descriptions: new() { "A deep wooded cleft with a stream at the bottom, its sides hung with ferns, the light green and dim" },
        moods: new[] { "deep", "ferny", "dim", "loud" });

    private static Area BuildCoppice() => new CoppiceArea(
        displayName: "Hazel Coppice",
        contextDescription: "in the hazel coppice",
        transitionDescription: "step into the coppice",
        descriptions: new() { "Hazel cut back to the stool in turn, a few years' growth here and straight new rods there, the cut stools like green fountains" },
        moods: new[] { "managed", "straight-rodded", "light", "cut" });

    private static Area BuildHollowOak() => new AlcoveArea(
        displayName: "Inside the Hollow Oak",
        contextDescription: "inside the hollow oak",
        transitionDescription: "squeeze inside the hollow oak",
        descriptions: new() { "The inside of a living oak, hollowed by age: a dry brown room of soft rotten wood, a chimney of daylight far above" },
        moods: new[] { "dry", "brown", "close", "secret" });

    // ── Contents ─────────────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        if (area == _hollowOak)
        {
            p.Add(BuildSquirrelHoard());
            p.Add(BuildOwlPellets());
            return;
        }

        bool wooded = area is not PondArea and not WellspringArea and not CoppiceArea;
        if (wooded)
            for (int i = 0, n = rng.Next(1, 4); i < n; i++) p.Add(PickTree(rng));

        switch (area)
        {
            case DeadwoodArea:
            case GroveArea:
                p.Add(TerrainSubfactory.BuildFelledLog());
                p.Add(TerrainSubfactory.BuildTreeStump());
                p.Add(TerrainSubfactory.BuildDeadfall());
                break;
            case StreamArea:
                p.Add(TerrainSubfactory.BuildStreamBank());
                break;
            case CarrArea:
                p.Add(BuildAlderStools());
                p.Add(TerrainSubfactory.BuildReedBed());
                break;
            case PondArea:
                p.Add(BuildBeaverLodge());
                p.Add(BuildBeaverDam());
                break;
            case WellspringArea:
                p.Add(BuildSpringHead());
                p.Add(TerrainSubfactory.BuildMossBank());
                break;
            case RavineArea:
                p.Add(TerrainSubfactory.BuildRockFace());
                p.Add(BuildHartsTongue());
                break;
            case CoppiceArea:
                p.Add(BuildHazelStools());
                p.Add(TerrainSubfactory.BuildTreeStump());
                break;
            case HollowayArea:
                p.Add(BuildBankRoots());
                break;
            case ThicketArea when _brambles.Contains(area):
                p.Add(TerrainSubfactory.BuildBerryBush());
                break;
            default:
                if (rng.NextDouble() < 0.4) p.Add(TerrainSubfactory.BuildFelledLog());
                break;
        }

        if (wooded)
        {
            if (rng.NextDouble() < 0.6) p.Add(TerrainSubfactory.BuildUndergrowthPatch());
            if (rng.NextDouble() < 0.4) p.Add(TerrainSubfactory.BuildMushroomCluster());
            if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildMossBank());
        }
    }

    private PointOfInterest PickTree(Random rng) => _type switch
    {
        ForestType.Coniferous => TerrainSubfactory.BuildPineTree(),
        ForestType.Mixed => rng.Next(4) switch
        {
            0 => TerrainSubfactory.BuildOakTree(),
            1 => TerrainSubfactory.BuildBeechTree(),
            2 => TerrainSubfactory.BuildPineTree(),
            _ => TerrainSubfactory.BuildBirchTree(),
        },
        _ => rng.Next(4) switch
        {
            0 => TerrainSubfactory.BuildOakTree(),
            1 => TerrainSubfactory.BuildBeechTree(),
            2 => TerrainSubfactory.BuildAshTree(),
            _ => TerrainSubfactory.BuildBirchTree(),
        },
    };

    private static PointOfInterest BuildAlderStools() => new WillowPointOfInterest(
        displayName: "Alder Stools",
        descriptions: new() { "Alders growing up out of black water from root-stools like islands, catkins hanging, the bark stained orange where it is cut" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Bark()) },
        moods: new[] { "islanded", "dripping", "orange-cut", "tangled" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "dowsing", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildBeaverLodge() => new MoundPointOfInterest(
        displayName: "Beaver Lodge",
        descriptions: new() { "A domed heap of gnawed sticks and mud standing out of the pond, a thin breath of warm air rising from its top" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Twig()) },
        moods: new[] { "domed", "gnawed", "muddy", "breathing" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["listen"] = "beast_call" } };

    private static PointOfInterest BuildBeaverDam() => new DeadfallPointOfInterest(
        displayName: "Beaver Dam",
        descriptions: new() { "A dam of sticks, stones and packed mud across the stream, water spilling thinly over its whole length" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Twig()), new ItemElement(new Clay()) },
        moods: new[] { "packed", "spilling", "gnawed", "patient" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildSpringHead() => new SpringPointOfInterest(
        displayName: "Spring Head",
        descriptions: new() { "Clear water rising in a sandy basin, the sand dancing where it wells up, watercress thick in the overflow" },
        items: new() { new ItemElement(new Watercress()), new ItemElement(new Watercress()) },
        moods: new[] { "welling", "dancing", "clear", "cold" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "dowsing", ["listen"] = "water_voice", ["smell"] = "petrichor", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildHartsTongue() => new HerbPointOfInterest(
        displayName: "Hart's-Tongue Ferns",
        descriptions: new() { "Glossy strap-leaved ferns hanging in curtains from the dripping sides of the ravine" },
        items: new() { new ItemElement(new Fern()), new ItemElement(new Moss()) },
        moods: new[] { "glossy", "hanging", "dripping", "green" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildHazelStools() => new TreePointOfInterest(
        displayName: "Hazel Stools",
        descriptions: new() { "Old hazel stools sprouting a fountain of straight rods each, the nuts hanging in clusters among the leaves" },
        items: new() { new ItemElement(new Hazelnut()), new ItemElement(new Hazelnut()), new ItemElement(new Branch()) },
        moods: new[] { "sprouting", "laden", "straight", "managed" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "coppicing", ["contemplate"] = "harvest_joy" } };

    private static PointOfInterest BuildBankRoots() => new UndergrowthPointOfInterest(
        displayName: "Root-Hung Banks",
        descriptions: new() { "The holloway's walls, head-high earth hung with roots and ferns, old marks cut into the clay here and there" },
        items: new() { new ItemElement(new Twig()), new ItemElement(new Fern()) },
        moods: new[] { "head-high", "rooted", "marked", "shaded" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "trailcraft", ["contemplate"] = "superstition" } };

    private static PointOfInterest BuildSquirrelHoard() => new HollowPointOfInterest(
        displayName: "Squirrel Hoard",
        descriptions: new() { "A cranny in the rotten wood packed with hazelnuts and acorns, some of them gnawed open" },
        items: new() { new ItemElement(new Hazelnut()), new ItemElement(new Acorn()), new ItemElement(new Hazelnut()) },
        moods: new[] { "packed", "gnawed", "secret", "dry" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "nutting" } };

    private static PointOfInterest BuildOwlPellets() => new BonesPointOfInterest(
        displayName: "Owl Pellets",
        descriptions: new() { "Grey felted pellets on the floor of the hollow, tiny skulls and bones showing through them" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Skull()) },
        moods: new[] { "grey", "felted", "tiny-boned", "dry" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore" } };

    // ── Life ─────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_clearing is null || _allAreas.Count == 0) return;

        if (_hasWoodcutter)
            TradeAt(SpawnWorker(rng, scene, new WoodcutterArchetype(), WorkerSchedule(TimePeriod.Evening)), _woodshed!);
        if (_hasCharcoalBurner)
            TradeAt(SpawnWorker(rng, scene, new CharcoalBurnerArchetype(), WorkerSchedule(TimePeriod.Noon)), _woodshed!);

        TrySpawnBeast(rng, scene, new BoarArchetype(), _character == Character.Wildwood ? 0.5 : 0.35);
        TrySpawnBeast(rng, scene, new WolfArchetype(), _character == Character.Wildwood ? 0.3 : 0.15);

        TrySpawnCreature(rng, scene, new DeerArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new BadgerArchetype(), 0.30);
        TrySpawnCreature(rng, scene, new SquirrelArchetype(), 0.60);
        TrySpawnCreature(rng, scene, new WoodMouseArchetype(), 0.45);
        TrySpawnCreature(rng, scene, new RobinArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new WoodpeckerArchetype(), 0.30);
        TrySpawnCreature(rng, scene, new OwlArchetype(), 0.30);
        TrySpawnCreatureIn<PondArea>(rng, scene, new BeaverArchetype(), 0.80);
        TrySpawnCreatureIn<StreamArea>(rng, scene, new KingfisherArchetype(), 0.45);
        TrySpawnCreatureIn<PondArea>(rng, scene, new KingfisherArchetype(), 0.35);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Woodland, 3, 6);
    }

    private NpcSchedule WorkerSchedule(TimePeriod atShed)
    {
        var clearing = _clearing!;
        var work  = _allAreas.FirstOrDefault(a => a is GroveArea or ThicketArea or CoppiceArea) ?? clearing;
        var haul  = _allAreas.FirstOrDefault(a => a is DeadwoodArea or CoppiceArea) ?? clearing;
        return NpcSchedule.Roaming(new()
        {
            [TimePeriod.Dawn]      = clearing,
            [TimePeriod.Morning]   = work,
            [TimePeriod.Noon]      = clearing,
            [TimePeriod.Afternoon] = haul,
            [TimePeriod.Evening]   = clearing,
            [TimePeriod.Night]     = clearing,
            [atShed]               = _woodshed!,
        });
    }

    private NpcEntity SpawnWorker(Random rng, Scene scene, NamedNpcArchetype archetype, NpcSchedule schedule)
    {
        var entity = archetype.Spawn(rng, _clearing!.ContextDescription,
            _locationState != null ? _locationState.AffinityFor : null);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = schedule;
        return entity;
    }
}
