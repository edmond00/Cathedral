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

namespace Cathedral.Game.Scene.Jungle;

/// <summary>
/// Builds a jungle — what forest becomes in hot country (see <c>ClimateRule</c>).
///
/// Identity: lowland (understorey and river), swamp (mangrove and blackwater) or ruined (the stones
/// of somebody's works, lost under the roots) — gates the areas.
/// Sections: Jungle Edge (where the light gets in) and Deep Jungle (where it does not).
/// Climb: an emergent tree, always present — on the ground a jungle is a green wall in every
/// direction, so the crown is worth more here than anywhere.
/// Uninhabited: no settlement stands in hot country yet.
/// </summary>
public class JungleSceneFactory : ClimateSceneFactory
{
    public JungleSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Lowland, Swamp, Ruined }

    private Identity _identity;
    private Area? _crown;
    private readonly List<Area> _wet = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        _identity = rng.NextDouble() switch
        {
            < 0.45 => Identity.Lowland,
            < 0.75 => Identity.Swamp,
            _      => Identity.Ruined,
        };

        var edge = new List<Area> { BuildLightGap() };
        var deep = new List<Area> { BuildUnderstorey() };

        switch (_identity)
        {
            case Identity.Lowland:
                edge.Add(BuildRiverbank());
                deep.Add(BuildBambooBrake());
                break;
            case Identity.Swamp:
                edge.Add(BuildMangrove());
                deep.Add(BuildSwamp());
                break;
            default:
                deep.Add(BuildRuin());
                edge.Add(rng.NextDouble() < 0.5 ? BuildRiverbank() : BuildBambooBrake());
                break;
        }
        if (_identity != Identity.Ruined && rng.NextDouble() < 0.2) deep.Add(BuildRuin());
        if (rng.NextDouble() < 0.4) deep.Add(_identity == Identity.Swamp ? BuildBambooBrake() : BuildSwamp());

        _wet.AddRange(edge.Concat(deep).Where(a => a is RiverbankArea or MangroveArea or SwampArea));

        foreach (var area in edge.Concat(deep))
            Populate(area, rng);

        AddSection(scene, "Jungle Edge", "Light breaking through in shafts, everything that can climb climbing toward it",
            seed => new NoisyGenerator { Seed = seed, Density = 0.62f }, edge);
        var deepSection = AddSection(scene, "Deep Jungle", "Green dusk at noon, the trunks like pillars, the air too wet to breathe",
            seed => new NoisyGenerator { Seed = seed, Density = 0.50f }, deep);

        ConnectChain(scene, edge.Concat(deep).ToList(),
            (a, b) => a is RiverbankArea || b is RiverbankArea ? "Bank Path"
                    : a is SwampArea or MangroveArea || b is SwampArea or MangroveArea ? "Root Walk"
                    : a is RuinArea || b is RuinArea ? "Paved Way"
                    : "Game Path",
            "through the green", new[] { "dripping", "root-crossed", "dim" });

        // The emergent tree stands in the understorey and its crown belongs to the same section, as
        // sections must partition the areas.
        _crown = BuildCrown();
        Populate(_crown, rng);
        deepSection.Areas.Add(_crown);
        RegisterAll(scene, _crown);
        new ScalePointOfInterest(
            deep[0], _crown, ScaleKind.Tree, "Emergent Tree",
            new() { "A tree standing head and shoulders above the canopy, its trunk hung with lianas thick enough to climb" },
            new[] { "vast", "liana-hung", "ancient" })
        {
            Senses = SensoryProfile.Examinable,
            VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft" },
        }.AttachTo(scene);
        AddLandscapes(scene, _crown, scene.AllAreas);

        Furnish(rng, scene, FurnitureSubfactory.Setting.Jungle, _crown);

        Console.WriteLine($"JungleSceneFactory: {_identity}, {_allAreas.Count + 1} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildUnderstorey() => new UnderstoreyArea(
        displayName: "Understorey",
        contextDescription: "in the understorey",
        transitionDescription: "push into the understorey",
        descriptions: new() { "Pillars of trunk in green half-dark, the floor bare but for leaves and the roots that cross it" },
        moods: new[] { "dim", "dripping", "humming", "close" });

    private static Area BuildLightGap() => new GapArea(
        displayName: "Light Gap",
        contextDescription: "in a light gap",
        transitionDescription: "step out into the light gap",
        descriptions: new() { "Where a giant fell and tore a hole in the roof, sun pours down onto a riot of young growth" },
        moods: new[] { "bright", "steaming", "tangled", "loud" });

    private static Area BuildBambooBrake() => new ThicketArea(
        displayName: "Bamboo Brake",
        contextDescription: "in the bamboo brake",
        transitionDescription: "slip into the bamboo",
        descriptions: new() { "Bamboo in clumps so dense the light comes through green, the canes knocking together overhead" },
        moods: new[] { "knocking", "green-lit", "dense", "creaking" });

    private static Area BuildRiverbank() => new RiverbankArea(
        displayName: "Riverbank",
        contextDescription: "on the riverbank",
        transitionDescription: "come out onto the riverbank",
        descriptions: new() { "A slick red bank above a brown river moving slow and wide, the far shore a wall of green" },
        moods: new[] { "slick", "open", "brown", "watched" });

    private static Area BuildMangrove() => new MangroveArea(
        displayName: "Mangrove",
        contextDescription: "among the mangroves",
        transitionDescription: "climb into the mangroves",
        descriptions: new() { "Trees standing on stilted roots over grey mud, the water coming and going between them" },
        moods: new[] { "stilted", "muddy", "salt-sour", "ticking" });

    private static Area BuildSwamp() => new SwampArea(
        displayName: "Blackwater Swamp",
        contextDescription: "in the blackwater swamp",
        transitionDescription: "wade into the swamp",
        descriptions: new() { "Water the colour of strong tea under the trees, knee-deep and warm, things moving in it" },
        moods: new[] { "black", "warm", "rotting", "still" });

    private static Area BuildRuin() => new RuinArea(
        displayName: "Overgrown Ruin",
        contextDescription: "in the overgrown ruin",
        transitionDescription: "climb into the ruin",
        descriptions: new() { "Cut stone under the roots: a stair, a wall, a doorway leading nowhere, all of it held in the grip of a fig" },
        moods: new[] { "strangled", "mossed", "silent", "watched" });

    private static Area BuildCrown() => new CrownArea(
        displayName: "Emergent Crown",
        contextDescription: "up in the emergent crown",
        transitionDescription: "haul yourself into the crown",
        descriptions: new() { "Limbs above the roof of the jungle, the green canopy rolling away below like a sea" },
        moods: new[] { "airy", "swaying", "sunstruck", "high" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case UnderstoreyArea:
                area.PointsOfInterest.Add(PickGiant(rng));
                area.PointsOfInterest.Add(BuildLianas());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildMushroomCluster());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildCacaoTree());
                if (rng.NextDouble() < 0.3) area.PointsOfInterest.Add(BuildPitchers());
                break;
            case GapArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildWildBanana());
                area.PointsOfInterest.Add(BuildOrchids());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildUndergrowthPatch());
                break;
            case ThicketArea:
                area.PointsOfInterest.Add(BuildBambooStand());
                area.PointsOfInterest.Add(BuildPepperVine());
                break;
            case RiverbankArea:
                area.PointsOfInterest.Add(BuildBrownRiver());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStranglerFig());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildReedBed());
                break;
            case MangroveArea:
                area.PointsOfInterest.Add(BuildMangroveRoots());
                area.PointsOfInterest.Add(BuildTidalMud());
                break;
            case SwampArea:
                area.PointsOfInterest.Add(BuildBlackwater());
                area.PointsOfInterest.Add(BuildRottingGiant());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildPitchers());
                break;
            case RuinArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStele("green with moss and split by a root"));
                area.PointsOfInterest.Add(BuildToppledIdol());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStranglerFig());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildCopalTree());
                break;
            case CrownArea:
                area.PointsOfInterest.Add(BuildBromeliad());
                area.PointsOfInterest.Add(BuildOrchids());
                break;
        }
    }

    private static PointOfInterest PickGiant(Random rng) => rng.Next(3) switch
    {
        0 => ClimateTerrainSubfactory.BuildKapok(),
        1 => ClimateTerrainSubfactory.BuildMahogany(),
        _ => ClimateTerrainSubfactory.BuildStranglerFig(),
    };

    private static PointOfInterest BuildLianas() => new LianaPointOfInterest(
        displayName: "Hanging Lianas",
        descriptions: new() { "Lianas dropping out of the dark above, thick as cables, some of them looped and knotted on themselves" },
        items: new() { new ItemElement(new Liana()), new ItemElement(new Liana()) },
        moods: new[] { "hanging", "looped", "rope-thick", "swaying" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork" } };

    private static PointOfInterest BuildPitchers() => new PitcherPointOfInterest(
        displayName: "Pitcher Plants",
        descriptions: new() { "Speckled red pitchers hanging from a vine, lids half open, something drowned in each" },
        items: new() { new ItemElement(new Grub()) },
        moods: new[] { "speckled", "lidded", "patient", "sweet-rotten" }
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "taint_sense", ["contemplate"] = "vanitas" } };

    private static PointOfInterest BuildOrchids() => new FlowerPointOfInterest(
        displayName: "Orchids",
        descriptions: new() { "Orchids growing on a bough in a spill of light, waxy and speckled, their scent heavy" },
        items: new() { new ItemElement(new Orchid()), new ItemElement(new Orchid()) },
        moods: new[] { "waxy", "heavy-scented", "speckled", "rare" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "perfumery", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildBambooStand() => new BambooPointOfInterest(
        displayName: "Bamboo Stand",
        descriptions: new() { "A clump of green canes as thick as an arm, jointed, rising out of sight, creaking as they sway" },
        items: new() { new ItemElement(new Bamboo()), new ItemElement(new Bamboo()), new ItemElement(new Twig()) },
        moods: new[] { "creaking", "jointed", "towering", "green" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["listen"] = "timber_ear", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildPepperVine() => new HerbPointOfInterest(
        displayName: "Pepper Vine",
        descriptions: new() { "A vine climbing a cane, hung with strings of small red berries that burn the tongue" },
        items: new() { new ItemElement(new Peppercorn()), new ItemElement(new Peppercorn()) },
        moods: new[] { "climbing", "red-strung", "pungent", "small" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildBrownRiver() => new StreamPointOfInterest(
        displayName: "Brown River",
        descriptions: new() { "The river sliding past the bank, opaque with silt, a log turning slowly in it" },
        items: new() { new ItemElement(new Catfish()), new ItemElement(new Clay()) },
        moods: new[] { "brown", "sliding", "wide", "secretive" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "anglery", ["listen"] = "water_voice", ["smell"] = "taint_sense", ["contemplate"] = "patience" } };

    private static PointOfInterest BuildMangroveRoots() => new TreePointOfInterest(
        displayName: "Mangrove Roots",
        descriptions: new() { "Arched roots like the legs of some vast insect, crusted with oysters and barnacles below the tide-mark" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Mussel()), new ItemElement(new Crab()) },
        moods: new[] { "arched", "crusted", "tidal", "knotted" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tidewatching", ["smell"] = "brine_sense" } };

    private static PointOfInterest BuildTidalMud() => new MudPointOfInterest(
        displayName: "Tidal Mud",
        descriptions: new() { "Grey mud between the roots, ticking and popping as the water draws off it, mudskippers on it" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Clay()) },
        moods: new[] { "grey", "ticking", "sucking", "rank" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "insect_chorus" } };

    private static PointOfInterest BuildBlackwater() => new PoolPointOfInterest(
        displayName: "Blackwater Pool",
        descriptions: new() { "A pool of tea-dark water between buttresses, mirror-still, its depth impossible to judge" },
        items: new() { new ItemElement(new Catfish()), new ItemElement(new Eel()) },
        moods: new[] { "black", "mirror-still", "deep", "warm" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "anglery", ["listen"] = "water_voice", ["smell"] = "taint_sense", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildRottingGiant() => new LogPointOfInterest(
        displayName: "Rotting Giant",
        descriptions: new() { "A fallen tree gone soft as cake, sprouting fungus, a whole wood growing out of its back" },
        items: new() { new ItemElement(new Mushroom()), new ItemElement(new Grub()), new ItemElement(new Log()) },
        moods: new[] { "soft", "fungal", "teeming", "rotting" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "mycology", ["smell"] = "taint_sense" } };

    private static PointOfInterest BuildToppledIdol() => new RubblePointOfInterest(
        displayName: "Toppled Idol",
        descriptions: new() { "A carved figure fallen on its face among the roots, too heavy to turn, one stone hand still raised" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Sandstone()) },
        moods: new[] { "fallen", "carved", "heavy", "reproachful" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "superstition" } };

    private static PointOfInterest BuildCopalTree() => new TreePointOfInterest(
        displayName: "Copal Tree",
        descriptions: new() { "A pale-barked tree weeping resin down its trunk, the drips gone hard and cloudy" },
        items: new() { new ItemElement(new Copal()), new ItemElement(new Copal()), new ItemElement(new Bark()) },
        moods: new[] { "pale", "weeping", "fragrant", "sticky" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "perfumery", ["contemplate"] = "piety" } };

    private static PointOfInterest BuildBromeliad() => new BromeliadPointOfInterest(
        displayName: "Bromeliads",
        descriptions: new() { "Rosettes of stiff leaves on the high boughs, each cupping a little pool with a frog in it" },
        items: new() { new ItemElement(new Orchid()) },
        moods: new[] { "cupped", "brimming", "red-hearted", "alive" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "creature_lore", ["listen"] = "insect_chorus", ["smell"] = "petrichor", ["contemplate"] = "fellow_feeling" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new JaguarArchetype(), 0.35);
        TrySpawnBeast(rng, scene, new TigerArchetype(),  0.15);

        TrySpawnCreature(rng, scene, new MonkeyArchetype(),      0.60);
        TrySpawnCreature(rng, scene, new ParrotArchetype(),      0.60);
        TrySpawnCreature(rng, scene, new ToucanArchetype(),      0.45);
        TrySpawnCreature(rng, scene, new HummingbirdArchetype(), 0.40);
        TrySpawnCreature(rng, scene, new PythonArchetype(),      0.30);
        TrySpawnCreature(rng, scene, new TapirArchetype(),       0.35);
        TrySpawnCreature(rng, scene, new TreeFrogArchetype(),    0.50);
        if (_wet.Count > 0) TrySpawnCreature(rng, scene, new CapybaraArchetype(), 0.45, _wet);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Tropical, 3, 6);
    }
}
