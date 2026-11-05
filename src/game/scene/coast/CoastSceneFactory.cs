using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Coast;

/// <summary>
/// Builds a stretch of coast, laid out as the land is: a spine.
///
/// <para><b>The strand</b> (tier 0) is the spine — three or four of sandy beach, rocky shore, tide
/// pools, shingle bank and cliff foot, joined in a line as a shore is. Everything else hangs off it
/// (tier 1): two or three of dunes behind the beach, saltmarsh and mudflat, an estuary with a far
/// shore, a headland climbed to with a seabird rookery, a sea cave, an islet across a channel.</para>
///
/// <para><b>Four coasts.</b> The identity roll weights which hang there: a <i>sandy</i> coast runs to
/// dunes and marsh, a <i>rocky</i> one to caves and islets, a <i>cliff</i> coast always has its
/// headland, an <i>estuary</i> its river mouth and flats.</para>
///
/// <para><b>Ways that are not a walk.</b> The headland climb and its view; the channel to an islet and
/// sometimes the sea cave, swum; the river mouth, swum to the far shore.</para>
///
/// <para>A fisherman may work the beach, with a net shed where his catch is salted and sold.</para>
/// </summary>
public class CoastSceneFactory : WildSceneFactory
{
    public CoastSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Sandy, Rocky, Cliff, Estuary }
    private enum FishKind { Herring, Cod, Mackerel }
    private enum Reach { Dunes, Marsh, Estuary, Headland, SeaCave, Islet }

    private Identity _identity;
    private FishKind _fish;
    private bool _hasFisherman;
    private Area? _beach, _rocky, _netShed, _cliffTop;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Profile = LayoutProfile.Spine;
        _identity = (Identity)rng.Next(4);
        _fish = (FishKind)rng.Next(3);
        double W(double sandy, double rocky, double cliff, double estuary) => _identity switch
        {
            Identity.Sandy => sandy, Identity.Rocky => rocky, Identity.Cliff => cliff, _ => estuary,
        };

        // ── The strand ───────────────────────────────────────────────────────
        var strand = Draw(rng, new Option[]
        {
            new(BuildSandyBeach,  W(3.0, 0.8, 0.6, 2.0)),
            new(BuildRockyShore,  W(1.0, 3.0, 2.0, 0.6)),
            new(BuildTidePools,   W(0.8, 2.0, 1.2, 0.3)),
            new(BuildShingleBank, W(1.2, 1.5, 1.0, 1.0)),
            new(BuildCliffFoot,   W(0.3, 1.0, 3.0, 0.1)),
        }, Between(rng, 3, 4));
        _beach = strand.OfType<BeachArea>().FirstOrDefault();
        _rocky = strand.OfType<ShoreArea>().FirstOrDefault();
        _hasFisherman = _beach != null && rng.NextDouble() < 0.35;

        // ── What hangs off it ────────────────────────────────────────────────
        var reachPool = new List<(Reach Kind, double Weight)>
        {
            (Reach.Dunes,    W(3.0, 0.5, 0.3, 1.0)),
            (Reach.Marsh,    W(1.5, 0.3, 0.2, 3.0)),
            (Reach.Estuary,  W(0.5, 0.4, 0.2, 4.0)),
            (Reach.Headland, W(0.7, 2.0, 5.0, 0.3)),
            (Reach.SeaCave,  W(0.5, 2.0, 2.0, 0.2)),
            (Reach.Islet,    W(1.0, 2.0, 1.5, 0.5)),
        };
        var reaches = new List<Reach>();
        for (int want = strand.Count == 3 ? 3 : Between(rng, 2, 3); reaches.Count < want;)
        {
            double total = reachPool.Sum(r => r.Weight), roll = rng.NextDouble() * total;
            int i = 0;
            for (double acc = reachPool[0].Weight; acc < roll && i < reachPool.Count - 1; acc += reachPool[++i].Weight) { }
            reaches.Add(reachPool[i].Kind);
            reachPool.RemoveAt(i);
        }

        foreach (var a in strand) Populate(a, rng);
        AddSection(scene, _identity == Identity.Estuary ? "Estuary Shore" : "The Strand",
            "Where land meets the sea: beach, rocks and the tide-line",
            seed => new RadiantGenerator { Seed = seed }, strand, tier: 0);

        string[] moods = { "salt-bitten", "wind-raked", "open" };
        static string Way(Area a, Area b) => a is BeachArea || b is BeachArea ? "Shore Path" : "Tide-Line";
        WeaveSection(rng, scene, strand, Weave.Line, Way, "along the shore", moods);

        var hidden = new List<Area>();
        foreach (var reach in reaches) BuildReach(rng, scene, reach, strand, hidden);

        if (_hasFisherman)
        {
            foreach (var poi in CampSubfactory.BuildCoastCamp()) _beach!.PointsOfInterest.Add(poi);
            _netShed = CampSubfactory.BuildNetShed();
            AddToSection(scene, scene.Sections[0], _netShed);
            OutdoorLayout.Link(scene, _beach!, _netShed, "Shore Path");
            scene.Stores.Add(_netShed);
            hidden.Add(_netShed);
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Water, hidden.ToArray());

        Console.WriteLine($"CoastSceneFactory: {_identity}, strand {strand.Count}, reaches {string.Join("/", reaches)}, fish={_fish}, fisherman={_hasFisherman}");
    }

    /// <summary>Builds one section hanging off the strand, and its way in.</summary>
    private void BuildReach(Random rng, Scene scene, Reach reach, List<Area> strand, List<Area> hidden)
    {
        Area Anchor(params Type[] prefer)
            => strand.FirstOrDefault(a => prefer.Any(t => t.IsInstanceOfType(a))) ?? strand[rng.Next(strand.Count)];
        string[] moods = { "salt-bitten", "wind-raked", "open" };

        switch (reach)
        {
            case Reach.Dunes:
            {
                var areas = new List<Area> { BuildDunes() };
                if (rng.NextDouble() < 0.5) areas.Add(BuildDuneSlack());
                Finish(scene, rng, "The Dunes", "Hills of sand held together by marram, rolling back from the beach",
                    seed => new NoisyGenerator { Seed = seed, Density = 0.75f }, areas);
                OutdoorLayout.Link(scene, Anchor(typeof(BeachArea)), areas[0], "Dune Path", moods, "into the dunes");
                break;
            }
            case Reach.Marsh:
            {
                var areas = new List<Area> { BuildSaltmarsh() };
                if (rng.NextDouble() < 0.6) areas.Add(BuildMudflat());
                Finish(scene, rng, "Saltmarsh", "Grazing marsh and grey flats cut by creeks, flooded at the springs",
                    seed => new NoisyGenerator { Seed = seed, Density = 0.7f }, areas);
                OutdoorLayout.Link(scene, Anchor(typeof(BeachArea), typeof(ShingleArea)), areas[0], "Sea Wall Path", moods, "out onto the marsh");
                break;
            }
            case Reach.Estuary:
            {
                var flat = BuildEstuaryFlat();
                var areas = new List<Area> { flat };
                Area? far = rng.NextDouble() < 0.6 ? BuildFarShore() : null;
                if (far != null) areas.Add(far);
                Finish(scene, rng, "Estuary", "Where river meets sea, brackish and muddy",
                    seed => new RadiantGenerator { Seed = seed }, areas, link: false);
                OutdoorLayout.Link(scene, Anchor(typeof(BeachArea)), flat, "River Path", moods, "up the river mouth");
                if (far != null)
                {
                    Swim(scene, flat, far, WaterKind.River, "River Mouth",
                        "The river running out to sea, brown and quick over the bar, the far shore a long swim off",
                        new[] { "brown", "quick", "tidal" }, lesson: "plunge");
                    hidden.Add(far);
                }
                break;
            }
            case Reach.Headland:
            {
                var top = _cliffTop = BuildCliffTop();
                var areas = new List<Area> { top };
                Area? rookery = rng.NextDouble() < 0.6 ? BuildRookery() : null;
                if (rookery != null) areas.Add(rookery);
                Finish(scene, rng, "Clifftop", "The headland above the sea, short turf and thrift and the wind off the water",
                    seed => new WaveGenerator { Seed = seed }, areas, link: false);
                var foot = Anchor(typeof(CliffArea), typeof(ShoreArea));
                AddViewpoint(scene, foot, top, "Cliff Face",
                    "A sheer cliff face with hand- and foot-holds, salt-stained from the spray",
                    new[] { "sheer", "wet", "salt-bitten", "vertiginous" });
                if (rookery != null)
                    Climb(scene, rookery, top, "Ledge Path",
                        "A narrow sheep-track going down off the clifftop onto the ledges where the seabirds nest",
                        new[] { "narrow", "guano-white", "screaming" }, lesson: "fowling");
                hidden.Add(top);
                if (rookery != null) hidden.Add(rookery);
                break;
            }
            case Reach.SeaCave:
            {
                var cave = BuildSeaCave();
                Finish(scene, rng, "Sea Cave", "A cave the sea has cut, booming at high water",
                    seed => new RoomsGenerator { Seed = seed }, new List<Area> { cave }, link: false);
                var mouth = Anchor(typeof(CliffArea), typeof(ShoreArea), typeof(PoolArea));
                if (rng.NextDouble() < 0.5)
                {
                    Swim(scene, mouth, cave, WaterKind.Cove, "Cave Channel",
                        "A channel of green water going in under the cliff, the cave mouth a dark arch at the end of it",
                        new[] { "green", "surging", "arched" }, lesson: "plunge");
                    hidden.Add(cave);
                }
                else
                    OutdoorLayout.Link(scene, mouth, cave, "Low-Tide Way", moods, "in under the cliff");
                break;
            }
            case Reach.Islet:
            {
                var islet = BuildIslet();
                Finish(scene, rng, "Off Shore", "A rock in the sea, out of reach of anyone who will not swim",
                    seed => new WaveGenerator { Seed = seed }, new List<Area> { islet }, link: false);
                Swim(scene, Anchor(typeof(ShoreArea), typeof(PoolArea)), islet, WaterKind.Cove, "Sound",
                    "The channel between the shore and the rock, the swell running through it and the cold coming up from below",
                    new[] { "swelling", "cold", "racing" }, lesson: "plunge");
                hidden.Add(islet);
                break;
            }
        }
    }

    /// <summary>Populates, registers and (unless <paramref name="link"/> is false) weaves one reach section.</summary>
    private void Finish(Scene scene, Random rng, string name, string description,
                        Func<int, Cathedral.Fight.IFightAreaGenerator> generator, List<Area> areas, bool link = true)
    {
        foreach (var a in areas) Populate(a, rng);
        AddSection(scene, name, description, generator, areas, tier: 1);
        if (link)
            WeaveSection(rng, scene, areas, Weave.Line, (_, _) => "Track", "behind the shore", new[] { "sandy", "winding" });
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSandyBeach() => new BeachArea(
        displayName: "Sandy Beach",
        contextDescription: "on the sandy beach",
        transitionDescription: "step onto the sandy beach",
        descriptions: new() { "A long stretch of pale sand, the tide-line marked by weed and shell" },
        moods: new[] { "open", "bright", "windswept", "salt-scented" });

    private static Area BuildRockyShore() => new ShoreArea(
        displayName: "Rocky Shore",
        contextDescription: "on the rocky shore",
        transitionDescription: "pick your way onto the rocky shore",
        descriptions: new() { "Dark rocks slick with weed, the sea working in and out between them" },
        moods: new[] { "slick", "dark", "surging", "barnacled" });

    private static Area BuildTidePools() => new PoolArea(
        displayName: "Tide Pool Zone",
        contextDescription: "among the tide pools",
        transitionDescription: "step among the tide pools",
        descriptions: new() { "A shelf of rock pitted with pools the tide leaves full, each a small world" },
        moods: new[] { "pitted", "glinting", "still", "teeming" });

    private static Area BuildShingleBank() => new ShingleArea(
        displayName: "Shingle Bank",
        contextDescription: "on the shingle bank",
        transitionDescription: "crunch up onto the shingle bank",
        descriptions: new() { "A steep bank of rounded stones thrown up by storms, rattling and dragging as each wave goes back" },
        moods: new[] { "rattling", "grey", "steep", "restless" });

    private static Area BuildCliffFoot() => new CliffArea(
        displayName: "Cliff Base",
        contextDescription: "at the foot of the cliff",
        transitionDescription: "make your way to the cliff base",
        descriptions: new() { "Boulders and wrack at the foot of a cliff that goes up out of sight, the sea booming in the gaps" },
        moods: new[] { "towering", "booming", "spray-soaked", "shadowed" });

    private static Area BuildCliffTop() => new CliffArea(
        displayName: "Cliff Top",
        contextDescription: "on the cliff top",
        transitionDescription: "pull yourself over the cliff edge",
        descriptions: new() { "Short turf and thrift on the headland, the sea far below and the wind straight off it" },
        moods: new[] { "high", "windswept", "airy", "commanding" });

    private static Area BuildEstuaryFlat() => new EstuaryArea(
        displayName: "Estuary Flat",
        contextDescription: "on the estuary flat",
        transitionDescription: "step onto the estuary flat",
        descriptions: new() { "A wide flat of mud and reed where the river spreads into the sea" },
        moods: new[] { "wide", "brackish", "muddy", "shifting" });

    private static Area BuildFarShore() => new ShoreArea(
        displayName: "Far Shore",
        contextDescription: "on the far shore",
        transitionDescription: "wade out on the far shore",
        descriptions: new() { "The other side of the river mouth, a strip of stones and wrack nobody walks" },
        moods: new[] { "lonely", "wrack-strewn", "quiet", "far" });

    private static Area BuildDunes() => new DuneArea(
        displayName: "Dunes",
        contextDescription: "among the dunes",
        transitionDescription: "climb into the dunes",
        descriptions: new() { "Hills of soft sand behind the beach, held together by marram grass that whips in the wind" },
        moods: new[] { "soft", "whispering", "hollowed", "sheltered" });

    private static Area BuildDuneSlack() => new PondArea(
        displayName: "Dune Slack",
        contextDescription: "in the dune slack",
        transitionDescription: "drop down into the dune slack",
        descriptions: new() { "A damp hollow between the dunes where rainwater pools, green with rush and loud with frogs" },
        moods: new[] { "damp", "green", "sheltered", "croaking" });

    private static Area BuildSaltmarsh() => new SaltmarshArea(
        displayName: "Saltmarsh",
        contextDescription: "on the saltmarsh",
        transitionDescription: "step out onto the saltmarsh",
        descriptions: new() { "Flat green marsh cut into islands by winding creeks, the tide filling them with a hiss" },
        moods: new[] { "flat", "creek-cut", "hissing", "salt-smelling" });

    private static Area BuildMudflat() => new MudflatArea(
        displayName: "Mudflat",
        contextDescription: "on the mudflat",
        transitionDescription: "squelch out onto the mudflat",
        descriptions: new() { "A shining grey flat the ebb leaves bare, pocked with worm-casts and stitched with the tracks of birds" },
        moods: new[] { "shining", "squelching", "grey", "teeming" });

    private static Area BuildSeaCave() => new SeaCaveArea(
        displayName: "Sea Cave",
        contextDescription: "in the sea cave",
        transitionDescription: "go in under the arch of the sea cave",
        descriptions: new() { "A high cave cut into the cliff by the sea, its floor of wet stones, its back lost in a booming dark" },
        moods: new[] { "booming", "dripping", "echoing", "green-lit" });

    private static Area BuildIslet() => new IsletArea(
        displayName: "Islet",
        contextDescription: "on the islet",
        transitionDescription: "haul yourself out onto the islet",
        descriptions: new() { "A hump of rock standing out of the sea, white with birds, the shore a long way back across the water" },
        moods: new[] { "isolated", "white-streaked", "wave-washed", "loud" });

    private static Area BuildRookery() => new RookeryArea(
        displayName: "Rookery Ledges",
        contextDescription: "on the rookery ledges",
        transitionDescription: "edge down onto the rookery ledges",
        descriptions: new() { "Ledge on ledge of nesting seabirds packed wing to wing down the cliff, the noise and reek unbelievable" },
        moods: new[] { "screaming", "reeking", "packed", "dizzying" });

    // ── Contents ─────────────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        switch (area)
        {
            case BeachArea:
                p.Add(BuildDriftwoodPile());
                p.Add(BuildKelpBed());
                if (rng.NextDouble() < 0.5) p.Add(BuildStrandedNet());
                break;
            case ShoreArea:
                p.Add(BuildKelpBed());
                if (rng.NextDouble() < 0.6) p.Add(BuildTidePool());
                p.Add(BuildRockCrevice());
                break;
            case PoolArea:
                p.Add(BuildTidePool());
                p.Add(BuildTidePool());
                break;
            case ShingleArea:
                p.Add(BuildShingleRidge());
                if (rng.NextDouble() < 0.5) p.Add(BuildDriftwoodPile());
                break;
            case CliffArea when area == _cliffTop:
                p.Add(BuildSeabirdNest());
                p.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case CliffArea:
                p.Add(TerrainSubfactory.BuildRockFace());
                p.Add(BuildKelpBed());
                break;
            case EstuaryArea:
                p.Add(BuildMudFlat());
                p.Add(BuildWillowBank());
                break;
            case DuneArea:
                p.Add(BuildMarram());
                if (rng.NextDouble() < 0.6) p.Add(BuildRabbitWarren());
                break;
            case PondArea:
                p.Add(TerrainSubfactory.BuildReedBed());
                break;
            case SaltmarshArea:
                p.Add(BuildSamphireBeds());
                p.Add(BuildTidalCreek());
                break;
            case MudflatArea:
                p.Add(BuildCockleBeds());
                break;
            case SeaCaveArea:
                p.Add(BuildBoomingHollow());
                p.Add(BuildTidePool());
                break;
            case IsletArea:
                p.Add(BuildSealRocks());
                p.Add(BuildGullNests());
                break;
            case RookeryArea:
                p.Add(BuildSeabirdLedges());
                break;
        }
    }

    private PointOfInterest BuildDriftwoodPile() => new DriftwoodPointOfInterest(
        displayName: "Driftwood Pile",
        descriptions: new() { "A heap of bleached driftwood pushed up by storm and tide" },
        items: new() { new ItemElement(new Driftwood()), new ItemElement(new Driftwood()), new ItemElement(new RopeFragment()) },
        moods: new[] { "bleached", "salt-stained", "tangled" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "beachcombing", ["smell"] = "petrichor" } };

    private PointOfInterest BuildKelpBed() => new KelpPointOfInterest(
        displayName: "Kelp Bed",
        descriptions: new() { "A heap of dark kelp washed up on the stones" },
        items: new() { new ItemElement(new Seaweed()), new ItemElement(new Seaweed()) },
        moods: new[] { "slick", "salt-fragrant", "dark" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "forage_lore", ["smell"] = "brine_sense" } };

    private PointOfInterest BuildTidePool() => new PoolPointOfInterest(
        displayName: "Tide Pool",
        descriptions: new() { "A small still pool cupped in the rock, anemones at the bottom" },
        items: new() { new ItemElement(new Crab()), new ItemElement(new Mussel()), new ItemElement(new Shell()), new ItemElement(new Rock()) },
        moods: new[] { "still", "salt-bright", "small" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "anglery", ["listen"] = "water_voice", ["smell"] = "brine_sense" } };

    private static PointOfInterest BuildRockCrevice() => new CrevicePointOfInterest(
        displayName: "Rock Crevice",
        descriptions: new() { "A narrow crevice between rocks, weed-fringed and damp" },
        items: new() { new ItemElement(new Shell()), new ItemElement(new Flint()) },
        moods: new[] { "narrow", "wet", "barnacled" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework", ["listen"] = "hollow_ear" } };

    private PointOfInterest BuildStrandedNet()
    {
        var fishItem = _fish switch
        {
            FishKind.Herring => new ItemElement(new Herring()),
            FishKind.Cod     => new ItemElement(new Cod()),
            _                => new ItemElement(new Mackerel()),
        };
        return new NetPointOfInterest(
            displayName: "Stranded Net",
            descriptions: new() { "A torn fishing net half-buried in sand, a dead fish still tangled in its mesh" },
            items: new() { new ItemElement(new Net()), fishItem },
            moods: new[] { "tangled", "salt-stained", "abandoned" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork" } };
    }

    private static PointOfInterest BuildShingleRidge() => new RockPointOfInterest(
        displayName: "Shingle Ridge",
        descriptions: new() { "The crest of the bank, stones of every colour rounded smooth, and caught among them a glint of something golden" },
        items: new() { new ItemElement(new Flint()), new ItemElement(new Shell()), new ItemElement(new Amber()) },
        moods: new[] { "rounded", "rattling", "glinting", "storm-thrown" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "beachcombing", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildSeabirdNest() => new NestPointOfInterest(
        displayName: "Seabird Nest",
        descriptions: new() { "A jumble of stick and weed lodged on a ledge, eggs glinting in the cup" },
        items: new() { new ItemElement(new Egg()), new ItemElement(new Feather()), new ItemElement(new Feather()) },
        moods: new[] { "high", "salt-stained", "noisy" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "husbandry", ["listen"] = "beast_call" } };

    private static PointOfInterest BuildMudFlat() => new MudPointOfInterest(
        displayName: "Mud Flat",
        descriptions: new() { "A glistening mud flat marked with wading-bird tracks" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Clay()), new ItemElement(new Reed()) },
        moods: new[] { "glistening", "soft", "tidal" }
    ) { Play = new MudPuddlePlay(), Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["smell"] = "taint_sense" } };

    private static PointOfInterest BuildWillowBank() => new WillowPointOfInterest(
        displayName: "Willow Bank",
        descriptions: new() { "A willow leans over the muddy bank, its branches trailing the water" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Bark()) },
        moods: new[] { "weeping", "trailing", "wet" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildMarram() => new TussockPointOfInterest(
        displayName: "Marram Grass",
        descriptions: new() { "Tough grey-green marram in clumps on the dune crests, every blade rolled tight against the wind" },
        items: new() { new ItemElement(new Reed()) },
        moods: new[] { "tough", "whipping", "rolled", "grey-green" },
        isNatural: true
    ) { Play = new GrassWhistlePlay(), Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "forage_lore", ["listen"] = "wind_reading" } };

    private static PointOfInterest BuildRabbitWarren() => new BurrowPointOfInterest(
        displayName: "Rabbit Warren",
        descriptions: new() { "Burrow mouths all over a dune face, the sand fanned out below each, droppings everywhere" },
        items: new() { new ItemElement(new Bone()) },
        moods: new[] { "riddled", "sandy", "busy", "nibbled" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ratcatcher" } };

    private static PointOfInterest BuildSamphireBeds() => new HerbPointOfInterest(
        displayName: "Samphire Beds",
        descriptions: new() { "Glassy green samphire standing thick on the low marsh, crunching underfoot" },
        items: new() { new ItemElement(new Samphire()), new ItemElement(new Samphire()) },
        moods: new[] { "glassy", "crunching", "salt-green", "thick" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "forage_lore", ["smell"] = "brine_sense" } };

    private static PointOfInterest BuildTidalCreek() => new StreamPointOfInterest(
        displayName: "Tidal Creek",
        descriptions: new() { "A creek winding through the marsh between banks of shining mud, full one hour and empty the next" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Worm()) },
        moods: new[] { "winding", "shining", "filling", "muddy" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tidewatching", ["listen"] = "water_voice" } };

    private static PointOfInterest BuildCockleBeds() => new MudPointOfInterest(
        displayName: "Cockle Beds",
        descriptions: new() { "Wet sand and mud just below the surface crowded with cockles, their shells showing in the ripples" },
        items: new() { new ItemElement(new Cockle()), new ItemElement(new Cockle()), new ItemElement(new Worm()) },
        moods: new[] { "rippled", "crowded", "wet", "shining" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "beachcombing" } };

    private static PointOfInterest BuildBoomingHollow() => new HollowPointOfInterest(
        displayName: "Booming Hollow",
        descriptions: new() { "A hollow at the back of the cave where each wave arrives as a thud felt in the chest" },
        items: new() { new ItemElement(new Driftwood()), new ItemElement(new Shell()) },
        moods: new[] { "booming", "dark", "thudding", "wet" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tidewatching", ["contemplate"] = "dread", ["listen"] = "hollow_ear" } };

    private static PointOfInterest BuildSealRocks() => new RockPointOfInterest(
        displayName: "Seal Rocks",
        descriptions: new() { "Flat rocks at the water's edge polished by the bodies that haul out on them" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Seaweed()) },
        moods: new[] { "polished", "weedy", "wave-washed", "musky" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "creature_lore", ["smell"] = "musk_reading" } };

    private static PointOfInterest BuildGullNests() => new NestPointOfInterest(
        displayName: "Gull Nests",
        descriptions: new() { "Rough nests of weed and feather in every hollow of the rock, the gulls rising and screaming at any approach" },
        items: new() { new ItemElement(new Egg()), new ItemElement(new Feather()) },
        moods: new[] { "screaming", "untidy", "guarded", "white-streaked" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "fowling", ["listen"] = "beast_call" } };

    private static PointOfInterest BuildSeabirdLedges() => new NestPointOfInterest(
        displayName: "Seabird Ledges",
        descriptions: new() { "Every ledge crammed with sitting birds, eggs balanced on bare rock between their feet, the whole cliff white" },
        items: new() { new ItemElement(new Egg()), new ItemElement(new Egg()), new ItemElement(new Feather()) },
        moods: new[] { "crammed", "white", "screaming", "precarious" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "fowling", ["listen"] = "beast_call", ["smell"] = "taint_sense", ["contemplate"] = "awe" } };

    // ── Life ─────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        if (_hasFisherman && _beach != null)
        {
            var archetype = new FishermanArchetype();
            var entity = archetype.Spawn(rng, _beach.ContextDescription,
                _locationState != null ? _locationState.AffinityFor : null);
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            // Evening in the net shed, salting down the catch — and the one time it can be bought.
            scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = _beach,
                [TimePeriod.Morning]   = null,
                [TimePeriod.Noon]      = null,
                [TimePeriod.Afternoon] = _rocky ?? _beach,
                [TimePeriod.Evening]   = _netShed ?? _beach,
                [TimePeriod.Night]     = _beach,
            });
            if (_netShed != null) TradeAt(entity, _netShed);
        }

        TrySpawnCreature(rng, scene, new SeagullArchetype(), 0.95);
        TrySpawnCreature(rng, scene, new SealArchetype(), 0.20);
        TrySpawnCreatureIn<IsletArea>(rng, scene, new SealArchetype(), 0.80);
        TrySpawnCreature(rng, scene, new HeronArchetype(), 0.35);
        TrySpawnCreature(rng, scene, new CrabArchetype(), 0.50);
        TrySpawnCreatureIn<EstuaryArea>(rng, scene, new SandpiperArchetype(), 0.60);
        TrySpawnCreatureIn<MudflatArea>(rng, scene, new OystercatcherArchetype(), 0.75);
        TrySpawnCreatureIn<ShingleArea>(rng, scene, new OystercatcherArchetype(), 0.35);
        TrySpawnCreatureIn<RookeryArea>(rng, scene, new PuffinArchetype(), 0.85);
        TrySpawnCreatureIn<IsletArea>(rng, scene, new PuffinArchetype(), 0.40);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Waterside, 2, 4);
    }
}
