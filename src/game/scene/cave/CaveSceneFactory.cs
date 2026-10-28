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

namespace Cathedral.Game.Scene.Cave;

/// <summary>
/// Builds a cave, laid out as a descent: one mouth, galleries below it, depths below those.
///
/// <para><b>Three levels.</b> The mouth (tier 0) is the entrance hall, sometimes a rock shelter
/// outside it, and the miner's ore store when there is a miner; only the hall goes down. The galleries
/// (tier 1) are three or four chambers and passages. The depths (tier 2) are two or three places at the
/// bottom, reached from the galleries by one way down — a walk down a steep passage, a crawlway on the
/// belly, or a climb down a chasm wall.</para>
///
/// <para><b>Three caves.</b> A <i>mined</i> cave is a working: a ladder down to the shaft, ore and
/// coal in the walls, perhaps a miner. A <i>karst</i> cave is the rock's own: flowstone halls, bat
/// colonies, a sinkhole open to the sky, a river below. A <i>den</i> is a beast's: gnawed bones, a
/// pit animals fell into, a bear.</para>
///
/// <para><b>Ways that are not a walk.</b> The ladder; the chasm; a crawlway to a crystal grotto; an
/// underground river with a far bank, swum; a bone pit climbed down into.</para>
/// </summary>
public class CaveSceneFactory : WildSceneFactory
{
    public CaveSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Mined, Karst, Den }
    private enum Ore { Iron, Coal, Stone }

    private Identity _identity;
    private Ore _ore;
    private bool _hasMiner;
    private Area? _entrance, _shaft, _oreChamber, _oreStore, _farBank;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Profile = LayoutProfile.Descent;
        double roll = rng.NextDouble();
        _identity = roll < 0.35 ? Identity.Mined : roll < 0.75 ? Identity.Karst : Identity.Den;
        _ore = (Ore)rng.Next(3);
        _hasMiner = _identity == Identity.Mined && rng.NextDouble() < 0.6;

        // ── The mouth ────────────────────────────────────────────────────────
        _entrance = BuildEntranceHall();
        var mouth = new List<Area> { _entrance };
        Area? overhang = rng.NextDouble() < 0.5 ? BuildOverhang() : null;
        if (overhang != null) mouth.Add(overhang);

        // ── The galleries ────────────────────────────────────────────────────
        List<Area> galleries;
        if (_identity == Identity.Mined)
        {
            _shaft = BuildMainShaft();
            galleries = new List<Area> { _shaft };
            if (_ore != Ore.Stone) { _oreChamber = BuildOreChamber(); galleries.Add(_oreChamber); }
            if (_ore == Ore.Coal || rng.NextDouble() < 0.2) galleries.Add(BuildCoalSeam());
            galleries.AddRange(Draw(rng, new Option[]
            {
                new(BuildCollapsedTunnel, 1.2),
                new(BuildSideAlcove, 1.0),
                new(BuildColony, 0.8),
                new(BuildSinkhole, 0.4),
            }, Math.Max(1, Between(rng, 3, 4) - galleries.Count)));
        }
        else
        {
            galleries = Draw(rng, new Option[]
            {
                new(BuildGreatChamber, _identity == Identity.Karst ? 2.0 : 1.0),
                new(BuildFlowstone,    _identity == Identity.Karst ? 2.5 : 0.4),
                new(BuildColony,       1.4),
                new(BuildSinkhole,     _identity == Identity.Karst ? 1.2 : 0.3),
                new(BuildSideAlcove,   1.0),
                new(BuildWindingPassage, 1.2),
                new(BuildDen,          _identity == Identity.Den ? 3.0 : 0.2),
            }, Between(rng, 3, 4));
        }

        // ── The depths ───────────────────────────────────────────────────────
        var depths = Draw(rng, new Option[]
        {
            new(BuildUndergroundPool, 1.2),
            new(BuildUndergroundRiver, _identity == Identity.Karst ? 2.0 : 0.8),
            new(BuildChasm, 1.0),
            new(BuildBonePit, _identity == Identity.Den ? 2.5 : 0.5),
            new(BuildCrystalGrotto, _identity == Identity.Karst ? 1.4 : 0.7),
        }, Between(rng, 2, 3));
        var river = depths.OfType<RiverArea>().FirstOrDefault();
        Area? farBank = _farBank = river != null && rng.NextDouble() < 0.7 ? BuildFarBank() : null;
        if (farBank != null) depths.Add(farBank);

        foreach (var area in mouth.Concat(galleries).Concat(depths)) Populate(area, rng);

        var mouthSection = AddSection(scene, "Cave Mouth", "Entrance zone where daylight reaches; relatively safe",
            seed => new RoomsGenerator { Seed = seed }, mouth, tier: 0);
        AddSection(scene, _identity == Identity.Mined ? "The Workings" : "Galleries",
            "Passages and chambers beyond the daylight, only the lantern's light reaching",
            seed => new CorridorGenerator { Seed = seed }, galleries, tier: 1);
        AddSection(scene, "The Depths", "The bottom of the cave, where the water goes and nothing has been for a long time",
            seed => new RoomsGenerator { Seed = seed }, depths, tier: 2);

        string[] moods = { "narrow", "rough-hewn", "echoing" };
        static string Way(Area a, Area b) =>
            a is PoolArea || b is PoolArea || a is RiverArea || b is RiverArea ? "Flooded Passage" : "Passage";

        // The mouth: the shelter opens on the hall, and the hall is the only way down.
        if (overhang != null) OutdoorLayout.Link(scene, overhang, _entrance, "Cave Mouth", moods, "under the overhang");
        if (_shaft != null)
        {
            // The ladder IS the way down. An edge beside it would hand a free bypass round the climb.
            Climb(scene, _shaft, _entrance, "Mineshaft Ladder",
                "A long timber ladder fixed against the rock, descending into the darker tunnels below",
                new[] { "long", "rope-bound", "creaking", "narrow" });
        }
        else
            OutdoorLayout.Link(scene, _entrance, galleries[0], "Sloping Passage", moods, "down into the cave");

        WeaveSection(rng, scene, galleries, _shaft != null ? Weave.Hub : RollWeave(rng), Way, "through the rock", moods);

        // The depths: what is reached by its own way stays out of the weave.
        Area? grotto = depths.OfType<GrottoArea>().FirstOrDefault();
        Area? pit    = depths.OfType<PitArea>().FirstOrDefault();
        var chasm  = depths.OfType<ChasmArea>().FirstOrDefault();
        var walk   = depths.Where(a => a != grotto && a != pit && a != farBank).ToList();
        if (walk.Count == 0) { walk.Add(grotto ?? pit!); if (grotto != null) grotto = null; else pit = null; }
        WeaveSection(rng, scene, walk, RollWeave(rng, Weave.Line, Weave.Hub), Way, "in the deep", moods);

        // One way down from the galleries: down the chasm wall, through a crawlway, or a steep passage.
        var above = galleries[rng.Next(galleries.Count)];
        var below = chasm ?? walk[0];
        if (below is ChasmArea)
            Climb(scene, below, above, "Chasm Wall",
                "A wall of wet rock dropping away into a blackness the lantern does not reach the bottom of",
                new[] { "wet", "sheer", "bottomless" });
        else if (rng.NextDouble() < 0.45)
            SlipInto(scene, above, below, SlipKind.Crawlway, "Crawlway",
                "A passage the height of a forearm, floored with wet grit, going down and on into the dark",
                new[] { "low", "wet", "tight" }, lesson: "potholing");
        else
            OutdoorLayout.Link(scene, above, below, "Steep Passage", moods, "down into the deep");

        if (grotto != null)
            SlipInto(scene, walk[rng.Next(walk.Count)], grotto, SlipKind.Crawlway, "Squeeze",
                "A crack low in the wall with cold air breathing out of it and, far in, a glitter",
                new[] { "tight", "glittering", "breathing" }, lesson: "potholing");
        if (pit != null)
            Climb(scene, pit, walk[rng.Next(walk.Count)], "Pit Edge",
                "The lip of a natural shaft, its sides worn smooth, the smell of old bones coming up out of it",
                new[] { "smooth", "deep", "foul" });
        if (farBank != null)
            Swim(scene, river!, farBank, WaterKind.River, "Black Water",
                "The underground river, fast and black and so cold it aches, the far side a pale ledge at the edge of the light",
                new[] { "black", "fast", "aching" }, lesson: "plunge");

        if (_hasMiner)
        {
            _oreStore = CampSubfactory.BuildOreStore();
            AddToSection(scene, mouthSection, _oreStore);
            OutdoorLayout.Link(scene, _entrance, _oreStore, "Passage");
            scene.Stores.Add(_oreStore);
        }

        // Shortcuts never touch the mouth's side places, which would make a second way down.
        Furnish(rng, scene, FurnitureSubfactory.Setting.Underground, overhang, _oreStore, grotto, pit, farBank);

        Console.WriteLine($"CaveSceneFactory: {_identity} ({_ore}), {mouth.Count}/{galleries.Count}/{depths.Count} areas, miner={_hasMiner}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildEntranceHall() => new EntranceArea(
        displayName: "Entrance Hall",
        contextDescription: "in the cave's entrance hall",
        transitionDescription: "step into the cave entrance",
        descriptions: new() { "The cave mouth opens into a wide low chamber lit by daylight from outside" },
        moods: new[] { "dim", "echoing", "cool", "damp", "wide" });

    private static Area BuildOverhang() => new OverhangArea(
        displayName: "Rock Shelter",
        contextDescription: "under the rock shelter",
        transitionDescription: "step under the rock shelter",
        descriptions: new() { "A dry floor under a jutting roof of rock beside the cave mouth, the ceiling black with the smoke of old fires" },
        moods: new[] { "dry", "smoke-blackened", "open", "ancient" });

    private static Area BuildMainShaft() => new ShaftArea(
        displayName: "Main Shaft",
        contextDescription: "in the main shaft",
        transitionDescription: "descend into the main shaft",
        descriptions: new() { "A long passage cut deep into the rock, the air close and damp" },
        moods: new[] { "narrow", "dark", "damp", "low-roofed", "echoing" });

    private static Area BuildOreChamber() => new ChamberArea(
        displayName: "Ore Chamber",
        contextDescription: "in the ore chamber",
        transitionDescription: "step into the ore chamber",
        descriptions: new() { "A wider chamber where a vein of iron ore breaks through the rock" },
        moods: new[] { "iron-stained", "rough-walled", "cool", "lantern-lit" });

    private static Area BuildGreatChamber() => new ChamberArea(
        displayName: "Great Chamber",
        contextDescription: "in the great chamber",
        transitionDescription: "step out into the great chamber",
        descriptions: new() { "A hall in the rock so large the lantern finds neither roof nor far wall, only a floor of fallen blocks going on" },
        moods: new[] { "vast", "echoing", "black", "still" });

    private static Area BuildCoalSeam() => new SeamArea(
        displayName: "Coal Seam",
        contextDescription: "at the coal seam",
        transitionDescription: "step to the coal seam",
        descriptions: new() { "A glittering black seam of coal cuts across the chamber wall" },
        moods: new[] { "black", "glittering", "soot-covered", "close" });

    private static Area BuildCollapsedTunnel() => new TunnelArea(
        displayName: "Collapsed Tunnel",
        contextDescription: "at the collapsed tunnel",
        transitionDescription: "approach the collapsed tunnel",
        descriptions: new() { "A dead-end of fallen rock and rubble, the way blocked" },
        moods: new[] { "dead-end", "rubble", "still", "warning" });

    private static Area BuildWindingPassage() => new TunnelArea(
        displayName: "Winding Passage",
        contextDescription: "in the winding passage",
        transitionDescription: "follow the winding passage",
        descriptions: new() { "A passage scalloped smooth by water long gone, twisting so the lantern never shows more than a few strides" },
        moods: new[] { "scalloped", "twisting", "smooth", "dark" });

    private static Area BuildSideAlcove() => new AlcoveArea(
        displayName: "Side Alcove",
        contextDescription: "in the side alcove",
        transitionDescription: "step into the side alcove",
        descriptions: new() { "A small offshoot from the passage, the air still and silent" },
        moods: new[] { "small", "still", "hidden" });

    private static Area BuildColony() => new ColonyArea(
        displayName: "Bat Colony",
        contextDescription: "under the bat colony",
        transitionDescription: "step into the colony chamber",
        descriptions: new() { "A high chamber whose roof moves: bats in their thousands, and under them a soft reeking floor of droppings" },
        moods: new[] { "reeking", "rustling", "warm", "seething" });

    private static Area BuildFlowstone() => new FlowstoneArea(
        displayName: "Flowstone Hall",
        contextDescription: "in the flowstone hall",
        transitionDescription: "step into the flowstone hall",
        descriptions: new() { "A hall where the rock has flowed: curtains of stone, columns floor to roof, terraced pools rimmed with white" },
        moods: new[] { "dripping", "glistening", "slow", "cathedral-like" });

    private static Area BuildSinkhole() => new SinkholeArea(
        displayName: "Sinkhole",
        contextDescription: "under the sinkhole",
        transitionDescription: "step into the light of the sinkhole",
        descriptions: new() { "A chamber whose roof fell in long ago: a shaft of daylight comes down onto ferns growing on the fallen stone" },
        moods: new[] { "sunlit", "green", "dripping", "startling" });

    private static Area BuildDen() => new DenArea(
        displayName: "Bear's Den",
        contextDescription: "in the den",
        transitionDescription: "step into the den",
        descriptions: new() { "A dry chamber with a trampled bed of leaves and hair in its corner and claw-scores on the walls higher than a man" },
        moods: new[] { "musky", "trampled", "claw-scored", "warm" });

    private static Area BuildUndergroundPool() => new PoolArea(
        displayName: "Underground Pool",
        contextDescription: "by the underground pool",
        transitionDescription: "approach the underground pool",
        descriptions: new() { "A still dark pool fed by water seeping through the rock" },
        moods: new[] { "still", "dark", "wet", "cold", "echoing" });

    private static Area BuildUndergroundRiver() => new RiverArea(
        displayName: "Underground River",
        contextDescription: "on the bank of the underground river",
        transitionDescription: "follow the roar down to the underground river",
        descriptions: new() { "A river in the dark, coming out of the rock and going back into it, loud in the enclosed space" },
        moods: new[] { "roaring", "black", "fast", "cold" });

    private static Area BuildFarBank() => new AlcoveArea(
        displayName: "Far Bank",
        contextDescription: "on the far bank",
        transitionDescription: "drag yourself out on the far bank",
        descriptions: new() { "A ledge of pale stone across the river, heaped with flood-wrack, a passage running on from it into the dark" },
        moods: new[] { "pale", "wrack-heaped", "lonely", "dripping" });

    private static Area BuildChasm() => new ChasmArea(
        displayName: "Chasm Floor",
        contextDescription: "at the bottom of the chasm",
        transitionDescription: "drop down onto the chasm floor",
        descriptions: new() { "The floor of a great crack in the rock, littered with what fell from above, the walls going up out of the light" },
        moods: new[] { "deep", "littered", "walled", "silent" });

    private static Area BuildBonePit() => new PitArea(
        displayName: "Bone Pit",
        contextDescription: "at the bottom of the bone pit",
        transitionDescription: "climb down into the bone pit",
        descriptions: new() { "The floor of a natural shaft heaped with the bones of everything that ever fell in and could not climb out" },
        moods: new[] { "bone-heaped", "foul", "silent", "trapped" });

    private static Area BuildCrystalGrotto() => new GrottoArea(
        displayName: "Crystal Grotto",
        contextDescription: "in the crystal grotto",
        transitionDescription: "wriggle out into the crystal grotto",
        descriptions: new() { "A small chamber lined with crystal, every face of it throwing the lantern back in a thousand points" },
        moods: new[] { "glittering", "hushed", "cold", "untouched" });

    // ── Contents ─────────────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        switch (area)
        {
            case EntranceArea:
                p.Add(TerrainSubfactory.BuildRockFace());
                if (_identity == Identity.Mined) p.Add(BuildToolCache());
                break;
            case OverhangArea:
                p.Add(BuildOldHearth());
                p.Add(BuildHandprints());
                break;
            case ShaftArea:
                p.Add(TerrainSubfactory.BuildRockFace());
                if (rng.NextDouble() < 0.5) p.Add(BuildBatRoost());
                break;
            case ChamberArea when area == _oreChamber:
                p.Add(BuildOreVein());
                break;
            case ChamberArea:
                p.Add(TerrainSubfactory.BuildFallenRocks());
                if (rng.NextDouble() < 0.5) p.Add(BuildDripstone());
                break;
            case SeamArea:
                p.Add(BuildCoalDeposit());
                break;
            case TunnelArea:
                p.Add(BuildRubblePile());
                break;
            case AlcoveArea when area == _farBank:
                p.Add(BuildCaveWrack());
                break;
            case AlcoveArea:
                p.Add(TerrainSubfactory.BuildRockFace());
                break;
            case ColonyArea:
                p.Add(BuildBatRoost());
                p.Add(BuildGuanoHeap());
                break;
            case FlowstoneArea:
                p.Add(BuildDripstone());
                p.Add(BuildRimPools());
                break;
            case SinkholeArea:
                p.Add(BuildSinkholeFerns());
                p.Add(TerrainSubfactory.BuildFallenRocks());
                break;
            case DenArea:
                p.Add(BuildBearBed());
                break;
            case PoolArea:
                p.Add(TerrainSubfactory.BuildGorgePool());
                break;
            case RiverArea:
                p.Add(BuildBlackWater());
                break;
            case ChasmArea:
                p.Add(TerrainSubfactory.BuildFallenRocks());
                p.Add(BuildLostRope());
                break;
            case PitArea:
                p.Add(BuildBoneHeap());
                break;
            case GrottoArea:
                p.Add(BuildCrystalWall());
                break;
        }
    }

    private static PointOfInterest BuildToolCache() => new ToolPointOfInterest(
        displayName: "Tool Cache",
        descriptions: new() { "A cache of mining tools propped in the rock" },
        items: new() { new ItemElement(new Pick()), new ItemElement(new Shovel()), new ItemElement(new Rope()) },
        moods: new[] { "ordered", "iron-grey", "soot-marked" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    private static PointOfInterest BuildBatRoost() => new RoostPointOfInterest(
        displayName: "Bat Roost",
        descriptions: new() { "A high hollow in the rock alive with the wing-flutter of bats" },
        moods: new[] { "high", "rustling", "fetid" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "beast_sense", ["listen"] = "beast_call", ["smell"] = "musk_reading", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildOreVein() => new OrePointOfInterest(
        displayName: "Ore Vein",
        descriptions: new() { "A bright streak of iron ore exposed by recent picking" },
        items: new() { new ItemElement(new IronOre()), new ItemElement(new IronOre()), new ItemElement(new IronOre()) },
        moods: new[] { "bright", "iron-red", "fresh-picked" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildCoalDeposit() => new CoalPointOfInterest(
        displayName: "Coal Seam Deposit",
        descriptions: new() { "A dense seam of coal, freshly worked at one end" },
        items: new() { new ItemElement(new Coal()), new ItemElement(new Coal()) },
        moods: new[] { "black", "glittering", "soot-stained" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    private static PointOfInterest BuildRubblePile() => new RubblePointOfInterest(
        displayName: "Rubble Pile",
        descriptions: new() { "A heap of broken stone where the roof came down" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Flint()) },
        moods: new[] { "loose", "treacherous", "dead-end" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "speleology" } };

    private static PointOfInterest BuildOldHearth() => new HearthPointOfInterest(
        displayName: "Old Hearth",
        descriptions: new() { "A ring of fire-cracked stones on the shelter floor, the ash under it trodden hard and very old" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Bone()) },
        moods: new[] { "fire-cracked", "ancient", "cold", "ringed" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "hearthlonging" } };

    private static PointOfInterest BuildHandprints() => new PetroglyphPointOfInterest(
        displayName: "Ochre Handprints",
        descriptions: new() { "Hands outlined in red ochre on the shelter wall, dozens of them, some no bigger than a child's" },
        items: new() { new ItemElement(new Ochre()) },
        moods: new[] { "red", "small", "reaching", "haunting" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "superstition" } };

    private static PointOfInterest BuildGuanoHeap() => new MoundPointOfInterest(
        displayName: "Guano Heap",
        descriptions: new() { "A soft black mound of droppings under the colony, waist-deep in places, crawling with beetles" },
        items: new() { new ItemElement(new Guano()), new ItemElement(new Guano()), new ItemElement(new Bone()) },
        moods: new[] { "soft", "crawling", "reeking", "warm" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "dunging", ["smell"] = "taint_sense" } };

    private static PointOfInterest BuildDripstone() => new CrystalPointOfInterest(
        displayName: "Dripstone",
        descriptions: new() { "Stone grown drop by drop: a forest of fingers hanging from the roof and stumps rising to meet them" },
        items: new() { new ItemElement(new CavePearl()), new ItemElement(new Rock()) },
        moods: new[] { "dripping", "fingered", "ageless", "glistening" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "speleology", ["contemplate"] = "awe", ["listen"] = "hollow_ear" } };

    private static PointOfInterest BuildRimPools() => new PoolPointOfInterest(
        displayName: "Rimstone Pools",
        descriptions: new() { "Terraces of little pools held in white stone rims, each brimming and spilling into the next, a pale pearl in the bottom of one" },
        items: new() { new ItemElement(new CavePearl()) },
        moods: new[] { "terraced", "brimming", "pale", "silent" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "speleology", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildSinkholeFerns() => new HerbPointOfInterest(
        displayName: "Sinkhole Ferns",
        descriptions: new() { "Ferns and moss growing thick in the patch of daylight, the only green thing for a long way in any direction" },
        items: new() { new ItemElement(new Fern()), new ItemElement(new Moss()) },
        moods: new[] { "green", "sunlit", "lush", "lonely" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "forage_lore", ["contemplate"] = "awe", ["smell"] = "petrichor" } };

    private static PointOfInterest BuildBearBed() => new BonesPointOfInterest(
        displayName: "Bear Bed",
        descriptions: new() { "A hollow scraped in the floor and lined with leaves and hair, gnawed bones kicked to its edges" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Hair()) },
        moods: new[] { "scraped", "musky", "gnawed", "warm" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading", ["smell"] = "musk_reading" } };

    private static PointOfInterest BuildBlackWater() => new StreamPointOfInterest(
        displayName: "Black Water",
        descriptions: new() { "The river where it runs out of the rock, smooth and black and fast, a sound like breath in the dark" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "black", "smooth", "fast", "breathing" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "speleology", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildCaveWrack() => new DeadfallPointOfInterest(
        displayName: "Flood Wrack",
        descriptions: new() { "Branches and bones carried in from the daylight and heaped here by floods, all of it bleached grey" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Bone()), new ItemElement(new Twig()) },
        moods: new[] { "bleached", "heaped", "flood-borne", "dry" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "speleology" } };

    private static PointOfInterest BuildLostRope() => new ToolPointOfInterest(
        displayName: "Lost Rope",
        descriptions: new() { "A coil of old rope lying where it fell, its end frayed through on a sharp edge somewhere above" },
        items: new() { new ItemElement(new Rope()) },
        moods: new[] { "frayed", "coiled", "ominous" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildBoneHeap() => new BonesPointOfInterest(
        displayName: "Bone Heap",
        descriptions: new() { "Bones of deer, of wolves, of things nobody has a name for, heaped at the bottom of the shaft under a skin of dripstone" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Skull()), new ItemElement(new Antler()), new ItemElement(new Tooth()) },
        moods: new[] { "heaped", "ancient", "stone-skinned", "silent" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildCrystalWall() => new CrystalPointOfInterest(
        displayName: "Crystal Wall",
        descriptions: new() { "A wall furred with clear crystal spars as long as fingers, cold to the touch and ringing faintly when tapped" },
        items: new() { new ItemElement(new RockCrystal()), new ItemElement(new RockCrystal()) },
        moods: new[] { "glittering", "clear", "ringing", "cold" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "veinsight", ["contemplate"] = "awe", ["listen"] = "hollow_ear" } };

    // ── Life ─────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_entrance is null) return;
        if (_hasMiner)
        {
            var archetype = new MinerArchetype();
            var entity = archetype.Spawn(rng, _entrance.ContextDescription,
                _locationState != null ? _locationState.AffinityFor : null);
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = BuildMinerSchedule();
            TradeAt(entity, _oreStore!);
        }

        if (_identity == Identity.Den) TrySpawnBeast(rng, scene, new BearArchetype(), 0.75);

        TrySpawnCreature(rng, scene, new CaveSpiderArchetype(), 0.25);
        TrySpawnCreature(rng, scene, new RatArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new BatArchetype(), 0.45);
        TrySpawnCreatureIn<ColonyArea>(rng, scene, new BatArchetype(), 0.95);
        TrySpawnCreatureIn<PoolArea>(rng, scene, new OlmArchetype(), 0.45);
        TrySpawnCreatureIn<RiverArea>(rng, scene, new OlmArchetype(), 0.55);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Subterranean, 2, 4);
    }

    private NpcSchedule BuildMinerSchedule()
    {
        var entrance = _entrance!;
        var ore      = _oreChamber ?? _shaft!;
        var shaft    = _shaft!;
        return NpcSchedule.Roaming(new()
        {
            [TimePeriod.Dawn]      = entrance,
            [TimePeriod.Morning]   = ore,
            [TimePeriod.Noon]      = entrance,
            [TimePeriod.Afternoon] = shaft,
            [TimePeriod.Evening]   = _oreStore ?? entrance,   // sacking the day's ore, and selling it
            [TimePeriod.Night]     = entrance,
        });
    }
}
