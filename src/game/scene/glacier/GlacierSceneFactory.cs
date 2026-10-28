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
public class GlacierSceneFactory : WildSceneFactory
{
    public GlacierSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _surface, _nunatak, _grotto;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // A spine: the ice flows through, and the ground beside it hangs off it.
        Profile = LayoutProfile.Spine;

        _surface = BuildSurface();
        var ice = new List<Area> { _surface };
        ice.AddRange(Draw(rng, new Option[]
        {
            new(BuildCrevasseField, 2.0),
            new(BuildIcefall, 1.2),
        }, Between(rng, 1, 2)));

        bool hasNeve = rng.NextDouble() < 0.5;
        var neve = !hasNeve ? new List<Area>() : new List<Area> { BuildNeve() };
        if (hasNeve && rng.NextDouble() < 0.5) neve.Add(rng.NextDouble() < 0.5 ? BuildIceCol() : BuildNeveCornice());
        var margin = Draw(rng, new Option[]
        {
            new(BuildMoraine, 2.0),
            new(BuildSnout, 1.5),
            new(BuildMeltLake, 1.2),
            new(BuildOutwash, 1.0),
            new(BuildAblationValley, 1.0),
            new(BuildMoraineCrest, 0.8),
        }, Between(rng, 2, 3));
        _nunatak = rng.NextDouble() < 0.75 ? BuildNunatak() : null;
        var snout = margin.OfType<SnoutArea>().FirstOrDefault();
        var crevasses = ice.OfType<CrevasseArea>().FirstOrDefault();
        var caveMouth = (Area?)snout ?? crevasses;
        _grotto = caveMouth != null && rng.NextDouble() < 0.5 ? BuildGrotto() : null;

        foreach (var area in ice.Concat(neve).Concat(margin)) Populate(area, rng);
        foreach (var area in new[] { _nunatak, _grotto }) if (area != null) Populate(area, rng);

        var iceSection = AddSection(scene, "The Ice", "A river of ice, white and blue and dead still, that is moving all the same",
            seed => new WaveGenerator { Seed = seed }, ice, tier: 0);
        if (neve.Count > 0)
            AddSection(scene, "Névé", "The high snowfield the glacier is fed from, blinding and silent",
                seed => new WaveGenerator { Seed = seed }, neve, tier: 1);
        var marginSection = AddSection(scene, "Ice Margin", "Rubble and grey silt along the glacier's edge, the ice standing over it in a dirty wall",
            seed => new NoisyGenerator { Seed = seed, Density = 0.75f }, margin, tier: 1);

        string[] moods = { "blue", "crunching", "wind-bitten" };
        static string Way(Area a, Area b) =>
            a is CrevasseArea || b is CrevasseArea ? "Roped Line"
            : a is MoraineArea || b is MoraineArea ? "Moraine Path"
            : "Ice Route";

        WeaveSection(rng, scene, ice, Weave.Line, Way, "over the ice", moods);
        WeaveSection(rng, scene, neve, Weave.Line, (_, _) => "Snow Route", "over the snow", moods);
        WeaveSection(rng, scene, margin, RollWeave(rng, Weave.Line, Weave.Hub), Way, "along the margin", moods);
        // The way onto a glacier is up its moraine.
        OutdoorLayout.Link(scene, margin[0], ice[rng.Next(ice.Count)], Way(margin[0], ice[0]), moods, "onto the ice");
        if (neve.Count > 0)
        {
            // The névé is reached over the bergschrund, a snow bridge, or straight up the ice.
            var top = ice[rng.Next(ice.Count)];
            if (rng.NextDouble() < 0.6)
                Cross(scene, top, neve[0], CrossingKind.SnowBridge, "Bergschrund",
                    "The crack where the moving ice pulls away from the snowfield above, bridged by a sag of old snow",
                    new[] { "sagging", "blue", "bottomless" }, "icecraft");
            else
                OutdoorLayout.Link(scene, top, neve[0], "Ice Route", moods, "up onto the névé");
        }

        if (_nunatak != null)
        {
            AddToSection(scene, iceSection, _nunatak);
            AddViewpoint(scene, _surface, _nunatak, "Nunatak Wall",
                "A wall of dark rock standing out of the ice, frost-shattered into holds, rime on every one of them",
                new[] { "dark", "rimed", "shattered", "sheer" }, icy: true);
        }
        if (_grotto != null)
        {
            AddToSection(scene, caveMouth is SnoutArea ? marginSection : iceSection, _grotto);
            SlipInto(scene, caveMouth!, _grotto, SlipKind.Crevice, "Ice Arch",
                "A low black arch in the ice, meltwater running out of it, the cold coming out of it like breath",
                new[] { "low", "black", "dripping" }, lesson: "potholing");
        }
        var lake = margin.OfType<LakeArea>().FirstOrDefault();
        var shoreAcross = margin.FirstOrDefault(a => a != lake && a is not SnoutArea);
        if (lake != null && shoreAcross != null && rng.NextDouble() < 0.6
            && !(scene.AreaGraph.TryGetValue(lake.Id, out var near) && near.Contains(shoreAcross.Id)))
            Swim(scene, lake, shoreAcross, WaterKind.Pond, "Meltwater Lake",
                "The milky lake under the ice, bergs adrift on it, the far shore a swim no one sane would try",
                new[] { "milky", "freezing", "berg-strewn" }, lesson: "plunge");

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _nunatak, _grotto);

        Console.WriteLine($"GlacierSceneFactory: ice {ice.Count}, neve {neve.Count}, margin {margin.Count}, nunatak={_nunatak != null}, grotto={_grotto != null}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildIceCol() => new ColArea(
        displayName: "Snow Saddle",
        contextDescription: "on the ice col",
        transitionDescription: "climb up onto the ice col",
        descriptions: new() { "A saddle of snow between two black peaks at the head of the glacier, the wind pouring through from the far side" },
        moods: new[] { "saddled", "howling", "white", "high" });

    private static Area BuildNeveCornice() => new CorniceArea(
        displayName: "Cornice",
        contextDescription: "near the cornice",
        transitionDescription: "edge out toward the cornice",
        descriptions: new() { "A lip of wind-packed snow curling out over the drop at the edge of the névé, nothing under it but air" },
        moods: new[] { "curling", "overhanging", "treacherous", "white" });

    private static Area BuildAblationValley() => new ValleyArea(
        displayName: "Ablation Valley",
        contextDescription: "in the ablation valley",
        transitionDescription: "drop down into the ablation valley",
        descriptions: new() { "A green trough between the moraine wall and the mountainside, grass and flowers in it, the ice grinding past just out of sight" },
        moods: new[] { "green", "sheltered", "surprising", "flowered" });

    private static Area BuildMoraineCrest() => new RidgeArea(
        displayName: "Lateral Crest",
        contextDescription: "on the moraine crest",
        transitionDescription: "scramble up onto the moraine crest",
        descriptions: new() { "The knife-edge top of the lateral moraine, loose grey rubble falling away to the ice on one side and the valley on the other" },
        moods: new[] { "knife-edged", "loose", "grey", "exposed" });

    private static Area BuildNeve() => new NeveArea(
        displayName: "Névé",
        contextDescription: "on the névé",
        transitionDescription: "climb up onto the névé",
        descriptions: new() { "The high field of old snow above the glacier, smooth and blinding, cornices on the ridges round it" },
        moods: new[] { "blinding", "smooth", "silent", "high" });

    private static Area BuildSnout() => new SnoutArea(
        displayName: "Snout",
        contextDescription: "below the glacier snout",
        transitionDescription: "walk up to the snout",
        descriptions: new() { "The end of the glacier, a grey cliff of ice that calves blocks with a crack like thunder, meltwater roaring out of its foot" },
        moods: new[] { "calving", "roaring", "grey", "thundering" });

    private static Area BuildMeltLake() => new LakeArea(
        displayName: "Meltwater Lake",
        contextDescription: "by the meltwater lake",
        transitionDescription: "come down to the meltwater lake",
        descriptions: new() { "A lake of milky green water dammed by the moraine, small bergs adrift on it, the ice cliff standing at its far end" },
        moods: new[] { "milky", "green", "berg-strewn", "still" });

    private static Area BuildOutwash() => new OutwashArea(
        displayName: "Outwash Plain",
        contextDescription: "on the outwash plain",
        transitionDescription: "walk out onto the outwash",
        descriptions: new() { "A wide flat of grey gravel below the ice, braided with streams that change their course by the hour" },
        moods: new[] { "grey", "braided", "flat", "loud" });


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
            case ColArea:
            case CorniceArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case ValleyArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildFlowerPatch());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildAlpineHerbPatch());
                break;
            case RidgeArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildErratic());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildCairn());
                break;
            case NeveArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                area.PointsOfInterest.Add(BuildNeveSnow());
                break;
            case SnoutArea:
                area.PointsOfInterest.Add(BuildIceWall());
                area.PointsOfInterest.Add(BuildMeltPool());
                break;
            case LakeArea:
                area.PointsOfInterest.Add(BuildLakeIce());
                break;
            case OutwashArea:
                area.PointsOfInterest.Add(BuildBraidedStreams());
                area.PointsOfInterest.Add(BuildSiltBank());
                break;
            case NunatakArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildCairn());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
        }
    }

    private static PointOfInterest BuildNeveSnow() => new CrustPointOfInterest(
        displayName: "Firn",
        descriptions: new() { "Old snow gone granular and hard, layered like the pages of a book where a crack shows its edge" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "granular", "layered", "glaring", "silent" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "icecraft" } };

    private static PointOfInterest BuildIceWall() => new SeracPointOfInterest(
        displayName: "Ice Cliff",
        descriptions: new() { "The glacier's end, a dirty wall of ice banded with grit, water pouring from a black arch at its foot" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new RockFlour()) },
        moods: new[] { "dirty", "banded", "pouring", "calving" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "icecraft", ["contemplate"] = "awe", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildLakeIce() => new PoolPointOfInterest(
        displayName: "Milky Water",
        descriptions: new() { "The lake, milky green with ground rock, small bergs adrift on it, its cold coming off it like a breath" },
        items: new() { new ItemElement(new RockFlour()), new ItemElement(new IceShard()) },
        moods: new[] { "milky", "green", "berg-strewn", "breathless" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "plunge", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildBraidedStreams() => new StreamPointOfInterest(
        displayName: "Braided Streams",
        descriptions: new() { "Meltwater running in a dozen grey channels over the gravel, splitting and joining, changing course by the hour" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new RockFlour()) },
        moods: new[] { "grey", "braided", "shifting", "loud" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fording", ["listen"] = "water_voice" } };

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
        TrySpawnCreatureIn<ValleyArea>(rng, scene, new MarmotArchetype(), 0.70);
        TrySpawnCreatureIn<ColArea>(rng, scene, new SnowFinchArchetype(), 0.50);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 1, 3);
    }
}
