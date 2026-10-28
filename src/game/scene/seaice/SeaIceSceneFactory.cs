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

namespace Cathedral.Game.Scene.SeaIce;

/// <summary>
/// Builds sea ice — what the coast becomes in cold country, and what the sea beside it becomes
/// where it is colder still (see <c>ClimateRule</c>).
///
/// Sections: Pack Ice (floe, pressure ridge, and an iceberg frozen in among them) and Ice Edge (the
/// shore ice, the lead, sometimes a polynya — the water that does not freeze).
/// Climb: the iceberg's flank, whose top sees the whole place.
/// Uninhabited: no settlement stands in cold country yet.
/// </summary>
public class SeaIceSceneFactory : WildSceneFactory
{
    public SeaIceSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _berg, _lead, _farFloe, _iceCave;
    private readonly List<Area> _edge = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // A spine: the edge of the ice runs along the open water, and the rest of the ice hangs off it.
        Profile = LayoutProfile.Spine;

        _lead = BuildLeadEdge();
        _edge.Add(_lead);
        _edge.AddRange(Draw(rng, new Option[]
        {
            new(BuildPolynya, 1.2),
            new(BuildBrash, 1.2),
        }, Between(rng, 1, 2)));

        var pack = Draw(rng, new Option[]
        {
            new(BuildFloe, 2.0),
            new(BuildPressureRidge, 2.0),
            new(BuildSnowFloe, 1.0),
            new(BuildBlueIce, 0.8),
        }, Between(rng, 2, 3));
        _berg = rng.NextDouble() < 0.75 ? BuildBergTop() : null;
        var ridge = pack.OfType<HummockArea>().FirstOrDefault();
        _iceCave = ridge != null && rng.NextDouble() < 0.45 ? BuildIceCave() : null;

        bool hasFast = rng.NextDouble() < 0.6;
        var fast = !hasFast ? new List<Area>() : Draw(rng, new Option[]
        {
            new(BuildIcefoot, 1.5),
            new(BuildHaulout, 1.5),
            new(BuildShoreIce, 0.6),
        }, Between(rng, 1, 2));
        _farFloe = !hasFast || rng.NextDouble() < 0.4 ? (rng.NextDouble() < 0.4 ? BuildIslet() : BuildFarFloe()) : null;

        foreach (var area in _edge.Concat(pack).Concat(fast)) Populate(area, rng);
        foreach (var area in new[] { _berg, _iceCave, _farFloe }) if (area != null) Populate(area, rng);

        AddSection(scene, "Ice Edge", "Where the ice gives out on black water, steaming in the cold",
            seed => new WaveGenerator { Seed = seed }, _edge, tier: 0);
        var packSection = AddSection(scene, "Pack Ice", "Plates of white to every horizon, heaved and broken, the sky low and the same colour",
            seed => new GeometricGenerator { Seed = seed }, pack, tier: 1);
        if (fast.Count > 0)
            AddSection(scene, "Fast Ice", "Ice frozen fast to the land, rough and tide-cracked, where the seals come up to lie",
                seed => new GeometricGenerator { Seed = seed }, fast, tier: 1);
        if (_farFloe != null)
            AddSection(scene, _farFloe is IsletArea ? "The Islet" : "Across the Lead", "A floe on the far side of the open water, cut off from everything else",
                seed => new GeometricGenerator { Seed = seed }, new[] { _farFloe }, tier: 1);

        string[] moods = { "white", "creaking", "wind-scoured" };
        static string Way(Area a, Area b) =>
            a is HummockArea || b is HummockArea ? "Ridge Crossing"
            : a is LeadArea or PolynyaArea or BrashArea || b is LeadArea or PolynyaArea or BrashArea ? "Edge Track"
            : "Floe Track";

        WeaveSection(rng, scene, _edge, Weave.Line, Way, "along the edge of the ice", moods);
        WeaveSection(rng, scene, pack, Weave.Line, Way, "over the ice", moods);
        WeaveSection(rng, scene, fast, Weave.Line, Way, "over the ice", moods);
        OutdoorLayout.Link(scene, _edge[rng.Next(_edge.Count)], pack[0], Way(_edge[0], pack[0]), moods, "over the ice");
        if (fast.Count > 0)
            OutdoorLayout.Link(scene, _edge[rng.Next(_edge.Count)], fast[0], "Floe Track", moods, "over the ice");

        if (_farFloe != null)
            Swim(scene, _lead, _farFloe, WaterKind.Lead, "Open Lead",
                "A channel of black open water between two sheets of ice, steaming, the far floe a short and terrible swim away",
                new[] { "black", "steaming", "deadly" }, lesson: "plunge");
        if (_berg != null)
        {
            AddToSection(scene, packSection, _berg);
            AddViewpoint(scene, pack[0], _berg, "Iceberg Flank",
                "The flank of an iceberg frozen fast in the floe, blue-white and fluted, steps cut in it by the wind",
                new[] { "blue-white", "glassy", "fluted", "towering" }, icy: true);
        }
        if (_iceCave != null)
        {
            AddToSection(scene, packSection, _iceCave);
            SlipInto(scene, ridge!, _iceCave, SlipKind.Crevice, "Gap in the Ridge",
                "A gap between two up-ended slabs of the pressure ridge, blue light inside where the ice has left a hollow",
                new[] { "blue-lit", "tight", "groaning" }, lesson: "potholing");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _berg, _iceCave, _farFloe);

        Console.WriteLine($"SeaIceSceneFactory: edge {_edge.Count}, pack {pack.Count}, fast {fast.Count}, far={_farFloe != null}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSnowFloe() => new SnowpackArea(
        displayName: "Snow Plain",
        contextDescription: "on the snow-covered floe",
        transitionDescription: "wade out onto the snow-covered floe",
        descriptions: new() { "A floe buried in wind-packed snow, carved into ridges and furrows all lying the same way" },
        moods: new[] { "furrowed", "wind-packed", "white", "featureless" });

    private static Area BuildBlueIce() => new IceArea(
        displayName: "Blue Sheet",
        contextDescription: "on the old ice",
        transitionDescription: "step out onto the old ice",
        descriptions: new() { "Ice that has lasted through summers, scoured bare of snow, blue and hard as glass, pooled with frozen melt" },
        moods: new[] { "blue", "bare", "glassy", "ancient" });

    private static Area BuildIslet() => new IsletArea(
        displayName: "Rocky Islet",
        contextDescription: "on the rocky islet",
        transitionDescription: "drag yourself out onto the islet",
        descriptions: new() { "A hump of black rock standing out of the ice, streaked white by birds, the only land for a day in any direction" },
        moods: new[] { "black", "bird-streaked", "lonely", "loud" });

    private static Area BuildBrash() => new BrashArea(
        displayName: "Brash",
        contextDescription: "at the edge of the brash",
        transitionDescription: "pick your way out toward the brash",
        descriptions: new() { "A field of broken ice lumps the size of barrels churning on the swell, the floe edge rotten where it meets them" },
        moods: new[] { "churning", "rotten-edged", "grinding", "grey" });

    private static Area BuildIcefoot() => new IcefootArea(
        displayName: "Icefoot",
        contextDescription: "on the ice foot",
        transitionDescription: "step onto the ice foot",
        descriptions: new() { "A shelf of ice frozen to the shore, rough and humped where the tide has broken and refrozen it a hundred times" },
        moods: new[] { "humped", "rough", "shore-bound", "groaning" });

    private static Area BuildHaulout() => new HauloutArea(
        displayName: "Seal Grounds",
        contextDescription: "on the seal ice",
        transitionDescription: "creep out onto the seal ice",
        descriptions: new() { "Flat ice pocked with breathing holes, each with a dark slick round it where the seals haul out to lie" },
        moods: new[] { "pocked", "stained", "musky", "watchful" });

    private static Area BuildFarFloe() => new FloeArea(
        displayName: "Drifting Plate",
        contextDescription: "on the far floe",
        transitionDescription: "haul yourself out onto the far floe",
        descriptions: new() { "A floe across the lead, cut off from the rest of the ice, nothing on it but snow and a seal's slick by a hole" },
        moods: new[] { "cut off", "white", "silent", "drifting" });

    private static Area BuildIceCave() => new GrottoArea(
        displayName: "Ice Hollow",
        contextDescription: "inside the pressure ridge",
        transitionDescription: "squeeze into the hollow in the ridge",
        descriptions: new() { "A hollow inside the pressure ridge where the slabs fell leaning on each other, walls of blue ice glowing all round" },
        moods: new[] { "blue", "glowing", "groaning", "hollow" });


    private static Area BuildFloe() => new FloeArea(
        displayName: "Pack Floe",
        contextDescription: "out on the floe",
        transitionDescription: "step out onto the floe",
        descriptions: new() { "A field of sea ice snowed over and wind-scoured, flat for a long way, groaning now and then underfoot" },
        moods: new[] { "flat", "groaning", "white", "endless" });

    private static Area BuildPressureRidge() => new HummockArea(
        displayName: "Pressure Ridge",
        contextDescription: "on the pressure ridge",
        transitionDescription: "climb among the pressure blocks",
        descriptions: new() { "A wall of ice blocks shoved up where two floes ground together, tumbled like a broken city" },
        moods: new[] { "tumbled", "blue-shadowed", "jagged", "grinding" });

    private static Area BuildLeadEdge() => new LeadArea(
        displayName: "Lead Edge",
        contextDescription: "at the edge of the lead",
        transitionDescription: "go out to the edge of the lead",
        descriptions: new() { "The ice ending at a lane of black open water, sea smoke rising off it in the cold" },
        moods: new[] { "black", "smoking", "breathing", "dangerous" });

    private static Area BuildShoreIce() => new ShoreArea(
        displayName: "Landfast Shelf",
        contextDescription: "on the ice foot",
        transitionDescription: "go down onto the ice foot",
        descriptions: new() { "A shelf of ice frozen to the shore, kelp and stones locked into it, the land rising white beyond" },
        moods: new[] { "frozen", "locked", "glittering", "still" });

    private static Area BuildPolynya() => new PolynyaArea(
        displayName: "Polynya",
        contextDescription: "beside the polynya",
        transitionDescription: "come out to the polynya",
        descriptions: new() { "A lake of open water in the middle of the ice that never freezes, loud with birds and breathing seals" },
        moods: new[] { "open", "loud", "steaming", "alive" });

    private static Area BuildBergTop() => new BergArea(
        displayName: "Iceberg Top",
        contextDescription: "on top of the iceberg",
        transitionDescription: "pull yourself onto the iceberg's top",
        descriptions: new() { "The summit of a berg locked in the pack, high above the floes, the frozen sea mapped out below" },
        moods: new[] { "high", "blue", "wind-torn", "commanding" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case FloeArea when area == _farFloe:
                area.PointsOfInterest.Add(BuildBreathingHole());
                area.PointsOfInterest.Add(BuildHaulOut());
                break;
            case FloeArea:
                area.PointsOfInterest.Add(BuildBreathingHole());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildFrozenDriftwood());
                break;
            case HummockArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Pressure Blocks",
                    "Slabs of sea ice stood on end and leaning on each other, green at their broken edges"));
                area.PointsOfInterest.Add(BuildIceCrack());
                break;
            case LeadArea:
                area.PointsOfInterest.Add(BuildLeadWater());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildHaulOut());
                break;
            case ShoreArea:
                area.PointsOfInterest.Add(BuildFrozenKelp());
                area.PointsOfInterest.Add(BuildFrozenDriftwood());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(BuildHaulOut());
                break;
            case PolynyaArea:
                area.PointsOfInterest.Add(BuildPolynyaWater());
                area.PointsOfInterest.Add(BuildBreathingHole());
                break;
            case SnowpackArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                area.PointsOfInterest.Add(BuildBreathingHole());
                break;
            case IceArea:
                area.PointsOfInterest.Add(BuildIceCrack());
                break;
            case IsletArea:
                area.PointsOfInterest.Add(BuildBirdLedge());
                area.PointsOfInterest.Add(BuildFrozenKelp());
                break;
            case BrashArea:
                area.PointsOfInterest.Add(BuildBrashWater());
                break;
            case IcefootArea:
                area.PointsOfInterest.Add(BuildIcefootCracks());
                area.PointsOfInterest.Add(BuildFrozenKelp());
                break;
            case HauloutArea:
                area.PointsOfInterest.Add(BuildHaulOut());
                area.PointsOfInterest.Add(BuildBreathingHole());
                break;
            case GrottoArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Blue Vault",
                    "A vault of blue ice inside the ridge, light coming through its walls as through deep water"));
                break;
            case BergArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Berg Pinnacle",
                    "The berg's highest point, carved by the wind into a fin of blue ice that sings when it blows"));
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
        }
    }

    private static PointOfInterest BuildBirdLedge() => new NestPointOfInterest(
        displayName: "Bird Ledges",
        descriptions: new() { "Ledges of the black rock packed with nesting guillemots, eggs on the bare stone, the noise carrying across the ice" },
        items: new() { new ItemElement(new Egg()), new ItemElement(new Feather()) },
        moods: new[] { "packed", "screaming", "white-streaked", "precarious" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fowling", ["listen"] = "beast_call" } };

    private static PointOfInterest BuildBrashWater() => new PoolPointOfInterest(
        displayName: "Brash",
        descriptions: new() { "A slurry of broken ice lumps churning together on black water, grinding and whispering as the swell moves them" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "churning", "grinding", "black", "whispering" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "icecraft", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildIcefootCracks() => new CrackPointOfInterest(
        displayName: "Tide Cracks",
        descriptions: new() { "Cracks along the ice foot where the tide lifts the sea ice and lets it down, sea water welling up green in them" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "welling", "green", "hinged", "groaning" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "icecraft", ["listen"] = "tidewatching" } };

    private static PointOfInterest BuildBreathingHole() => new HolePointOfInterest(
        displayName: "Breathing Hole",
        descriptions: new() { "A hole the width of a head kept open through the ice, its rim frosted with breath" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "small", "frosted", "breathing", "watched" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "hunt", ["listen"] = "keen_ear" } };

    private static PointOfInterest BuildIceCrack() => new CrackPointOfInterest(
        displayName: "Ice Crack",
        descriptions: new() { "A dark crack running across the floe, slush in it, the ice either side moving very slightly" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "dark", "groaning", "shifting", "ominous" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "survivalism", ["listen"] = "hollow_ear" } };

    private static PointOfInterest BuildFrozenDriftwood() => new DriftwoodPointOfInterest(
        displayName: "Frozen Driftwood",
        descriptions: new() { "A silver log carried from some far forest coast, frozen into the ice to half its depth" },
        items: new() { new ItemElement(new Driftwood()), new ItemElement(new Driftwood()) },
        moods: new[] { "silver", "locked", "far-travelled", "brittle" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["contemplate"] = "wanderlust" } };

    private static PointOfInterest BuildLeadWater() => new PoolPointOfInterest(
        displayName: "Black Water",
        descriptions: new() { "The open water of the lead, black and very still, fish turning silver just under it" },
        items: new() { new ItemElement(new ArcticChar()), new ItemElement(new Cod()), new ItemElement(new Herring()) },
        moods: new[] { "black", "still", "steaming", "deep" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "anglery", ["listen"] = "water_voice", ["smell"] = "brine_sense", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildPolynyaWater() => new PoolPointOfInterest(
        displayName: "Open Water",
        descriptions: new() { "Water that never freezes, upwelling from somewhere warmer, thick with fish and the things that eat them" },
        items: new() { new ItemElement(new Herring()), new ItemElement(new Seaweed()), new ItemElement(new ArcticChar()) },
        moods: new[] { "upwelling", "teeming", "loud", "warm-breathed" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "tidewatching", ["listen"] = "birdsong", ["smell"] = "brine_sense", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildHaulOut() => new BonesPointOfInterest(
        displayName: "Haul-Out Remains",
        descriptions: new() { "Where walrus haul out onto the ice, the bones of one that never left, its tusks still in the skull" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Tusk()), new ItemElement(new Skull()) },
        moods: new[] { "picked", "stained", "frozen", "rank" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["smell"] = "carrion_sense" } };

    private static PointOfInterest BuildFrozenKelp() => new KelpPointOfInterest(
        displayName: "Frozen Kelp",
        descriptions: new() { "Ribbons of kelp locked in the ice foot where the tide left them, brown under the glaze" },
        items: new() { new ItemElement(new Seaweed()), new ItemElement(new Seaweed()) },
        moods: new[] { "glazed", "brown", "locked", "salt-white" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "forage_lore", ["smell"] = "brine_sense" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new WhiteBearArchetype(), 0.40);

        var edge = _edge.ToList();
        TrySpawnCreature(rng, scene, new SealArchetype(),       0.60, edge);
        TrySpawnCreature(rng, scene, new WalrusArchetype(),     0.35, edge);
        TrySpawnCreature(rng, scene, new ArcticFoxArchetype(),  0.40);
        TrySpawnCreature(rng, scene, new SkuaArchetype(),       0.40);
        TrySpawnCreature(rng, scene, new ArcticTernArchetype(), 0.45, edge);
        TrySpawnCreature(rng, scene, new PuffinArchetype(),     0.30, edge);
        TrySpawnCreature(rng, scene, new SeagullArchetype(),    0.40);
        TrySpawnCreatureIn<HauloutArea>(rng, scene, new SealArchetype(), 0.85);
        TrySpawnCreatureIn<LeadArea>(rng, scene, new NarwhalArchetype(), 0.30);
        TrySpawnCreatureIn<PolynyaArea>(rng, scene, new NarwhalArchetype(), 0.40);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 1, 2);
    }
}
