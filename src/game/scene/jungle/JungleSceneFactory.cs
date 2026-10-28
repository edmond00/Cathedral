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
public class JungleSceneFactory : WildSceneFactory
{
    public JungleSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Lowland, Swamp, Ruined }

    private Identity _identity;
    private Area? _crown, _sandbar, _temple, _falls, _behindFalls;
    private readonly List<Area> _wet = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // A spine: the river runs through, and the jungle hangs off its banks.
        Profile = LayoutProfile.Spine;
        _identity = rng.NextDouble() switch
        {
            < 0.45 => Identity.Lowland,
            < 0.75 => Identity.Swamp,
            _      => Identity.Ruined,
        };
        double W(double low, double swamp, double ruined)
            => _identity switch { Identity.Lowland => low, Identity.Swamp => swamp, _ => ruined };

        // ── The river ────────────────────────────────────────────────────────
        var spine = Draw(rng, new Option[]
        {
            new(BuildRiverbank, 2.0),
            new(BuildMangrove, W(0.4, 3.0, 0.4)),
            new(BuildOxbow,    W(1.5, 1.5, 0.8)),
            new(BuildFarRiverbank, 1.2),
        }, Between(rng, 2, 3));
        _sandbar = rng.NextDouble() < 0.5 ? BuildSandbar() : null;

        // ── What hangs off it: the deep jungle, and one or two more ──────────
        bool hasFalls = rng.NextDouble() < 0.3;
        bool hasEdge = rng.NextDouble() < 0.55;
        bool hasRuins = rng.NextDouble() < W(0.3, 0.2, 1.0);
        bool hasLick = rng.NextDouble() < 0.35;
        if (!hasRuins && !hasLick && !hasFalls) hasEdge = true;
        int others = (hasEdge ? 1 : 0) + (hasRuins ? 1 : 0) + (hasLick ? 1 : 0) + (hasFalls ? 1 : 0);
        var reaches = new List<(string Name, string Description, List<Area> Areas)>
        {
            ("Deep Jungle", "Green dusk at noon, the trunks like pillars, the air too wet to breathe",
                Draw(rng, new Option[]
                {
                    new(BuildUnderstorey, 1.6),
                    new(BuildSwamp, W(0.6, 2.5, 0.6)),
                    new(BuildFigStand, 1.3),
                }, others <= 1 ? 2 : Between(rng, 1, 2))),
        };
        if (hasEdge)
            reaches.Add(("Jungle Edge", "Light breaking through in shafts, everything that can climb climbing toward it",
                Draw(rng, new Option[]
                {
                    new(BuildLightGap, 2.0),
                    new(BuildBambooBrake, W(1.5, 0.6, 1.0)),
                }, others == 3 ? 1 : Between(rng, 1, 2))));
        if (hasRuins)
            reaches.Add(("The Ruins", "Cut stone in the grip of the roots: stairs, walls and a well of the old people",
                Draw(rng, new Option[] { new(BuildRuin, 3.0), new(BuildCenote, 1.2) }, others == 3 ? 1 : Between(rng, 1, 2))));
        if (hasFalls)
            reaches.Add(("The Falls", "White water dropping out of the green into a pool, the air full of spray and noise",
                new List<Area> { (_falls = BuildJungleFalls()) }));
        if (hasLick)
            reaches.Add(("Clay Lick", "A raw red bank where the birds come down to eat the earth",
                new List<Area> { BuildClayLick() }));

        var understorey = reaches.SelectMany(r => r.Areas).OfType<UnderstoreyArea>().FirstOrDefault();
        var ruin = reaches.SelectMany(r => r.Areas).OfType<RuinArea>().FirstOrDefault();
        _temple = ruin != null && rng.NextDouble() < 0.55 ? BuildTempleChamber() : null;
        _behindFalls = _falls != null && rng.NextDouble() < 0.6 ? BuildBehindFalls() : null;

        _wet.AddRange(spine.Concat(reaches.SelectMany(r => r.Areas)).Where(a => a is RiverbankArea or MangroveArea or SwampArea or OxbowArea));

        foreach (var area in spine.Concat(reaches.SelectMany(r => r.Areas))) Populate(area, rng);
        foreach (var area in new[] { _sandbar, _temple, _behindFalls }) if (area != null) Populate(area, rng);

        var spineSection = AddSection(scene, "The River", "A brown river moving slow and wide, the banks a wall of green",
            seed => new NoisyGenerator { Seed = seed, Density = 0.55f }, spine, tier: 0);
        var sections = reaches.Select(r => (r.Areas, Section: AddSection(scene, r.Name, r.Description,
            seed => new NoisyGenerator { Seed = seed, Density = 0.55f }, r.Areas, tier: 1))).ToList();

        string[] moods = { "dripping", "root-crossed", "dim" };
        static string Way(Area a, Area b) =>
            a is RiverbankArea || b is RiverbankArea ? "Bank Path"
            : a is SwampArea or MangroveArea || b is SwampArea or MangroveArea ? "Root Walk"
            : a is RuinArea || b is RuinArea ? "Paved Way"
            : "Game Path";

        WeaveSection(rng, scene, spine, Weave.Line, Way, "along the river", moods);
        foreach (var (areas, _) in sections)
        {
            var walk = areas.Where(a => a is not CenoteArea || areas.Count == 1).ToList();
            WeaveSection(rng, scene, walk, RollWeave(rng), Way, "through the green", moods);
            OutdoorLayout.Link(scene, spine[rng.Next(spine.Count)], walk[0], Way(spine[0], walk[0]), moods, "through the green");
            foreach (var cenote in areas.OfType<CenoteArea>().Where(c => !walk.Contains(c)))
                Climb(scene, cenote, walk[rng.Next(walk.Count)], "Cenote Lip",
                    "The edge of a round well in the rock, roots hanging down its sides into the light far below, clear water at the bottom",
                    new[] { "round", "root-hung", "sheer" });
        }

        if (_sandbar != null)
        {
            AddToSection(scene, spineSection, _sandbar);
            Swim(scene, spine[0], _sandbar, WaterKind.River, "Brown River",
                "The river itself, wide and slow and the colour of milky tea, a bar of pale sand showing in the middle of it",
                new[] { "brown", "wide", "lazy" }, lesson: "plunge");
        }

        // The emergent tree: scaled from the understorey, and its crown sees the whole place.
        if (understorey != null)
        {
            _crown = BuildCrown();
            Populate(_crown, rng);
            AddToSection(scene, sections.First(s => s.Areas.Contains(understorey)).Section, _crown);
            new ScalePointOfInterest(
                understorey, _crown, ScaleKind.Tree, "Emergent Tree",
                new() { "A tree standing head and shoulders above the canopy, its trunk hung with lianas thick enough to climb" },
                new[] { "vast", "liana-hung", "ancient" })
            {
                Senses = SensoryProfile.Examinable,
                VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft" },
            }.AttachTo(scene);
            AddLandscapes(scene, _crown, scene.AllAreas);
        }
        if (_temple != null)
        {
            AddToSection(scene, sections.First(s => s.Areas.Contains(ruin!)).Section, _temple);
            SlipInto(scene, ruin!, _temple, SlipKind.Crevice, "Root-Split Doorway",
                "A doorway half closed by a fig root as thick as a man, a black gap left at one side where the stone has cracked",
                new[] { "root-split", "black", "carved" }, lesson: "potholing");
        }

        if (_behindFalls != null)
        {
            AddToSection(scene, sections.First(s => s.Areas.Contains(_falls!)).Section, _behindFalls);
            Swim(scene, _falls!, _behindFalls, WaterKind.Pond, "Plunge Pool",
                "The churning pool under the fall, green and deep, a dark gap showing behind the curtain of water",
                new[] { "churning", "green", "deep" }, lesson: "plunge");
        }

        // A cenote climbed down into is a top in reverse: no shortcut may walk into it.
        var climbedTo = new List<Area?> { _crown, _sandbar, _temple, _behindFalls };
        climbedTo.AddRange(scene.AllAreas.OfType<CenoteArea>().Where(c => !scene.AreaGraph.ContainsKey(c.Id)));
        Furnish(rng, scene, FurnitureSubfactory.Setting.Jungle, climbedTo.ToArray());

        Console.WriteLine($"JungleSceneFactory: {_identity}, river {spine.Count}, {string.Join("/", reaches.Select(r => r.Name))}");
    }

    private static Area BuildFarRiverbank() => new RiverbankArea(
        displayName: "Bend of the River",
        contextDescription: "at the bend of the river",
        transitionDescription: "follow the bank round the bend",
        descriptions: new() { "Where the river swings wide, the outside bank cut sheer and red, the inside a beach of grey silt" },
        moods: new[] { "swinging", "red-cut", "silty", "wide" });

    private static Area BuildOxbow() => new OxbowArea(
        displayName: "Oxbow Lake",
        contextDescription: "by the oxbow lake",
        transitionDescription: "push through to the oxbow lake",
        descriptions: new() { "A crescent of still black water left by the river, lilies on it, dead trees standing in it white" },
        moods: new[] { "still", "black", "lilied", "eerie" });

    private static Area BuildSandbar() => new SandbarArea(
        displayName: "Sandbar",
        contextDescription: "on the sandbar",
        transitionDescription: "wade up onto the sandbar",
        descriptions: new() { "A long bar of pale sand in the middle of the river, ridged by the current, tracks of birds and something heavier across it" },
        moods: new[] { "pale", "ridged", "exposed", "midstream" });

    private static Area BuildFigStand() => new GroveArea(
        displayName: "Strangler Figs",
        contextDescription: "among the strangler figs",
        transitionDescription: "step in among the strangler figs",
        descriptions: new() { "Figs that grew down round other trees and killed them, standing now as hollow lattices of root, the dead tree long gone from inside" },
        moods: new[] { "latticed", "hollow", "strangling", "cathedral-like" });

    private static Area BuildCenote() => new CenoteArea(
        displayName: "Cenote",
        contextDescription: "at the bottom of the cenote",
        transitionDescription: "climb down into the cenote",
        descriptions: new() { "A round shaft in the rock open to the sky, its floor a pool of water so clear it looks like air, roots hanging all the way down" },
        moods: new[] { "clear", "round", "sacred", "echoing" });

    private static Area BuildJungleFalls() => new WaterfallArea(
        displayName: "Jungle Falls",
        contextDescription: "at the foot of the falls",
        transitionDescription: "follow the roar to the falls",
        descriptions: new() { "A river dropping off a lip of black rock into a pool, the spray drifting through the trees and every leaf dripping" },
        moods: new[] { "roaring", "spray-soaked", "green", "loud" });

    private static Area BuildBehindFalls() => new GrottoArea(
        displayName: "Behind the Falls",
        contextDescription: "behind the falls",
        transitionDescription: "haul yourself out behind the falling water",
        descriptions: new() { "A dripping hollow behind the curtain of water, swifts nesting in its roof, the world outside a roaring green blur" },
        moods: new[] { "hidden", "roaring", "dripping", "dim" });

    private static Area BuildClayLick() => new LickArea(
        displayName: "Clay Lick",
        contextDescription: "at the clay lick",
        transitionDescription: "come out at the clay lick",
        descriptions: new() { "A raw cliff of red clay over the water, pocked with beak-holes, the trees round it full of screaming colour" },
        moods: new[] { "red", "screaming", "pocked", "crowded" });

    private static Area BuildTempleChamber() => new ChamberArea(
        displayName: "Temple Chamber",
        contextDescription: "inside the temple",
        transitionDescription: "squeeze into the temple",
        descriptions: new() { "A dark room of cut stone inside the ruin, the walls carved with figures, roots coming through the roof" },
        moods: new[] { "carved", "dark", "root-pierced", "watchful" });

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
            case OxbowArea:
                area.PointsOfInterest.Add(BuildBlackwater());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildReedBed());
                break;
            case SandbarArea:
                area.PointsOfInterest.Add(BuildSandbarTracks());
                break;
            case GroveArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStranglerFig());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStranglerFig());
                area.PointsOfInterest.Add(BuildLianas());
                break;
            case WaterfallArea:
                area.PointsOfInterest.Add(BuildJungleFallsPoi());
                area.PointsOfInterest.Add(BuildOrchids());
                break;
            case GrottoArea:
                area.PointsOfInterest.Add(TerrainSubfactory.BuildMossBank());
                break;
            case CenoteArea:
                area.PointsOfInterest.Add(BuildCenotePool());
                break;
            case LickArea:
                area.PointsOfInterest.Add(BuildClayBank());
                break;
            case ChamberArea:
                area.PointsOfInterest.Add(BuildCarvedWalls());
                area.PointsOfInterest.Add(BuildToppledIdol());
                break;
            case CrownArea:
                area.PointsOfInterest.Add(BuildBromeliad());
                area.PointsOfInterest.Add(BuildOrchids());
                break;
        }
    }

    private static PointOfInterest BuildSandbarTracks() => new MudPointOfInterest(
        displayName: "Sandbar Tracks",
        descriptions: new() { "Bird tracks all over the bar, and down its middle the long drag-mark of something heavy with a tail" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Shell()) },
        moods: new[] { "tracked", "dragged", "pale", "warning" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading" } };

    private static PointOfInterest BuildJungleFallsPoi() => new FallsPointOfInterest(
        displayName: "Falling Water",
        descriptions: new() { "The fall itself, a brown-white column of water thundering into the pool, a rainbow standing in the spray" },
        moods: new[] { "thundering", "brown-white", "rainbowed", "endless" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "plunge", ["listen"] = "water_voice", ["smell"] = "petrichor", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildCenotePool() => new PoolPointOfInterest(
        displayName: "Cenote Pool",
        descriptions: new() { "Water so clear the bottom seems an arm's length down and is ten times that, pale things lying on the floor of it" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Bone()) },
        moods: new[] { "clear", "deep", "sacred", "still" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "plunge", ["contemplate"] = "mysticism" } };

    private static PointOfInterest BuildClayBank() => new CrustPointOfInterest(
        displayName: "Clay Bank",
        descriptions: new() { "The red clay face itself, bitten into hollows by a thousand beaks, a salt bitterness in the air near it" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Salt()) },
        moods: new[] { "bitten", "red", "salty", "noisy" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["listen"] = "birdsong" } };

    private static PointOfInterest BuildCarvedWalls() => new PetroglyphPointOfInterest(
        displayName: "Carved Walls",
        descriptions: new() { "Figures in feathered headdresses carved on every wall, rows of glyphs between them that no one living can read" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "carved", "feathered", "glyphed", "unread" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "decipher", ["contemplate"] = "ruin_sense" } };

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
        TrySpawnCreatureIn<LickArea>(rng, scene, new MacawArchetype(), 0.90);
        TrySpawnCreatureIn<RiverbankArea>(rng, scene, new CaimanArchetype(), 0.35);
        TrySpawnCreatureIn<OxbowArea>(rng, scene, new CaimanArchetype(), 0.45);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Tropical, 3, 6);
    }
}
