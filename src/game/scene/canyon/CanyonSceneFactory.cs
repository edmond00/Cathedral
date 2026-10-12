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
/// Sections: Rim Country (the rim, and the mesa above it) and Canyon Floor (talus, wash, and when
/// rolled a slot canyon, a seep spring and an alcove in the wall).
/// Climbs: the mesa wall, whose top sees the whole place; and sometimes a toe-hold trail to an alcove
/// where somebody once built.
/// Uninhabited: no settlement stands in hot country yet.
/// </summary>
public class CanyonSceneFactory : ClimateSceneFactory
{
    public CanyonSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _rim, _wash, _mesa, _alcove;
    private readonly List<Area> _walls = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        bool hasSlot   = rng.NextDouble() < 0.60;
        bool hasSeep   = rng.NextDouble() < 0.50;
        bool hasAlcove = rng.NextDouble() < 0.50;

        _rim  = BuildRim();
        _mesa = BuildMesa();
        _wash = BuildWash();

        var floor = new List<Area> { BuildTalus(), _wash };
        if (hasSlot) floor.Add(BuildSlot());
        if (hasSeep) floor.Add(BuildSeep());
        if (hasAlcove) _alcove = BuildAlcove();

        _walls.AddRange(floor.Where(a => a is SlotArea or SeepArea).Append(_rim));

        var everything = new List<Area> { _rim, _mesa };
        everything.AddRange(floor);
        if (_alcove != null) everything.Add(_alcove);
        foreach (var area in everything) Populate(area, rng);

        AddSection(scene, "Rim Country", "Juniper and slickrock along the edge of the drop, the far wall glowing across the gulf",
            seed => new WaveGenerator { Seed = seed }, new[] { _rim, _mesa });
        var floorAreas = _alcove != null ? floor.Append(_alcove) : floor;
        AddSection(scene, "Canyon Floor", "Banded walls going up out of sight, the strip of sky between them very blue",
            seed => new CorridorGenerator { Seed = seed }, floorAreas);

        ConnectChain(scene, new List<Area> { _rim }.Concat(floor).ToList(),
            (a, b) => a is RimArea || b is RimArea ? "Switchback Trail"
                    : a is SlotArea || b is SlotArea ? "Narrows"
                    : "Wash Path",
            "along the canyon", new[] { "sandy", "winding", "walled" });

        AddViewpoint(scene, _rim, _mesa, "Mesa Wall",
            "A wall of banded red rock going up to the mesa, ledged and cracked, the cracks just wide enough for a hand",
            new[] { "banded", "sheer", "ledged", "sun-hot" });

        if (_alcove != null)
        {
            // A climb with no view: the alcove looks only at the opposite wall, and that is its point.
            new CliffPointOfInterest(_wash, _alcove, "Toe-Hold Trail",
                new() { "Shallow cups pecked into the rock a stride apart, going up to a dark alcove in the wall" },
                moods: new[] { "pecked", "worn", "dizzying", "old" }).AttachTo(scene);
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Arid, _mesa, _alcove);

        Console.WriteLine($"CanyonSceneFactory: slot={hasSlot}, seep={hasSeep}, alcove={hasAlcove}, {_allAreas.Count} areas");
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

    private static Area BuildAlcove() => new AlcoveArea(
        displayName: "Cliff Alcove",
        contextDescription: "in the cliff alcove",
        transitionDescription: "climb into the alcove",
        descriptions: new() { "A great scoop in the canyon wall, smoke-blackened at the back, with the stubs of masonry walls across it" },
        moods: new[] { "hollow", "smoke-blackened", "deserted", "sheltered" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case RimArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildJuniper());
                area.PointsOfInterest.Add(BuildYucca());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildPinyon());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildHoodoos());
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
            case AlcoveArea:
                area.PointsOfInterest.Add(BuildMasonry());
                area.PointsOfInterest.Add(BuildGrindingStones());
                break;
        }
    }

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
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage" } };

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
