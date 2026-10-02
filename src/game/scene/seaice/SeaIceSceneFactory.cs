using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
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
public class SeaIceSceneFactory : ClimateSceneFactory
{
    public SeaIceSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _berg, _lead;
    private readonly List<Area> _edge = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        bool hasPolynya = rng.NextDouble() < 0.40;
        bool hasShore   = rng.NextDouble() < 0.60;

        var pack = new List<Area> { BuildFloe(), BuildPressureRidge() };
        _lead = BuildLeadEdge();
        if (hasShore) _edge.Add(BuildShoreIce());
        _edge.Add(_lead);
        if (hasPolynya) _edge.Add(BuildPolynya());
        _berg = BuildBergTop();

        foreach (var area in pack.Concat(_edge).Append(_berg)) Populate(area, rng);

        AddSection(scene, "Pack Ice", "Plates of white to every horizon, heaved and broken, the sky low and the same colour",
            seed => new GeometricGenerator { Seed = seed }, pack.Append(_berg));
        AddSection(scene, "Ice Edge", "Where the ice gives out on black water, steaming in the cold",
            seed => new WaveGenerator { Seed = seed }, _edge);

        ConnectChain(scene, pack.Concat(_edge).ToList(),
            (a, b) => a is HummockArea || b is HummockArea ? "Ridge Crossing"
                    : a is LeadArea or PolynyaArea || b is LeadArea or PolynyaArea ? "Edge Track"
                    : "Floe Track",
            "over the ice", new[] { "white", "creaking", "wind-scoured" });

        AddViewpoint(scene, pack[0], _berg, "Iceberg Flank",
            "The flank of an iceberg frozen fast in the floe, blue-white and fluted, steps cut in it by the wind",
            new[] { "blue-white", "glassy", "fluted", "towering" }, icy: true);

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _berg);

        Console.WriteLine($"SeaIceSceneFactory: polynya={hasPolynya}, shore={hasShore}, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

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
            case BergArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Berg Pinnacle",
                    "The berg's highest point, carved by the wind into a fin of blue ice that sings when it blows"));
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
        }
    }

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

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 1, 2);
    }
}
