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

namespace Cathedral.Game.Scene.Plain;

/// <summary>
/// Builds open country, laid out as a mosaic of its kinds of ground.
///
/// <para><b>Patches that meet on several sides.</b> The open country is always there — grassland,
/// meadow, hedgerow, gorse common, heath. Two or three more are drawn: the downs (hill, barrow, henge,
/// valley), the wetlands (bog and fen), the river land (water meadow, a ford, an oxbow). They are
/// joined in a ring, so the country closes loops.</para>
///
/// <para><b>Four plains.</b> The identity roll weights the draw: an <i>open</i> plain is grass and
/// hedge, a <i>heath</i> runs to gorse and the old ground of the downs, a <i>wetland</i> to bog and
/// fen, a <i>river</i> plain to its water meadows and ford.</para>
///
/// <para><b>Ways that are not a walk.</b> A tor on the hill to climb, which sees the country; a barrow
/// to squeeze into; the ford, waded across; hedges and mud crossed.</para>
/// </summary>
public class PlainSceneFactory : WildSceneFactory
{
    public PlainSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Open, Heath, Wetland, River }
    private enum Patch { Downs, Wet, Riverland }

    private Identity _identity;
    private Area? _tor, _chamber;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Profile = LayoutProfile.Mosaic;
        _identity = (Identity)rng.Next(4);
        double W(double open, double heath, double wet, double river) => _identity switch
        {
            Identity.Open => open, Identity.Heath => heath, Identity.Wetland => wet, _ => river,
        };

        var pool = new List<(Patch Kind, double Weight)>
        {
            (Patch.Downs,     W(2.0, 3.0, 0.6, 1.0)),
            (Patch.Wet,       W(0.8, 0.6, 4.0, 1.2)),
            (Patch.Riverland, W(1.2, 0.5, 1.5, 4.0)),
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

        int openSize = Between(rng, 2, 3);
        var sizes = patches.ToDictionary(p => p, p => p == Patch.Riverland ? 2 : 1);
        for (int left = Between(rng, 6, 8) - openSize - sizes.Values.Sum(); left > 0; left--)
        {
            var room = patches.Where(p => sizes[p] < (p == Patch.Wet ? 2 : 3)).ToList();
            if (room.Count == 0) break;
            sizes[room[rng.Next(room.Count)]]++;
        }

        var open = Draw(rng, new Option[]
        {
            new(BuildGrassland, W(2.0, 1.0, 1.0, 1.5)),
            new(BuildMeadow,    1.5),
            new(BuildHedgerow,  W(2.0, 0.8, 1.0, 1.5)),
            new(BuildCommon,    W(0.8, 2.5, 0.5, 0.6)),
            new(BuildHeath,     W(0.4, 3.0, 0.2, 0.2)),
        }, openSize);

        var sections = new List<(string Name, string Description, List<Area> Areas)>
        {
            ("Flatlands", "Open, low-lying ground; wide sky and long sightlines", open),
        };
        foreach (var patch in patches)
            sections.Add(patch switch
            {
                Patch.Downs => ("The Downs", "Rolling high ground of short turf, the old people's mounds and stones on the skyline",
                    Draw(rng, new Option[]
                    {
                        new(BuildHill, 2.0),
                        new(BuildBarrow, W(1.0, 2.0, 0.6, 0.8)),
                        new(BuildHenge, W(0.8, 1.5, 0.5, 0.6)),
                        new(BuildValley, 1.0),
                    }, sizes[patch])),
                Patch.Wet => ("Wetlands", "Low wet ground of reed and sedge, more water than land after rain",
                    Draw(rng, new Option[]
                    {
                        new(BuildBoggyGround, 1.5),
                        new(BuildFen, 2.0),
                    }, sizes[patch])),
                _ => ("River Land", "The flat green ground along a slow river, flooded every winter",
                    Draw(rng, new Option[]
                    {
                        new(BuildWaterMeadow, 2.0),
                        new(BuildFord, 2.0),
                        new(BuildOxbow, 1.2),
                    }, sizes[patch])),
            });

        // Hidden places: a tor on the hill, a chamber in the barrow.
        var hill = sections.SelectMany(s => s.Areas).OfType<HillArea>().FirstOrDefault();
        if (hill != null && rng.NextDouble() < 0.55) _tor = BuildTor();
        var barrow = sections.SelectMany(s => s.Areas).OfType<BarrowArea>().FirstOrDefault();
        if (barrow != null && rng.NextDouble() < 0.55) _chamber = BuildBarrowChamber();

        foreach (var area in sections.SelectMany(s => s.Areas)) Populate(area, rng);
        if (_tor != null) Populate(_tor, rng);
        if (_chamber != null) Populate(_chamber, rng);

        var registered = sections.Select(s => AddSection(scene, s.Name, s.Description,
            seed => new NoisyGenerator { Seed = seed, Density = 0.9f }, s.Areas)).ToList();

        // ── Ways ─────────────────────────────────────────────────────────────
        string[] moods = { "worn", "narrow", "winding" };
        foreach (var (_, _, areas) in sections)
        {
            var ford = areas.OfType<FordArea>().FirstOrDefault();
            if (ford != null && areas.Count >= 2)
            {
                // The river runs between the ford and the far meadow: it is waded, not walked.
                var rest = areas.Where(a => a != ford).ToList();
                WeaveSection(rng, scene, rest, RollWeave(rng), WayKind, "across the open country", moods);
                Swim(scene, ford, rest[0], WaterKind.Creek, "The Ford",
                    "The river running wide and shallow over a bed of stones, knee-deep at the crossing, faster than it looks",
                    new[] { "shallow", "stony", "quick" });
            }
            else
                WeaveSection(rng, scene, areas, RollWeave(rng), WayKind, "across the open country", moods);
        }

        for (int i = 0, joins = sections.Count == 2 ? 1 : sections.Count; i < joins; i++)
        {
            var a = sections[i].Areas;
            var b = sections[(i + 1) % sections.Count].Areas;
            var x = a[rng.Next(a.Count)];
            var y = b[rng.Next(b.Count)];
            OutdoorLayout.Link(scene, x, y, WayKind(x, y), moods, "across the open country");
        }

        if (_tor != null)
        {
            AddToSection(scene, registered.First(s => s.Areas.Contains(hill!)), _tor);
            AddViewpoint(scene, hill!, _tor, "Tor",
                "A heap of weathered granite blocks on the hilltop, piled like loaves, with cracks enough to climb by",
                new[] { "weathered", "piled", "lichened" });
        }
        if (_chamber != null)
        {
            AddToSection(scene, registered.First(s => s.Areas.Contains(barrow!)), _chamber);
            SlipInto(scene, barrow!, _chamber, SlipKind.Crevice, "Fallen Kerbstone",
                "At the barrow's end a great stone has tipped, leaving a black gap under the turf that cold air comes out of",
                new[] { "tipped", "black", "cold-breathing" }, lesson: "grave_robbing");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Farmland, _tor, _chamber);

        Console.WriteLine($"PlainSceneFactory: {_identity}, {string.Join("+", sections.Select(s => s.Areas.Count))} areas");
    }

    private static string WayKind(Area a, Area b) =>
        a is HeathArea || b is HeathArea || a is CommonArea || b is CommonArea ? "Heath Track"
        : a is BogArea || b is BogArea || a is FenArea || b is FenArea ? "Bog Track"
        : a is HedgerowArea || b is HedgerowArea ? "Hedge Gap"
        : a is HillArea || b is HillArea || a is BarrowArea || b is BarrowArea ? "Ridgeway"
        : "Open Path";

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildGrassland() => new GrasslandArea(
        displayName: "Grassland",
        contextDescription: "crossing the open grassland",
        transitionDescription: "move into the grassland",
        descriptions: new() { "Open wind-swept grassland, the sky vast and unbroken above" },
        moods: new[] { "vast", "windy", "yellowed", "rustling", "open" });

    private static Area BuildMeadow() => new MeadowArea(
        displayName: "Meadow",
        contextDescription: "wandering through the meadow",
        transitionDescription: "move into the meadow",
        descriptions: new() { "A sheltered meadow speckled with wildflowers and the hum of bees" },
        moods: new[] { "sunlit", "fragrant", "soft", "quiet", "windless" });

    private static Area BuildHeath() => new HeathArea(
        displayName: "Heath",
        contextDescription: "crossing the dry heath",
        transitionDescription: "move into the heath",
        descriptions: new() { "A dry stretch of scrub and heather, broken by stands of gorse" },
        moods: new[] { "dry", "scrubby", "purple", "wind-bent", "low" });

    private static Area BuildCommon() => new CommonArea(
        displayName: "Gorse Common",
        contextDescription: "on the gorse common",
        transitionDescription: "step onto the common",
        descriptions: new() { "Rough open ground of gorse and bracken, grazed in patches, the gorse yellow and smelling of coconut in the sun" },
        moods: new[] { "rough", "yellow-flowered", "prickly", "warm" });

    private static Area BuildHill() => new HillArea(
        displayName: "Hill",
        contextDescription: "on the open hill",
        transitionDescription: "move up onto the hill",
        descriptions: new() { "A gentle rise of grass and stone, exposed to the wind" },
        moods: new[] { "windswept", "exposed", "rolling", "bare", "open" });

    private static Area BuildValley() => new ValleyArea(
        displayName: "Valley",
        contextDescription: "down in the valley",
        transitionDescription: "descend into the valley",
        descriptions: new() { "A shallow damp depression sheltered from the wind" },
        moods: new[] { "sheltered", "damp", "lush", "shadowed", "still" });

    private static Area BuildHedgerow() => new HedgerowArea(
        displayName: "Hedgerow",
        contextDescription: "walking along the hedgerow",
        transitionDescription: "move along the hedgerow",
        descriptions: new() { "A long thorny hedgerow standing between two open stretches" },
        moods: new[] { "tangled", "thorny", "dense", "narrow", "shaded" });

    private static Area BuildBoggyGround() => new BogArea(
        displayName: "Boggy Ground",
        contextDescription: "wading through boggy ground",
        transitionDescription: "step into the boggy ground",
        descriptions: new() { "Soft wet ground beneath a tangle of reeds and rushes" },
        moods: new[] { "wet", "soft", "still", "rustling", "treacherous" });

    private static Area BuildFen() => new FenArea(
        displayName: "Fen",
        contextDescription: "out on the fen",
        transitionDescription: "squelch out onto the fen",
        descriptions: new() { "Flat wet country of sedge and reed to the horizon, cut by black dykes, a great sky over it" },
        moods: new[] { "flat", "sedgy", "black-dyked", "huge-skied" });

    private static Area BuildWaterMeadow() => new WaterMeadowArea(
        displayName: "Water Meadow",
        contextDescription: "in the water meadow",
        transitionDescription: "step into the water meadow",
        descriptions: new() { "Lush meadow beside the river, cut with little channels for flooding it, the grass rich and the ground soft" },
        moods: new[] { "lush", "channelled", "soft", "green" });

    private static Area BuildFord() => new FordArea(
        displayName: "Ford",
        contextDescription: "at the ford",
        transitionDescription: "go down to the ford",
        descriptions: new() { "A place where the river spreads wide and shallow over stones, the bank trodden down by every crossing" },
        moods: new[] { "trodden", "shallow", "chattering", "open" });

    private static Area BuildOxbow() => new OxbowArea(
        displayName: "Oxbow",
        contextDescription: "by the oxbow",
        transitionDescription: "come to the oxbow",
        descriptions: new() { "A loop of old river cut off from the stream, still and reedy, a heron standing in the shallows" },
        moods: new[] { "still", "reedy", "curved", "quiet" });

    private static Area BuildBarrow() => new BarrowArea(
        displayName: "Long Barrow",
        contextDescription: "by the long barrow",
        transitionDescription: "walk up to the long barrow",
        descriptions: new() { "A long grassy mound on the skyline, too regular to be natural, great stones showing at its end" },
        moods: new[] { "long", "grassy", "watchful", "old" });

    private static Area BuildHenge() => new HengeArea(
        displayName: "Stone Ring",
        contextDescription: "inside the stone ring",
        transitionDescription: "step into the stone ring",
        descriptions: new() { "A ring of grey standing stones on open ground, some leaning, some fallen, the grass inside it very green" },
        moods: new[] { "ringed", "leaning", "hushed", "watchful" });

    private static Area BuildTor() => new CragArea(
        displayName: "Tor",
        contextDescription: "on top of the tor",
        transitionDescription: "climb onto the tor",
        descriptions: new() { "The flat top of a heap of granite on the hilltop, the whole country spread out round it" },
        moods: new[] { "high", "granite", "windy", "commanding" });

    private static Area BuildBarrowChamber() => new ChamberArea(
        displayName: "Barrow Chamber",
        contextDescription: "inside the barrow",
        transitionDescription: "crawl into the barrow",
        descriptions: new() { "A low passage of great stones under the mound, side-chambers opening off it, the air dead and cold" },
        moods: new[] { "low", "dead-aired", "cold", "forbidding" });

    // ── Contents ─────────────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        switch (area)
        {
            case BarrowArea:
                p.Add(BuildMound());
                break;
            case ChamberArea:
                p.Add(BuildCist());
                return;
            case HengeArea:
                p.Add(BuildStandingStones());
                break;
            case CragArea:
                p.Add(TerrainSubfactory.BuildLichenCrust());
                return;
            case FenArea:
                p.Add(BuildSedgeBeds());
                p.Add(BuildBogMyrtle());
                return;
            case CommonArea:
                p.Add(BuildGorse());
                break;
            case WaterMeadowArea:
                p.Add(BuildFloatedChannels());
                p.Add(BuildMeadowsweet());
                break;
            case FordArea:
                p.Add(BuildFordStones());
                p.Add(TerrainSubfactory.BuildStreamBank());
                break;
            case OxbowArea:
                p.Add(BuildStillWater());
                p.Add(TerrainSubfactory.BuildReedBed());
                return;
            case BogArea:
                p.Add(TerrainSubfactory.BuildReedBed());
                break;
        }

        for (int i = 0, n = rng.Next(1, 3); i < n; i++) p.Add(PickTree(rng));
        if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildBoulder());
        if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildFlowerPatch());
        if (rng.NextDouble() < 0.4) p.Add(PickBerryBush(rng));
        if (rng.NextDouble() < 0.3) p.Add(TerrainSubfactory.BuildMushroomCluster());
    }

    private PointOfInterest PickTree(Random rng) => _identity switch
    {
        Identity.Heath   => rng.NextDouble() < 0.5 ? TerrainSubfactory.BuildElderTree()  : TerrainSubfactory.BuildHawthornTree(),
        Identity.Wetland => rng.NextDouble() < 0.6 ? TerrainSubfactory.BuildWillowTree() : TerrainSubfactory.BuildElderTree(),
        Identity.River   => rng.NextDouble() < 0.5 ? TerrainSubfactory.BuildWillowTree() : TerrainSubfactory.BuildOakTree(),
        _                => rng.NextDouble() < 0.5 ? TerrainSubfactory.BuildOakTree()    : TerrainSubfactory.BuildHawthornTree(),
    };

    private static PointOfInterest PickBerryBush(Random rng) => rng.Next(3) switch
    {
        0 => TerrainSubfactory.BuildBerryBush(),
        1 => TerrainSubfactory.BuildBilberryBush(),
        _ => TerrainSubfactory.BuildSloeBush(),
    };

    private static PointOfInterest BuildMound() => new MoundPointOfInterest(
        displayName: "Barrow Mound",
        descriptions: new() { "The long mound itself, its turf scarred where rabbits and diggers have been at it, flints and old sherds turned up in the spoil" },
        items: new() { new ItemElement(new ElfShot()), new ItemElement(new Potsherd()), new ItemElement(new Flint()) },
        moods: new[] { "scarred", "turfed", "spoil-strewn", "brooding" },
        isNatural: false
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "superstition" } };

    private static PointOfInterest BuildCist() => new GravePointOfInterest(
        displayName: "Burial Cist",
        descriptions: new() { "A box of stone slabs in a side-chamber, its lid shifted, bones inside and a few grave-goods among them" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Skull()), new ItemElement(new ElfShot()), new ItemElement(new Potsherd()) },
        moods: new[] { "slabbed", "shifted", "dust-dry", "forbidding" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "gravesight", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildStandingStones() => new MonolithPointOfInterest(
        displayName: "Standing Stones",
        descriptions: new() { "Grey stones taller than a man, rough and lichened, set in a ring by somebody who knew where the sun would rise" },
        items: new() { new ItemElement(new Lichen()) },
        moods: new[] { "grey", "towering", "lichened", "aligned" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "astronomy", ["contemplate"] = "superstition", ["listen"] = "wind_reading" } };

    private static PointOfInterest BuildSedgeBeds() => new ReedPointOfInterest(
        displayName: "Sedge Beds",
        descriptions: new() { "Sedge and reed standing thick between black dykes, cut in strips for thatch, the ground quaking underfoot" },
        items: new() { new ItemElement(new Reed()), new ItemElement(new Reed()) },
        moods: new[] { "thick", "quaking", "strip-cut", "rustling" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fencraft", ["listen"] = "insect_chorus" } };

    private static PointOfInterest BuildBogMyrtle() => new HerbPointOfInterest(
        displayName: "Bog Myrtle",
        descriptions: new() { "Low bushes of bog myrtle at the fen edge, the leaves resinous and sharp-smelling when brushed" },
        items: new() { new ItemElement(new BogMyrtle()), new ItemElement(new BogMyrtle()) },
        moods: new[] { "low", "resinous", "sharp-smelling", "grey-green" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fencraft", ["smell"] = "herblore", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildGorse() => new BushPointOfInterest(
        displayName: "Gorse Thicket",
        descriptions: new() { "Gorse in solid yellow banks, spined all over, smelling of coconut in the sun and crackling in the heat" },
        items: new() { new ItemElement(new Thorn()), new ItemElement(new Twig()) },
        moods: new[] { "yellow", "spined", "crackling", "warm" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "hedgecraft", ["smell"] = "petrichor", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildFloatedChannels() => new DitchPointOfInterest(
        displayName: "Floating Channels",
        descriptions: new() { "Little channels cut across the meadow in a herringbone, with turf hatches to let the river in over the grass in winter" },
        items: new() { new ItemElement(new Clay()) },
        moods: new[] { "herringboned", "neat", "wet", "old" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "sluicecraft" } };

    private static PointOfInterest BuildMeadowsweet() => new HerbPointOfInterest(
        displayName: "Meadowsweet",
        descriptions: new() { "Frothy cream meadowsweet standing waist-high along the wet edges, its smell heavy in the warm air" },
        items: new() { new ItemElement(new Meadowsweet()), new ItemElement(new Meadowsweet()) },
        moods: new[] { "frothy", "cream", "heavy-scented", "waist-high" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "perfumery", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildFordStones() => new RockPointOfInterest(
        displayName: "Ford Stones",
        descriptions: new() { "Big flat stones laid in the shallows by somebody long ago, a slippery line of them across the river" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "flat", "slippery", "laid", "worn" },
        isNatural: false
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fording", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildStillWater() => new PoolPointOfInterest(
        displayName: "Still Water",
        descriptions: new() { "The oxbow's water, dark and unmoving, lily pads at the edge and the rings of rising fish" },
        items: new() { new ItemElement(new Reed()) },
        moods: new[] { "dark", "unmoving", "lily-padded", "ringed" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "anglery", ["contemplate"] = "meditation" } };

    // ── Life ─────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new BoarArchetype(), 0.25);
        TrySpawnBeast(rng, scene, new FoxArchetype(),  0.40);
        TrySpawnBeast(rng, scene, new WolfArchetype(), 0.10);

        TrySpawnCreature(rng, scene, new DeerArchetype(),    0.50);
        TrySpawnCreature(rng, scene, new HareArchetype(),    0.50);
        TrySpawnCreature(rng, scene, new CrowArchetype(),    0.60);
        TrySpawnCreature(rng, scene, new LarkArchetype(),    0.45);
        TrySpawnCreature(rng, scene, new SparrowArchetype(), 0.45);
        TrySpawnCreatureIn<BogArea>(rng, scene, new FrogArchetype(), 0.6);
        TrySpawnCreatureIn<FenArea>(rng, scene, new ToadArchetype(), 0.4);
        TrySpawnCreatureIn<FenArea>(rng, scene, new BitternArchetype(), 0.55);
        TrySpawnCreatureIn<OxbowArea>(rng, scene, new HeronArchetype(), 0.6);
        TrySpawnCreatureIn<WaterMeadowArea>(rng, scene, new LapwingArchetype(), 0.65);
        TrySpawnCreatureIn<GrasslandArea>(rng, scene, new LapwingArchetype(), 0.25);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Cultivated, 2, 5);
    }
}
