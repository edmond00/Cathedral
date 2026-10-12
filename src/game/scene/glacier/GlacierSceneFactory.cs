using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Glacier;

/// <summary>
/// Builds a glacier — what a peak becomes in cold country (see <c>ClimateRule</c>).
///
/// Sections: The Ice (the glacier's surface, its crevasses and, when rolled, an icefall) and Ice
/// Margin (the moraine it has pushed up, and sometimes a grotto melted into its snout).
/// Climb: the rock wall of a nunatak — a peak standing up through the ice — whose top sees the whole
/// place. The poorest of the eight in life, deliberately, as the peak is among the temperate ones.
/// Uninhabited: no settlement stands in cold country yet.
/// </summary>
public class GlacierSceneFactory : ClimateSceneFactory
{
    public GlacierSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _surface, _nunatak;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        bool hasIcefall = rng.NextDouble() < 0.50;
        bool hasGrotto  = rng.NextDouble() < 0.40;

        _surface = BuildSurface();
        var ice = new List<Area> { _surface, BuildCrevasseField() };
        if (hasIcefall) ice.Add(BuildIcefall());
        var margin = new List<Area> { BuildMoraine() };
        if (hasGrotto) margin.Add(BuildGrotto());
        _nunatak = BuildNunatak();

        foreach (var area in ice.Concat(margin).Append(_nunatak)) Populate(area, rng);

        AddSection(scene, "The Ice", "A river of ice, white and blue and dead still, that is moving all the same",
            seed => new WaveGenerator { Seed = seed }, ice.Append(_nunatak));
        AddSection(scene, "Ice Margin", "Rubble and grey silt along the glacier's edge, the ice standing over it in a dirty wall",
            seed => new NoisyGenerator { Seed = seed, Density = 0.75f }, margin);

        // The margin first: the way onto a glacier is up its moraine.
        ConnectChain(scene, margin.Concat(ice).ToList(),
            (a, b) => a is CrevasseArea || b is CrevasseArea ? "Roped Line"
                    : a is MoraineArea || b is MoraineArea ? "Moraine Path"
                    : "Ice Route",
            "over the ice", new[] { "blue", "crunching", "wind-bitten" });

        AddViewpoint(scene, _surface, _nunatak, "Nunatak Wall",
            "A wall of dark rock standing out of the ice, frost-shattered into holds, rime on every one of them",
            new[] { "dark", "rimed", "shattered", "sheer" }, icy: true);

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _nunatak);

        Console.WriteLine($"GlacierSceneFactory: icefall={hasIcefall}, grotto={hasGrotto}, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSurface() => new IceArea(
        displayName: "Glacier Surface",
        contextDescription: "on the glacier",
        transitionDescription: "step out onto the glacier",
        descriptions: new() { "A broad white road of ice running down between the ranges, ribbed, its surface pitted by the sun" },
        moods: new[] { "broad", "pitted", "glaring", "still" });

    private static Area BuildCrevasseField() => new CrevasseArea(
        displayName: "Crevasse Field",
        contextDescription: "in the crevasse field",
        transitionDescription: "edge into the crevasse field",
        descriptions: new() { "Ice split into blue-lipped crevasses as the glacier bends, some open, some bridged with treacherous snow" },
        moods: new[] { "split", "blue-lipped", "treacherous", "silent" });

    private static Area BuildIcefall() => new IcefallArea(
        displayName: "Icefall",
        contextDescription: "below the icefall",
        transitionDescription: "approach the icefall",
        descriptions: new() { "Where the glacier pours over a step in the rock and breaks into towers, which lean, and crack, and fall" },
        moods: new[] { "towering", "cracking", "chaotic", "deadly" });

    private static Area BuildMoraine() => new MoraineArea(
        displayName: "Moraine",
        contextDescription: "on the moraine",
        transitionDescription: "climb onto the moraine",
        descriptions: new() { "A long ridge of rubble and grey grit shoved up by the ice, stones of every kind mixed together" },
        moods: new[] { "rubbled", "grey", "loose", "raw" });

    private static Area BuildGrotto() => new GrottoArea(
        displayName: "Ice Grotto",
        contextDescription: "in the ice grotto",
        transitionDescription: "duck into the ice grotto",
        descriptions: new() { "A cave melted into the glacier's snout, its walls glowing an unearthly blue, water dripping everywhere" },
        moods: new[] { "blue", "glowing", "dripping", "hushed" });

    private static Area BuildNunatak() => new NunatakArea(
        displayName: "Nunatak Top",
        contextDescription: "on top of the nunatak",
        transitionDescription: "climb out onto the nunatak's top",
        descriptions: new() { "A crown of bare rock above a sea of ice, the glaciers flowing past on either side" },
        moods: new[] { "bare", "islanded", "high", "lonely" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case IceArea:
                area.PointsOfInterest.Add(BuildMoulin());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildErratic());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Sun Cups",
                    "A field of hollows scooped in the ice by the sun, each holding a little blue light"));
                break;
            case CrevasseArea:
                area.PointsOfInterest.Add(BuildCrevasse());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildSerac());
                break;
            case IcefallArea:
                area.PointsOfInterest.Add(BuildSerac());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Ice Debris",
                    "A spill of shattered ice blocks below the icefall, fresh-broken and green at the edges"));
                break;
            case MoraineArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildErratic());
                area.PointsOfInterest.Add(BuildSiltBank());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(BuildIceRemains());
                break;
            case GrottoArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildIceSculpture("Blue Ice Walls",
                    "Walls of ice so old and pressed it has gone the blue of deep water, bubbles of ancient air trapped in it"));
                area.PointsOfInterest.Add(BuildMeltPool());
                break;
            case NunatakArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildCairn());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
        }
    }

    private static PointOfInterest BuildMoulin() => new MoulinPointOfInterest(
        displayName: "Moulin",
        descriptions: new() { "A shaft in the ice where a meltwater stream pours down and vanishes, roaring, into the glacier's heart" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "roaring", "bottomless", "blue", "hungry" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "water_voice", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildCrevasse() => new CrevassePointOfInterest(
        displayName: "Crevasse",
        descriptions: new() { "A crack in the ice a stride across, its walls going down from white to blue to black" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new IceShard()) },
        moods: new[] { "deep", "blue-black", "silent", "patient" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "survivalism", ["listen"] = "hollow_ear" } };

    private static PointOfInterest BuildSerac() => new SeracPointOfInterest(
        displayName: "Seracs",
        descriptions: new() { "Towers of ice the height of a church, leaning at angles that cannot last, cracking in the sun" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "leaning", "towering", "cracking", "beautiful" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["contemplate"] = "awe", ["listen"] = "weather_ear" } };

    private static PointOfInterest BuildSiltBank() => new MudPointOfInterest(
        displayName: "Silt Bank",
        descriptions: new() { "A bank of grey glacier silt, fine as flour, washed out of the ice by its melt" },
        items: new() { new ItemElement(new RockFlour()), new ItemElement(new Clay()) },
        moods: new[] { "grey", "fine", "wet", "cold" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage" } };

    private static PointOfInterest BuildIceRemains() => new BonesPointOfInterest(
        displayName: "Ice-Locked Remains",
        descriptions: new() { "The bones of some great antlered beast melting out of the moraine, brown with age, the ice only now letting go" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Antler()), new ItemElement(new Skull()) },
        moods: new[] { "ancient", "brown", "emerging", "uncanny" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "vanitas" } };

    private static PointOfInterest BuildMeltPool() => new PoolPointOfInterest(
        displayName: "Melt Pool",
        descriptions: new() { "A pool of meltwater on the grotto floor, milky with ground stone, so cold it aches to look at" },
        items: new() { new ItemElement(new RockFlour()), new ItemElement(new IceShard()) },
        moods: new[] { "milky", "aching-cold", "still", "dripped-into" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["listen"] = "water_voice" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new SnowLeopardArchetype(), 0.15);
        TrySpawnBeast(rng, scene, new WhiteWolfArchetype(),   0.15);

        TrySpawnCreature(rng, scene, new RavenArchetype(),     0.45);
        TrySpawnCreature(rng, scene, new PtarmiganArchetype(), 0.40);
        TrySpawnCreature(rng, scene, new SnowyOwlArchetype(),  0.25);
        TrySpawnCreature(rng, scene, new IceWormArchetype(),   0.60, new[] { _surface! });

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 1, 3);
    }
}
