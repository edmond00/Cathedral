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

namespace Cathedral.Game.Scene.Canyon;

/// <summary>
/// Builds a canyon — the broken ground <c>ClimateRule</c> puts wherever temperate plain or field
/// meets hot steppe, as the coast lies between land and sea.
///
/// Laid out as a spine: the canyon floor (wash, talus, slot, seep, a pothole pool swum across) runs
/// through, and off it hang the rim country with its mesa, a side canyon ending in a box with an
/// alcove up a toe-hold trail, and badlands of clay and hoodoos.
/// Climbs: the mesa wall, whose top sees the whole place; the toe-hold trail to the alcove.
/// Uninhabited: no settlement stands in hot country yet.
/// </summary>
public class CanyonSceneFactory : WildSceneFactory
{
    public CanyonSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _rim, _wash, _mesa, _alcove;
    private readonly List<Area> _walls = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // A spine: the canyon floor runs through, and everything else is reached from it.
        Profile = LayoutProfile.Spine;

        // ── The floor ────────────────────────────────────────────────────────
        _wash = BuildWash();
        var floor = new List<Area> { _wash };
        floor.AddRange(Draw(rng, new Option[]
        {
            new(BuildTalus, 1.5),
            new(BuildSlot, 1.4),
            new(BuildSeep, 1.0),
            new(BuildPlungePool, 0.9),
            new(BuildBoulderChoke, 0.8),
        }, Between(rng, 2, 3)));
        Shuffle(rng, floor);
        _walls.AddRange(floor.Where(a => a is SlotArea or SeepArea));

        // ── What hangs off it ────────────────────────────────────────────────
        bool hasRim  = rng.NextDouble() < 0.6;
        bool hasSide = rng.NextDouble() < 0.55;
        bool hasBad  = rng.NextDouble() < 0.45;
        // Two of the three at least, so the floor is never alone with one branch.
        if ((hasRim ? 1 : 0) + (hasSide ? 1 : 0) + (hasBad ? 1 : 0) < 2)
        {
            var missing = new List<int>();
            if (!hasRim) missing.Add(0);
            if (!hasSide) missing.Add(1);
            if (!hasBad) missing.Add(2);
            switch (missing[rng.Next(missing.Count)]) { case 0: hasRim = true; break; case 1: hasSide = true; break; default: hasBad = true; break; }
        }

        var rim = new List<Area>();
        if (hasRim)
        {
            rim.Add(_rim = BuildRim());
            if (rng.NextDouble() < 0.45) rim.Add(BuildSlickrock());
            if (rng.NextDouble() < 0.6) rim.Add(_mesa = BuildMesa());
            _walls.Add(_rim);
        }
        var side = new List<Area>();
        if (hasSide)
        {
            side.AddRange(Draw(rng, new Option[]
            {
                new(BuildSideCanyon, 2.0),
                new(BuildCottonwoods, 1.2),
                new(BuildAmphitheatre, 1.2),
            }, Between(rng, 1, 2)));
            if (rng.NextDouble() < 0.55) _alcove = BuildAlcove();
        }
        var bad = new List<Area>();
        if (hasBad)
        {
            bad.Add(BuildBadlands());
            if (rng.NextDouble() < 0.5) bad.Add(BuildHoodooGarden());
        }

        foreach (var area in floor.Concat(rim).Concat(side).Concat(bad)) Populate(area, rng);
        if (_alcove != null) Populate(_alcove, rng);

        AddSection(scene, "Canyon Floor", "Banded walls going up out of sight, the strip of sky between them very blue",
            seed => new CorridorGenerator { Seed = seed }, floor, tier: 0);
        Section? rimSection = rim.Count == 0 ? null : AddSection(scene, "Rim Country",
            "Juniper and slickrock along the edge of the drop, the far wall glowing across the gulf",
            seed => new WaveGenerator { Seed = seed }, rim.Where(a => a != _mesa).ToList(), tier: 1);
        Section? sideSection = side.Count == 0 ? null : AddSection(scene, "Side Canyon",
            "A branch of the canyon going back into the rock, narrower and quieter, ending in a wall",
            seed => new CorridorGenerator { Seed = seed }, side, tier: 1);
        if (bad.Count > 0)
            AddSection(scene, "Badlands", "Bare clay hills gullied into ribs and spires, nothing growing, colours banded like cloth",
                seed => new WaveGenerator { Seed = seed }, bad, tier: 1);

        string[] moods = { "sandy", "winding", "walled" };
        static string Way(Area a, Area b) =>
            a is RimArea || b is RimArea ? "Switchback Trail"
            : a is SlotArea || b is SlotArea ? "Narrows"
            : "Wash Path";

        // The floor is a line, except that a plunge pool in it is swum, not walked round.
        var pool = floor.OfType<PoolArea>().FirstOrDefault();
        for (int i = 0; i < floor.Count - 1; i++)
        {
            var (a, b) = (floor[i], floor[i + 1]);
            if (a == pool || b == pool)
                Swim(scene, a, b, WaterKind.Pond, "Pothole Pool",
                    "A deep green pool scoured out of the canyon floor between sheer walls, no way past but through it",
                    new[] { "green", "walled", "cold" }, lesson: "plunge");
            else
                OutdoorLayout.Link(scene, a, b, Way(a, b), moods, "along the canyon");
        }

        if (rim.Count > 0)
        {
            WeaveSection(rng, scene, rim.Where(x => x != _mesa).ToList(), Weave.Line, (_, _) => "Rim Path", "along the rim", moods);
            OutdoorLayout.Link(scene, floor[rng.Next(floor.Count)], _rim!, "Switchback Trail", moods, "up the canyon wall");
        }
        if (side.Count > 0)
        {
            WeaveSection(rng, scene, side, Weave.Line, Way, "into the side canyon", moods);
            OutdoorLayout.Link(scene, floor[rng.Next(floor.Count)], side[0], "Wash Path", moods, "into the side canyon");
        }
        if (bad.Count > 0)
        {
            WeaveSection(rng, scene, bad, Weave.Line, (_, _) => "Gully Path", "through the badlands", moods);
            OutdoorLayout.Link(scene, floor[rng.Next(floor.Count)], bad[0], "Gully Path", moods, "out of the canyon");
        }

        if (_mesa != null)
        {
            AddToSection(scene, rimSection!, _mesa);
            AddViewpoint(scene, _rim!, _mesa, "Mesa Wall",
                "A wall of banded red rock going up to the mesa, ledged and cracked, the cracks just wide enough for a hand",
                new[] { "banded", "sheer", "ledged", "sun-hot" }, lesson: "summit_fever");
        }
        if (_alcove != null)
        {
            // A climb with no view: the alcove looks only at the opposite wall, and that is its point.
            AddToSection(scene, sideSection!, _alcove);
            if (rng.NextDouble() < 0.5)
                Climb(scene, side[^1], _alcove, "Toe-Hold Trail",
                    "Shallow cups pecked into the rock a stride apart, going up to a dark alcove in the wall",
                    new[] { "pecked", "worn", "dizzying", "old" });
            else
                SlipInto(scene, side[^1], _alcove, SlipKind.Crevice, "Fallen Block",
                    "A block the size of a house leaning against the wall, a dark slot behind it going up into the rock",
                    new[] { "leaning", "dark", "tight" }, lesson: "potholing");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Arid, _mesa, _alcove);

        Console.WriteLine($"CanyonSceneFactory: floor {floor.Count}, rim {rim.Count}, side {side.Count}, badlands {bad.Count}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildRim() => new RimArea(
        displayName: "Canyon Rim",
        contextDescription: "on the canyon rim",
        transitionDescription: "walk out to the rim",
        descriptions: new() { "The edge of the world: slickrock and juniper, then nothing, then the far wall a long way off" },
        moods: new[] { "vertiginous", "windy", "glowing", "vast" });

    private static Area BuildMesa() => new MesaArea(
        displayName: "Mesa Top",
        contextDescription: "on top of the mesa",
        transitionDescription: "pull yourself over the mesa's lip",
        descriptions: new() { "A flat island of rock high above the canyon, grassed and empty, falling away sheer on every side" },
        moods: new[] { "high", "flat", "islanded", "commanding" });

    private static Area BuildTalus() => new ScreeArea(
        displayName: "Talus Slope",
        contextDescription: "on the talus slope",
        transitionDescription: "start down the talus",
        descriptions: new() { "A fan of broken blocks fallen from the walls above, loose and clinking underfoot" },
        moods: new[] { "loose", "clinking", "steep", "hot" });

    private static Area BuildWash() => new WashArea(
        displayName: "Dry Wash",
        contextDescription: "in the dry wash",
        transitionDescription: "step down into the wash",
        descriptions: new() { "The canyon floor: a bed of sand and cobbles between the walls, the high-water line drawn in drift above" },
        moods: new[] { "sandy", "walled", "echoing", "flood-marked" });

    private static Area BuildSlot() => new SlotArea(
        displayName: "Slot Canyon",
        contextDescription: "in the slot canyon",
        transitionDescription: "turn sideways into the slot",
        descriptions: new() { "A crack in the earth a shoulder wide and a hundred feet deep, its walls carved by water into ribbons" },
        moods: new[] { "narrow", "glowing", "ribboned", "claustrophobic" });

    private static Area BuildSeep() => new SeepArea(
        displayName: "Seep Spring",
        contextDescription: "at the seep spring",
        transitionDescription: "duck under the dripping ledge",
        descriptions: new() { "Water weeping out of the rock under an overhang, a hanging garden of fern and moss below it" },
        moods: new[] { "dripping", "green", "cool", "secret" });

    private static Area BuildPlungePool() => new PoolArea(
        displayName: "Pothole Pool",
        contextDescription: "by the pothole pool",
        transitionDescription: "come down to the pothole pool",
        descriptions: new() { "A round green pool scoured into the canyon floor by flood-spun boulders, walls going up sheer on both sides" },
        moods: new[] { "round", "green", "walled", "cold" });

    private static Area BuildAlcove() => new AlcoveArea(
        displayName: "Cliff Alcove",
        contextDescription: "in the cliff alcove",
        transitionDescription: "climb into the alcove",
        descriptions: new() { "A great scoop in the canyon wall, smoke-blackened at the back, with the stubs of masonry walls across it" },
        moods: new[] { "hollow", "smoke-blackened", "deserted", "sheltered" });

    private static Area BuildSideCanyon() => new WashArea(
        displayName: "Side Canyon",
        contextDescription: "in the side canyon",
        transitionDescription: "turn off into the side canyon",
        descriptions: new() { "A narrower canyon branching off the main one, cottonwoods along its floor, the walls closing in as it goes back" },
        moods: new[] { "narrowing", "shaded", "quiet", "green-floored" });

    private static Area BuildAmphitheatre() => new AmphitheatreArea(
        displayName: "Box Canyon",
        contextDescription: "in the box canyon",
        transitionDescription: "walk into the head of the box canyon",
        descriptions: new() { "The head of the side canyon, a great bowl of wall all round, a dry pour-off high up where the water comes over in flood" },
        moods: new[] { "enclosed", "echoing", "bowl-like", "sun-trapped" });

    private static Area BuildCottonwoods() => new GroveArea(
        displayName: "Cottonwood Bottom",
        contextDescription: "in the cottonwood bottom",
        transitionDescription: "walk in under the cottonwoods",
        descriptions: new() { "A flat of green grass and great cottonwoods where the side canyon widens, water close under the sand, shade at last" },
        moods: new[] { "green", "shaded", "rustling", "cool" });

    private static Area BuildSlickrock() => new LedgeArea(
        displayName: "Slickrock Bench",
        contextDescription: "on the slickrock bench",
        transitionDescription: "walk out onto the slickrock",
        descriptions: new() { "A broad shelf of bare pink sandstone below the rim, swirled like frozen dunes, potholes of rainwater in it" },
        moods: new[] { "bare", "swirled", "pink", "grippy" });

    private static Area BuildBoulderChoke() => new BoulderArea(
        displayName: "Boulder Choke",
        contextDescription: "in the boulder choke",
        transitionDescription: "scramble into the boulder choke",
        descriptions: new() { "The canyon floor jammed with fallen blocks the size of carts, the way on going under, over and between them" },
        moods: new[] { "jammed", "shadowed", "awkward", "cool" });

    private static Area BuildBadlands() => new BadlandsArea(
        displayName: "Badlands",
        contextDescription: "in the badlands",
        transitionDescription: "climb out into the badlands",
        descriptions: new() { "Bare clay hills above the canyon, gullied into ribs by rain, banded grey and purple and rust, nothing growing" },
        moods: new[] { "bare", "ribbed", "banded", "barren" });

    private static Area BuildHoodooGarden() => new BadlandsArea(
        displayName: "Hoodoo Garden",
        contextDescription: "among the hoodoos",
        transitionDescription: "walk in among the hoodoos",
        descriptions: new() { "Spires of soft rock standing in crowds, each with a cap of harder stone, the ground between them white and crunching" },
        moods: new[] { "spired", "crowded", "uncanny", "white" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case RimArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildJuniper());
                area.PointsOfInterest.Add(BuildYucca());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildPinyon());
                break;
            case MesaArea:
                area.PointsOfInterest.Add(BuildArch());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildPinyon());
                break;
            case ScreeArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildFallenRocks());
                area.PointsOfInterest.Add(BuildJasperGravel());
                break;
            case WashArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildPetroglyphs());
                area.PointsOfInterest.Add(BuildFloodWrack());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildJuniper());
                break;
            case SlotArea:
                area.PointsOfInterest.Add(BuildRibbonWalls());
                area.PointsOfInterest.Add(BuildJammedLog());
                break;
            case SeepArea:
                area.PointsOfInterest.Add(BuildSeepPoi());
                area.PointsOfInterest.Add(BuildSwallowNests());
                break;
            case PoolArea:
                area.PointsOfInterest.Add(BuildPotholeWater());
                break;
            case AlcoveArea:
                area.PointsOfInterest.Add(BuildMasonry());
                area.PointsOfInterest.Add(BuildGrindingStones());
                break;
            case GroveArea:
                area.PointsOfInterest.Add(BuildCottonwoodTrees());
                area.PointsOfInterest.Add(BuildSeepPoi());
                break;
            case LedgeArea:
                area.PointsOfInterest.Add(BuildPotholeWater());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case BoulderArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildBoulder());
                area.PointsOfInterest.Add(BuildJammedLog());
                break;
            case AmphitheatreArea:
                area.PointsOfInterest.Add(BuildPourOff());
                area.PointsOfInterest.Add(BuildSwallowNests());
                break;
            case BadlandsArea:
                area.PointsOfInterest.Add(rng.NextDouble() < 0.5 ? BuildHoodoos() : BuildAgateNodules());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
        }
    }

    private static PointOfInterest BuildPotholeWater() => new PoolPointOfInterest(
        displayName: "Pothole Water",
        descriptions: new() { "Still green water in a stone bowl, cold even at noon, the scour-stones that cut it lying on the bottom" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "still", "green", "cold", "bowl-shaped" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "floodwatch", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildCottonwoodTrees() => new TreePointOfInterest(
        displayName: "Cottonwoods",
        descriptions: new() { "Great pale-trunked cottonwoods, their leaves clattering in the least wind, cotton drifting off them like snow" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Bark()) },
        moods: new[] { "pale-trunked", "clattering", "drifting", "great" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "dowsing", ["listen"] = "wind_reading" } };

    private static PointOfInterest BuildPourOff() => new RockPointOfInterest(
        displayName: "Dry Pour-Off",
        descriptions: new() { "A black-streaked lip high in the head wall where the floods come over, a scoured hollow under it in the floor" },
        items: new() { new ItemElement(new Sandstone()), new ItemElement(new Driftwood()) },
        moods: new[] { "black-streaked", "high", "scoured", "dry" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "floodwatch", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildAgateNodules() => new RockPointOfInterest(
        displayName: "Agate Nodules",
        descriptions: new() { "Rough grey lumps weathering out of the clay, one split by frost to show rings of white and rust inside" },
        items: new() { new ItemElement(new Agate()), new ItemElement(new Flint()) },
        moods: new[] { "lumpy", "weathered", "banded", "hidden" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "veinsight" } };

    private static PointOfInterest BuildYucca() => new YuccaPointOfInterest(
        displayName: "Yucca",
        descriptions: new() { "A yucca like a bundle of daggers, a tall spike of cream bells rising out of the middle" },
        items: new() { new ItemElement(new YuccaRoot()), new ItemElement(new Thorn()) },
        moods: new[] { "spiked", "stiff", "belled", "tough" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "survivalism", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildHoodoos() => new HoodooPointOfInterest(
        displayName: "Hoodoos",
        descriptions: new() { "Pillars of soft rock left standing by the rain, each wearing a cap of harder stone, like a crowd waiting" },
        items: new() { new ItemElement(new Sandstone()) },
        moods: new[] { "capped", "eroded", "crowd-like", "uncanny" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework", ["contemplate"] = "superstition" } };

    private static PointOfInterest BuildArch() => new ArchPointOfInterest(
        displayName: "Stone Arch",
        descriptions: new() { "A span of red rock left standing across the sky when the rest of the wall fell away" },
        items: new() { new ItemElement(new Sandstone()) },
        moods: new[] { "spanning", "red", "impossible", "framing" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "geometric_scheme", ["contemplate"] = "awe", ["listen"] = "wind_reading" } };

    private static PointOfInterest BuildJasperGravel() => new RockPointOfInterest(
        displayName: "Jasper Gravel",
        descriptions: new() { "A spill of gravel at the talus foot, blood-red pebbles of jasper showing among the brown" },
        items: new() { new ItemElement(new Jasper()), new ItemElement(new Flint()), new ItemElement(new Rock()) },
        moods: new[] { "spilled", "red-flecked", "loose", "bright" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "veinsight" } };

    private static PointOfInterest BuildFloodWrack() => new DeadfallPointOfInterest(
        displayName: "Flood Wrack",
        descriptions: new() { "A tangle of branches and bleached wood jammed high against a boulder by the last flood" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Twig()), new ItemElement(new Driftwood()) },
        moods: new[] { "jammed", "bleached", "high-flung", "dry" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "floodwatch" } };

    private static PointOfInterest BuildRibbonWalls() => new RockPointOfInterest(
        displayName: "Ribboned Walls",
        descriptions: new() { "Walls carved by flood into smooth ribbons and flutes, glowing orange where the light finds them" },
        items: new() { new ItemElement(new Sandstone()) },
        moods: new[] { "fluted", "glowing", "smooth", "echoing" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["contemplate"] = "aesthetic", ["listen"] = "hollow_ear" } };

    private static PointOfInterest BuildJammedLog() => new LogPointOfInterest(
        displayName: "Jammed Log",
        descriptions: new() { "A whole tree trunk wedged across the slot high above, put there by a flood that filled it to the brim" },
        items: new() { new ItemElement(new Log()) },
        moods: new[] { "wedged", "high", "ominous", "bleached" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildSeepPoi() => new SeepPointOfInterest(
        displayName: "Weeping Rock",
        descriptions: new() { "Water oozing from a seam in the rock and dripping in a curtain, ferns hanging all along the drip line" },
        items: new() { new ItemElement(new Fern()), new ItemElement(new Moss()) },
        moods: new[] { "weeping", "fern-hung", "cool", "dripping" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "drainage", ["listen"] = "water_voice", ["smell"] = "petrichor", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildSwallowNests() => new NestPointOfInterest(
        displayName: "Swallow Nests",
        descriptions: new() { "Little jugs of dried mud stuck in rows under the overhang, birds darting in and out of them" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Egg()), new ItemElement(new Feather()) },
        moods: new[] { "rowed", "darting", "noisy", "mud-built" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["listen"] = "birdsong" } };

    private static PointOfInterest BuildMasonry() => new RubblePointOfInterest(
        displayName: "Fallen Masonry",
        descriptions: new() { "The stubs of small square rooms built of shaped stone and mud, their roofs long gone, a doorway shaped like a keyhole" },
        items: new() { new ItemElement(new Sandstone()), new ItemElement(new Rock()) },
        moods: new[] { "square", "keyholed", "empty", "patient" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "ruin_sense" } };

    private static PointOfInterest BuildGrindingStones() => new MortarPointOfInterest(
        displayName: "Grinding Hollows",
        descriptions: new() { "Smooth hollows worn into the alcove floor by grinding, a hand-stone still lying in one of them" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new WildMillet()) },
        moods: new[] { "worn", "smooth", "abandoned", "domestic" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "hearthlonging" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new PumaArchetype(), 0.35);

        TrySpawnCreature(rng, scene, new BighornArchetype(),     0.50);
        TrySpawnCreature(rng, scene, new CondorArchetype(),      0.40);
        TrySpawnCreature(rng, scene, new CanyonWrenArchetype(),  0.55, _walls);
        TrySpawnCreature(rng, scene, new RockDoveArchetype(),    0.50);
        TrySpawnCreature(rng, scene, new SwiftArchetype(),       0.50, _walls);
        TrySpawnCreature(rng, scene, new RattlesnakeArchetype(), 0.35);
        TrySpawnCreature(rng, scene, new RingtailArchetype(),    0.35);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Arid, 2, 4);
    }
}
