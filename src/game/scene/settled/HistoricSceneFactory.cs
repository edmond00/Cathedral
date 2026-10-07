using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.History;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Settled;

/// <summary>
/// The places history built, standing on the map as locations — one factory, one programme per
/// <see cref="PlaceKind"/>. A lore place uses its kind's programme like any other (a citadel named
/// in the empire's chronicles is still a citadel); what history adds is the <b>name</b>, taken from
/// <see cref="WorldSites"/> when a world is behind the build.
///
/// <list type="bullet">
/// <item><b>Urban</b> — citadel, port, palace, imperial school: a town (<see cref="SettledSceneFactory.BuildTown"/>)
/// with the place's great building on its square. A port's is a custom house, and it has a harbour.</item>
/// <item><b>Great buildings</b> — castle, fortress, temple, imperial temple, commandery, monastery,
/// pyramid: an approach of two or three outdoor areas and an edifice of three to six storeys and thirty
/// to fifty rooms (<see cref="EdificeFactory"/>), self-sufficient, with the people who keep it.</item>
/// <item><b>Open sites</b> — mine, sanctuary, burial field, wreck: worked or holy ground with at most a
/// lodge.</item>
/// </list>
/// A ruined place of any kind is not built here: see <see cref="RuinSceneFactory"/>.
/// </summary>
public sealed class HistoricSceneFactory : SettledSceneFactory
{
    private readonly PlaceKind _kind;
    private readonly List<Area> _outdoors = new();
    private EdificeResult? _edifice;
    private CityPlan? _town;

    public HistoricSceneFactory(PlaceKind kind, string? biome = null, string? sessionPath = null) : base(biome, sessionPath)
        => _kind = kind;

    /// <summary>The kind each historical location key names.</summary>
    public static PlaceKind KindOf(string key) => key switch
    {
        "citadel"         => PlaceKind.Citadel,
        "port"            => PlaceKind.Port,
        "palace"          => PlaceKind.Palace,
        "imperial school" => PlaceKind.ImperialSchool,
        "castle"          => PlaceKind.Castle,
        "fortress"        => PlaceKind.Fortress,
        "temple"          => PlaceKind.Temple,
        "imperial temple" => PlaceKind.ImperialTemple,
        "commandery"      => PlaceKind.Commandery,
        "monastery"       => PlaceKind.Monastery,
        "mine"            => PlaceKind.Mine,
        "sanctuary"       => PlaceKind.Sanctuary,
        "burial field"    => PlaceKind.BurialField,
        "pyramid"         => PlaceKind.Pyramid,
        "wreck"           => PlaceKind.Wreck,
        _ => throw new ArgumentException($"HistoricSceneFactory: '{key}' is not a historical location"),
    };

    protected override string DefaultBiome => _kind switch
    {
        PlaceKind.Port or PlaceKind.Commandery or PlaceKind.Wreck => "coast",
        PlaceKind.Mine or PlaceKind.Monastery                     => "mountain",
        PlaceKind.Pyramid                                         => Glyph.Microworld.BiomeDatabase.Desert,
        _                                                         => "plain",
    };

    /// <summary>The place's own name when history gave it one, else what it is.</summary>
    private string PlaceName => Site?.Place?.Name is { Length: > 0 } n ? Capitalise(n) : Generic;

    private string Generic => _kind switch
    {
        PlaceKind.Citadel        => "The Citadel",
        PlaceKind.Port           => "The Custom House",
        PlaceKind.Palace         => "The Palace",
        PlaceKind.ImperialSchool => "The Imperial School",
        PlaceKind.Castle         => "The Castle",
        PlaceKind.Fortress       => "The Fortress",
        PlaceKind.Temple         => "The Temple",
        PlaceKind.ImperialTemple => "The Imperial Temple",
        PlaceKind.Commandery     => "The Commandery",
        PlaceKind.Monastery      => "The Monastery",
        PlaceKind.Mine           => "The Mine",
        PlaceKind.Sanctuary      => "The Sanctuary",
        PlaceKind.BurialField    => "The Burial Field",
        PlaceKind.Pyramid        => "The Pyramid",
        _                        => "The Wreck",
    };

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    protected override void BuildPlace(Random rng, int locationId, Scene scene)
    {
        switch (_kind)
        {
            case PlaceKind.Citadel:        BuildUrban(rng, scene, 3, harbour: false); break;
            case PlaceKind.Port:           BuildUrban(rng, scene, 2, harbour: true);  break;
            case PlaceKind.Palace:
            case PlaceKind.ImperialSchool: BuildUrban(rng, scene, 1, harbour: false); break;
            case PlaceKind.Mine:           BuildMine(rng, scene);        break;
            case PlaceKind.Sanctuary:      BuildSanctuary(rng, scene);   break;
            case PlaceKind.BurialField:    BuildBurialField(rng, scene); break;
            case PlaceKind.Wreck:          BuildWreck(rng, scene);       break;
            default:                       BuildGreatPlace(rng, scene);  break;
        }
        Console.WriteLine($"HistoricSceneFactory: Built {PlaceName} ({_kind}) — biome={Biome}, {scene.AllAreas.Count} areas");
    }

    // ── Urban: a town round a great building ─────────────────────────────────

    private void BuildUrban(Random rng, Scene scene, int size, bool harbour)
    {
        _town = BuildTown(rng, scene, size, Site?.Place?.Name is { } n ? $"Streets of {n}" : "Town", harbour);
        _outdoors.AddRange(_town.All);
        if (harbour) BuildHarbour(rng, scene);
        BuildEdifice(rng, scene, _town.Squares[0]);
        FinishTown(rng, scene);
    }

    private void BuildHarbour(Random rng, Scene scene)
    {
        var pool = new (Func<Area> Make, int Weight)[]
        {
            (() => Harbour((n, c, t, d, m) => new QuayArea(n, c, t, d, m), "Quay", "A stone quay with ships moored two deep along it, ropes creaking", new[] { "busy", "salt", "creaking" },
                new BollardPointOfInterest("Bollards", new() { "Iron bollards worn bright where the ropes run" }, Items(() => new Rope(), () => new Oakum()), new[] { "worn", "iron" })
                { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "seamanship")) }), 1),
            (() => Harbour((n, c, t, d, m) => new DockArea(n, c, t, d, m), "Dock", "A walled dock where cargo swings ashore on a creaking crane", new[] { "loud", "heaving", "wet" },
                new CranePointOfInterest("Treadwheel Crane", new() { "A wooden crane worked by men walking a great wheel" }, Items(() => new Rope(), () => new Amphora()), new[] { "huge", "creaking" })
                { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "stevedoring"), ("listen", "crowd_murmur")) }), 1),
            (() => Harbour((n, c, t, d, m) => new ShipyardArea(n, c, t, d, m), "Shipyard", "A slipway with a ship's ribs standing bare, shavings everywhere", new[] { "tarry", "noisy", "half-built" },
                new HullPointOfInterest("Hull on the Slip", new() { "A hull half planked, its ribs showing at the bow" }, Items(() => new Plank(), () => new Tar()), new[] { "half-built", "tarry" })
                { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "shipwrighting"), ("smell", "brine_sense")) }), 1),
            (() => Harbour((n, c, t, d, m) => new FishmarketArea(n, c, t, d, m), "Fish Market", "Wet slabs of the morning's catch under a loud crowd of buyers and gulls", new[] { "reeking", "loud", "wet" },
                new StallPointOfInterest("Fish Slabs", new() { "Stone slabs heaped with fish and shellfish" }, Items(() => new Herring(), () => new Mackerel(), () => new SaltFish()), new[] { "glistening", "reeking" })
                { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "appraisal"), ("smell", "brine_sense"), ("listen", "hawking")) }), 1),
            (() => Harbour((n, c, t, d, m) => new RopewalkArea(n, c, t, d, m), "Ropewalk", "A long covered walk where rope is twisted strand by strand", new[] { "long", "dusty", "rhythmic" },
                new CoilPointOfInterest("Rope Coils", new() { "Coils of new rope as thick as an arm" }, Items(() => new Rope(), () => new Rope(), () => new Narrative.World.Items.Thread()), new[] { "new", "heavy" })
                { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "knotwork")) }), 1),
        };
        var harbour = new Section("Harbour", new() { "The waterfront, where the town meets the sea" }, seed => new NoisyGenerator { Seed = seed, Density = 0.85f });
        foreach (var idx in SampleUniqueIndices(rng, pool.Length, rng.Next(3, 5)))
            harbour.Areas.Add(pool[idx].Make());
        scene.Sections.Add(harbour);
        RegisterAll(scene, harbour);
        var streets = _town!.Streets.Count > 0 ? _town.Streets : _town.Squares;
        for (int i = 0; i < harbour.Areas.Count; i++)
        {
            OutdoorLayout.Link(scene, harbour.Areas[i], i == 0 ? streets[rng.Next(streets.Count)] : harbour.Areas[i - 1], "Waterfront");
            _outdoors.Add(harbour.Areas[i]);
        }
    }

    private static Area Harbour(Func<string, string, string, List<string>, string[], Area> ctor,
                                string name, string text, string[] moods, PointOfInterest poi)
    {
        var a = ctor(name, $"on the {name.ToLowerInvariant()}", $"go down to the {name.ToLowerInvariant()}", new() { text }, moods);
        a.PointsOfInterest.Add(poi);
        return a;
    }

    // ── Great buildings ───────────────────────────────────────────────────────

    private void BuildGreatPlace(Random rng, Scene scene)
    {
        var approach = new Section(ApproachName(), new() { ApproachText() }, seed => new NoisyGenerator { Seed = seed, Density = 0.84f });
        foreach (var a in ApproachAreas(rng)) approach.Areas.Add(a);
        scene.Sections.Add(approach);
        RegisterAll(scene, approach);
        _outdoors.AddRange(approach.Areas);
        OutdoorLayout.Connect(scene, _outdoors, LayoutShape.Chain, "Way", rng);

        BuildEdifice(rng, scene, _outdoors[^1]);

        var setting = Biome is "plain" or "coast" ? FurnitureSubfactory.Setting.Settlement : FurnitureSubfactory.Setting.Highland;
        FurnitureSubfactory.AddSitSpots(rng, _outdoors, setting);
        FurnitureSubfactory.AddHidingPlaces(rng, _outdoors, setting);
        FurnitureSubfactory.AddExtractionPoints(rng, _outdoors, setting);
        AddRoofLandscapes(scene);
        AddLandscapes(scene, _edifice!.Battlements, _outdoors);
    }

    private string ApproachName() => _kind switch
    {
        PlaceKind.Castle or PlaceKind.Fortress or PlaceKind.Commandery => "Outer Works",
        PlaceKind.Monastery => "Monastery Grounds",
        PlaceKind.Pyramid   => "Mortuary Precinct",
        _                   => "Temple Precinct",
    };

    private string ApproachText() => _kind switch
    {
        PlaceKind.Castle or PlaceKind.Fortress or PlaceKind.Commandery => "The ditch, the gate and the ground under the walls",
        PlaceKind.Monastery => "The walled grounds of the monastery, its gardens and its cloister",
        PlaceKind.Pyramid   => "The causeway and the walled court at the pyramid's foot",
        _                   => "The forecourt and gardens before the temple doors",
    };

    private IEnumerable<Area> ApproachAreas(Random rng)
    {
        Area Make(Func<string, string, string, List<string>, string[], Area> ctor, string name, string ctx, string text, string[] moods, params PointOfInterest[] pois)
        {
            var a = ctor(name, ctx, $"go to the {name.ToLowerInvariant()}", new() { text }, moods);
            foreach (var p in pois) a.PointsOfInterest.Add(p);
            return a;
        }

        switch (_kind)
        {
            case PlaceKind.Castle:
            case PlaceKind.Fortress:
            case PlaceKind.Commandery:
                yield return Make((n, c, t, d, m) => new WallfootArea(n, c, t, d, m), "Wall Foot", "under the walls",
                    "A strip of trampled ground under the curtain wall, the ditch beside it", new[] { "shadowed", "overlooked", "damp" },
                    new BattlementPointOfInterest("Arrow Slits", new() { "Narrow slits high in the wall, dark and watching" }, null, new[] { "high", "dark" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "fortification")) });
                if (_kind == PlaceKind.Commandery)
                    yield return Make((n, c, t, d, m) => new ShoreArea(n, c, t, d, m), "Sea Wall", "on the sea wall",
                        "A sloped stone wall with the sea breaking against its foot, the star fort's points behind it", new[] { "wind-scoured", "salt", "loud" },
                        new CairnPointOfInterest("Sea Mark", new() { "A tall cairn painted white, a mark for ships" }, Items(() => new Rock()), new[] { "white", "tall" })
                        { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "pilotage")) });
                else
                    yield return Make((n, c, t, d, m) => new OutworksArea(n, c, t, d, m), "Outworks", "among the outworks",
                        "Earth banks and stakes thrown up before the gate", new[] { "staked", "muddy", "angular" },
                        new RubblePointOfInterest("Spent Shot", new() { "Stone balls heaped where they fell in some old siege" }, Items(() => new Rock(), () => new Rock()), new[] { "heavy", "old" })
                        { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "siegecraft")) });
                yield return Make((n, c, t, d, m) => new GatefrontArea(n, c, t, d, m), "Gate", "before the gate",
                    "A drawbridge over the ditch and a gate under a squat gatehouse", new[] { "guarded", "imposing", "shadowed" },
                    new BannerPointOfInterest("Gate Banner", new() { "A banner over the gate in the holder's colours" }, null, new[] { "proud", "faded" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "heraldry"), ("contemplate", "pageantry")) });
                break;
            case PlaceKind.Monastery:
                yield return Make((n, c, t, d, m) => new GardenArea(n, c, t, d, m), "Herb Garden", "in the herb garden",
                    "Beds of herbs in neat squares, bees busy among them", new[] { "fragrant", "ordered", "humming" },
                    new SagePointOfInterest("Sage Bed", new() { "A bed of grey-green sage, a slate label at its end" }, Items(() => new Sage()), new[] { "soft", "grey-green" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) },
                    new ThymePointOfInterest("Thyme Bed", new() { "A low mat of thyme along the path" }, Items(() => new Thyme()), new[] { "low", "fragrant" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "simpling"), ("smell", "apothecary_nose")) },
                    new MintPointOfInterest("Mint Bed", new() { "Mint kept in a sunken pot so it cannot spread" }, Items(() => new Mint()), new[] { "cool", "bright" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "perfumery")) },
                    new ChamomilePointOfInterest("Chamomile Bed", new() { "White-petalled chamomile in a low drift" }, Items(() => new Chamomile()), new[] { "white", "sweet" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "physic"), ("smell", "apothecary_nose")) },
                    new WormwoodPointOfInterest("Wormwood Bed", new() { "Silvered wormwood, bitter even to smell" }, Items(() => new Wormwood()), new[] { "silver", "bitter" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "diagnosis"), ("smell", "taint_sense")) });
                yield return Make((n, c, t, d, m) => new CloisterArea(n, c, t, d, m), "Cloister", "in the cloister walk",
                    "A square of grass with a well, walled by an arcaded walk", new[] { "hushed", "shadowed", "still" },
                    new WellPointOfInterest("Cloister Well", new() { "A well at the centre of the cloister garth" }, Items(() => new WaterDraught()), new[] { "still", "deep" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("listen", "cloister_silence"), ("examine", "drainage")) });
                break;
            case PlaceKind.Pyramid:
                yield return Make((n, c, t, d, m) => new WalkArea(n, c, t, d, m), "Causeway", "on the causeway",
                    "A long paved causeway between leaning stone figures, sand drifted over it", new[] { "long", "sun-struck", "silent" },
                    new StatuePointOfInterest("Causeway Figures", new() { "Stone figures with animal heads lining the causeway, worn faceless" }, Items(() => new Potsherd()), new[] { "worn", "staring" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "archeology"), ("contemplate", "awe")) });
                yield return Make((n, c, t, d, m) => new CourtyardArea(n, c, t, d, m), "Mortuary Court", "in the mortuary court",
                    "A walled court at the pyramid's foot, an altar for offerings at its centre", new[] { "walled", "baking", "solemn" },
                    new AltarPointOfInterest("Offering Table", new() { "A stone table worn smooth, a few dry offerings on it" }, Items(() => new Offering(), () => new Incense()), new[] { "worn", "solemn" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "relic_lore"), ("contemplate", "devotion")) });
                break;
            default:
                yield return Make((n, c, t, d, m) => new GardenArea(n, c, t, d, m), "Temple Garden", "in the temple garden",
                    "A walled garden of clipped trees and a still pool", new[] { "quiet", "green", "still" },
                    new PoolPointOfInterest("Lustral Pool", new() { "A square pool for washing before worship, carp turning in it" }, Items(() => new HolyWater()), new[] { "still", "clear" })
                    { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("examine", "liturgy"), ("contemplate", "meditation")) });
                if (_kind == PlaceKind.ImperialTemple)
                    yield return Make((n, c, t, d, m) => new RuinArea(n, c, t, d, m), "Old Foundations", "among the old foundations",
                        "The broken footings of the native temple the empire built over, left showing on purpose", new[] { "broken", "pointed", "old" },
                        new RubblePointOfInterest("Native Stones", new() { "Carved stones of an older faith, broken and stacked" }, Items(() => new Idol(), () => new Inscription()), new[] { "carved", "broken" })
                        { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "archeology"), ("contemplate", "heresy")) });
                yield return Make((n, c, t, d, m) => new CourtyardArea(n, c, t, d, m), "Forecourt", "in the temple forecourt",
                    "A paved forecourt before the great doors, beggars and pilgrims on its steps", new[] { "open", "crowded", "solemn" },
                    new ShrinePointOfInterest("Wayside Shrine", new() { "A small shrine by the steps, heaped with candle-ends and offerings" }, Items(() => new VotiveCandle(), () => new Offering()), new[] { "cluttered", "warm" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "liturgy"), ("smell", "incensing"), ("contemplate", "reverence")) });
                break;
        }
    }

    // ── The great building itself ────────────────────────────────────────────

    private void BuildEdifice(Random rng, Scene scene, Area approach)
    {
        var roster = Roster(rng);
        var (rooms, noun, publicRooms, sleep, floors, size) = Programme();
        var spec = new EdificeSpec
        {
            Name         = PlaceName,
            Prefix       = PrefixOf(),
            Approach     = approach,
            Material     = _kind is PlaceKind.Pyramid or PlaceKind.Castle or PlaceKind.Fortress or PlaceKind.Commandery or PlaceKind.Citadel
                               ? BuildingMaterial.Stone : RollMaterial(rng),
            FunctionNoun = noun,
            Rooms        = rooms,
            Residents    = roster.Count,
            Sleep        = sleep,
            MinFloors    = floors.Min, MaxFloors = floors.Max,
            MinRooms     = size.Min,   MaxRooms  = size.Max,
            PublicRooms  = publicRooms,
            Furnish      = Furnish,
            Arena        = seed => new RoomsGenerator { Seed = seed },
        };
        _edifice = EdificeFactory.Build(spec, rng);
        RegisterEdifice(scene, _edifice);

        var e = _edifice;
        var table = e.First(EdificeRoom.Refectory) ?? e.First(EdificeRoom.GreatHall);
        var dawn  = e.First(EdificeRoom.Chapel);
        var outside = _outdoors.ToList();
        Crews.Add(new Crew(roster, e.BedAreas, e.Entrance.ContextDescription,
            (i, who, bed, r) =>
            {
                var work = WorkOf(who, e, outside);
                if (work.Count == 0) work = new List<Area> { e.Entrance };
                bool devout = who is PriestArchetype or MonkArchetype;
                return BuildingSchedule.ForResident(bed, work, table, devout ? dawn : null, r);
            },
            new[] { e.Section }));
    }

    private string PrefixOf() => _kind switch
    {
        PlaceKind.Citadel        => "Citadel",
        PlaceKind.Port           => "Custom House",
        PlaceKind.Palace         => "Palace",
        PlaceKind.ImperialSchool => "School",
        PlaceKind.Castle         => "Castle",
        PlaceKind.Fortress       => "Fortress",
        PlaceKind.Temple         => "Temple",
        PlaceKind.ImperialTemple => "Temple",
        PlaceKind.Commandery     => "Commandery",
        PlaceKind.Monastery      => "Abbey",
        _                        => "Pyramid",
    };

    private static EdificeRoomRequest R(EdificeRoom role, int n = 1, EdificeLevel level = EdificeLevel.Any) => new(role, n, level);

    private (IReadOnlyList<EdificeRoomRequest> Rooms, string Noun, HashSet<EdificeRoom> Public, SleepStyle Sleep,
             (int Min, int Max) Floors, (int Min, int Max) Size) Programme()
    {
        var hall = new HashSet<EdificeRoom> { EdificeRoom.Entrance, EdificeRoom.GreatHall };
        var temple = new HashSet<EdificeRoom> { EdificeRoom.Entrance, EdificeRoom.GreatHall, EdificeRoom.Chapel };
        return _kind switch
        {
            PlaceKind.Castle => (new[]
            {
                R(EdificeRoom.GreatHall, 1, EdificeLevel.Ground), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground), R(EdificeRoom.Larder),
                R(EdificeRoom.Guardroom, 1, EdificeLevel.Ground), R(EdificeRoom.Armoury), R(EdificeRoom.Dungeon, 1, EdificeLevel.Below),
                R(EdificeRoom.Solar, 1, EdificeLevel.Top), R(EdificeRoom.Chapel), R(EdificeRoom.Treasury), R(EdificeRoom.Countinghouse),
                R(EdificeRoom.Chamber, 3), R(EdificeRoom.Latrine, 2), R(EdificeRoom.Wardrobe), R(EdificeRoom.Cellar, 1, EdificeLevel.Below),
            }, "castle", hall, SleepStyle.Dormitories, (3, 5), (30, 42)),
            PlaceKind.Fortress => (new[]
            {
                R(EdificeRoom.Guardroom, 2, EdificeLevel.Ground), R(EdificeRoom.Armoury, 2), R(EdificeRoom.Dungeon, 2, EdificeLevel.Below),
                R(EdificeRoom.Refectory, 1, EdificeLevel.Ground), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground), R(EdificeRoom.Store, 3),
                R(EdificeRoom.Workshop), R(EdificeRoom.Chapel), R(EdificeRoom.Chancery), R(EdificeRoom.Latrine, 2), R(EdificeRoom.Cellar, 1, EdificeLevel.Below),
            }, "fortress", new HashSet<EdificeRoom> { EdificeRoom.Entrance }, SleepStyle.Dormitories, (3, 5), (30, 40)),
            PlaceKind.Commandery => (new[]
            {
                R(EdificeRoom.Chancery), R(EdificeRoom.Archive), R(EdificeRoom.Countinghouse), R(EdificeRoom.Guardroom, 2, EdificeLevel.Ground),
                R(EdificeRoom.Armoury), R(EdificeRoom.Chapel), R(EdificeRoom.Refectory, 1, EdificeLevel.Ground), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground),
                R(EdificeRoom.Observatory, 1, EdificeLevel.Top), R(EdificeRoom.Dungeon, 1, EdificeLevel.Below), R(EdificeRoom.Treasury), R(EdificeRoom.Store, 2),
            }, "star fort", new HashSet<EdificeRoom> { EdificeRoom.Entrance, EdificeRoom.Chancery }, SleepStyle.Dormitories, (3, 5), (32, 44)),
            PlaceKind.Temple => (new[]
            {
                R(EdificeRoom.GreatHall, 1, EdificeLevel.Ground), R(EdificeRoom.Chapel, 3), R(EdificeRoom.Crypt, 2, EdificeLevel.Below),
                R(EdificeRoom.Library), R(EdificeRoom.Refectory), R(EdificeRoom.Kitchen), R(EdificeRoom.Treasury), R(EdificeRoom.Chamber, 2),
                R(EdificeRoom.Latrine), R(EdificeRoom.Store),
            }, "temple", temple, SleepStyle.Cells, (3, 4), (30, 38)),
            PlaceKind.ImperialTemple => (new[]
            {
                R(EdificeRoom.GreatHall, 1, EdificeLevel.Ground), R(EdificeRoom.Chapel, 4), R(EdificeRoom.Crypt, 3, EdificeLevel.Below),
                R(EdificeRoom.Library), R(EdificeRoom.Archive), R(EdificeRoom.Scriptorium), R(EdificeRoom.Refectory), R(EdificeRoom.Kitchen),
                R(EdificeRoom.Treasury), R(EdificeRoom.Guardroom), R(EdificeRoom.Chamber, 3), R(EdificeRoom.Latrine, 2), R(EdificeRoom.Bathhouse),
            }, "imperial temple", temple, SleepStyle.Cells, (4, 6), (38, 50)),
            PlaceKind.Monastery => (new[]
            {
                R(EdificeRoom.Chapel, 2), R(EdificeRoom.Refectory, 1, EdificeLevel.Ground), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground),
                R(EdificeRoom.Scriptorium), R(EdificeRoom.Library), R(EdificeRoom.Infirmary), R(EdificeRoom.Brewhouse), R(EdificeRoom.Workshop),
                R(EdificeRoom.Cellar, 1, EdificeLevel.Below), R(EdificeRoom.Crypt, 1, EdificeLevel.Below), R(EdificeRoom.Bathhouse),
                R(EdificeRoom.Latrine, 2), R(EdificeRoom.Laundry), R(EdificeRoom.Larder),
            }, "monastery", new HashSet<EdificeRoom> { EdificeRoom.Entrance, EdificeRoom.Chapel, EdificeRoom.Infirmary }, SleepStyle.Cells, (3, 5), (34, 48)),
            PlaceKind.Pyramid => (new[]
            {
                R(EdificeRoom.Crypt, 4, EdificeLevel.Below), R(EdificeRoom.Treasury, 1, EdificeLevel.Below), R(EdificeRoom.Chapel, 1, EdificeLevel.Top),
                R(EdificeRoom.Chamber, 4), R(EdificeRoom.Store, 2), R(EdificeRoom.Guardroom, 1, EdificeLevel.Ground),
            }, "pyramid", new HashSet<EdificeRoom> { EdificeRoom.Entrance }, SleepStyle.Dormitories, (4, 6), (30, 36)),
            PlaceKind.Palace => (new[]
            {
                R(EdificeRoom.GreatHall, 1, EdificeLevel.Ground), R(EdificeRoom.Solar, 2, EdificeLevel.Upper), R(EdificeRoom.Chapel), R(EdificeRoom.Library),
                R(EdificeRoom.Treasury), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground), R(EdificeRoom.Larder), R(EdificeRoom.Bathhouse, 2),
                R(EdificeRoom.Guardroom, 1, EdificeLevel.Ground), R(EdificeRoom.Armoury), R(EdificeRoom.Chancery), R(EdificeRoom.Archive),
                R(EdificeRoom.Wardrobe, 2), R(EdificeRoom.Chamber, 4), R(EdificeRoom.Countinghouse), R(EdificeRoom.Observatory, 1, EdificeLevel.Top),
                R(EdificeRoom.Dungeon, 1, EdificeLevel.Below),
            }, "palace", hall, SleepStyle.Dormitories, (4, 6), (40, 50)),
            PlaceKind.ImperialSchool => (new[]
            {
                R(EdificeRoom.Lecture, 3), R(EdificeRoom.Classroom, 4), R(EdificeRoom.Library, 2), R(EdificeRoom.Observatory, 1, EdificeLevel.Top),
                R(EdificeRoom.Scriptorium), R(EdificeRoom.Archive), R(EdificeRoom.Refectory, 1, EdificeLevel.Ground), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground),
                R(EdificeRoom.Chapel), R(EdificeRoom.Bathhouse), R(EdificeRoom.Latrine, 2), R(EdificeRoom.Laundry),
            }, "imperial school", new HashSet<EdificeRoom> { EdificeRoom.Entrance, EdificeRoom.Lecture, EdificeRoom.Library }, SleepStyle.Dormitories, (3, 5), (36, 48)),
            PlaceKind.Port => (new[]
            {
                R(EdificeRoom.Countinghouse, 2, EdificeLevel.Ground), R(EdificeRoom.Chancery), R(EdificeRoom.Store, 4), R(EdificeRoom.Guardroom, 1, EdificeLevel.Ground),
                R(EdificeRoom.Cellar, 1, EdificeLevel.Below), R(EdificeRoom.Archive),
            }, "custom house", new HashSet<EdificeRoom> { EdificeRoom.Entrance, EdificeRoom.Countinghouse }, SleepStyle.Dormitories, (3, 3), (30, 32)),
            _ => (new[]   // Citadel
            {
                R(EdificeRoom.GreatHall, 1, EdificeLevel.Ground), R(EdificeRoom.Solar, 1, EdificeLevel.Top), R(EdificeRoom.Chapel),
                R(EdificeRoom.Guardroom, 2, EdificeLevel.Ground), R(EdificeRoom.Armoury, 2), R(EdificeRoom.Dungeon, 1, EdificeLevel.Below),
                R(EdificeRoom.Treasury), R(EdificeRoom.Chancery), R(EdificeRoom.Countinghouse), R(EdificeRoom.Kitchen, 1, EdificeLevel.Ground),
                R(EdificeRoom.Refectory), R(EdificeRoom.Store, 2), R(EdificeRoom.Cellar, 1, EdificeLevel.Below), R(EdificeRoom.Latrine, 2),
            }, "citadel", hall, SleepStyle.Dormitories, (4, 6), (36, 48)),
        };
    }

    private List<NamedNpcArchetype> Roster(Random rng)
    {
        var r = new List<NamedNpcArchetype>();
        void Add(Func<NamedNpcArchetype> make, int min, int max) { for (int i = 0, n = rng.Next(min, max + 1); i < n; i++) r.Add(make()); }
        switch (_kind)
        {
            case PlaceKind.Castle:         r.Add(new LordArchetype()); r.Add(new StewardArchetype()); r.Add(new CaptainArchetype());
                                           Add(() => new GuardArchetype(), 3, 5); Add(() => new ClerkArchetype(), 1, 1); Add(() => new PriestArchetype(), 0, 1); break;
            case PlaceKind.Fortress:       r.Add(new CaptainArchetype()); Add(() => new GuardArchetype(), 6, 9); Add(() => new ClerkArchetype(), 1, 1); break;
            case PlaceKind.Commandery:     r.Add(new CaptainArchetype()); Add(() => new GuardArchetype(), 5, 7); Add(() => new ClerkArchetype(), 2, 2);
                                           Add(() => new PriestArchetype(), 1, 1); Add(() => new ScholarArchetype(), 0, 1); break;
            case PlaceKind.Temple:         r.Add(new PriestArchetype()); Add(() => new PriestArchetype(), 1, 2); Add(() => new MonkArchetype(), 1, 2); r.Add(new GravediggerArchetype()); break;
            case PlaceKind.ImperialTemple: r.Add(new PriestArchetype()); Add(() => new PriestArchetype(), 2, 3); Add(() => new MonkArchetype(), 2, 3);
                                           Add(() => new GuardArchetype(), 2, 2); Add(() => new ClerkArchetype(), 1, 1); break;
            case PlaceKind.Monastery:      r.Add(new PriestArchetype()); Add(() => new MonkArchetype(), 6, 10); break;
            case PlaceKind.Pyramid:        r.Add(new PriestArchetype()); Add(() => new GuardArchetype(), 2, 3); break;
            case PlaceKind.Palace:         r.Add(new LordArchetype()); r.Add(new StewardArchetype()); r.Add(new CaptainArchetype());
                                           Add(() => new GuardArchetype(), 4, 6); Add(() => new ClerkArchetype(), 2, 2); r.Add(new ScholarArchetype()); r.Add(new PriestArchetype()); break;
            case PlaceKind.ImperialSchool: r.Add(new ScholarArchetype()); Add(() => new ScholarArchetype(), 2, 4); Add(() => new ClerkArchetype(), 2, 2); r.Add(new StewardArchetype()); break;
            case PlaceKind.Port:           r.Add(new MerchantArchetype()); Add(() => new ClerkArchetype(), 2, 3); Add(() => new GuardArchetype(), 2, 2); break;
            default:                       r.Add(new LordArchetype()); r.Add(new StewardArchetype()); r.Add(new CaptainArchetype());
                                           Add(() => new GuardArchetype(), 4, 6); Add(() => new ClerkArchetype(), 1, 2); break;
        }
        return r;
    }

    /// <summary>Where each kind of resident spends the working periods of their day.</summary>
    private static List<Area> WorkOf(NamedNpcArchetype who, EdificeResult e, List<Area> outside)
    {
        var rooms = who switch
        {
            LordArchetype      => e.Of(EdificeRoom.Solar, EdificeRoom.GreatHall, EdificeRoom.Chapel).Append(e.Battlements),
            StewardArchetype   => e.Of(EdificeRoom.GreatHall, EdificeRoom.Countinghouse, EdificeRoom.Kitchen, EdificeRoom.Store, EdificeRoom.Larder),
            CaptainArchetype   => e.Of(EdificeRoom.Guardroom, EdificeRoom.Armoury).Append(e.Battlements).Append(e.Entrance),
            GuardArchetype     => e.Of(EdificeRoom.Guardroom, EdificeRoom.Dungeon).Append(e.Battlements).Append(e.Entrance).Concat(outside.Take(2)),
            ClerkArchetype     => e.Of(EdificeRoom.Chancery, EdificeRoom.Archive, EdificeRoom.Countinghouse, EdificeRoom.Scriptorium),
            PriestArchetype    => e.Of(EdificeRoom.Chapel, EdificeRoom.GreatHall, EdificeRoom.Crypt, EdificeRoom.Library),
            MonkArchetype      => e.Of(EdificeRoom.Scriptorium, EdificeRoom.Library, EdificeRoom.Kitchen, EdificeRoom.Infirmary, EdificeRoom.Brewhouse, EdificeRoom.Workshop).Concat(outside),
            ScholarArchetype   => e.Of(EdificeRoom.Lecture, EdificeRoom.Classroom, EdificeRoom.Library, EdificeRoom.Observatory),
            MerchantArchetype  => e.Of(EdificeRoom.Countinghouse, EdificeRoom.Store).Concat(outside.Take(3)),
            GravediggerArchetype => e.Of(EdificeRoom.Crypt).Concat(outside),
            _                  => e.Of(EdificeRoom.GreatHall),
        };
        return rooms.Distinct().ToList();
    }

    /// <summary>What is particular to this kind of great building, on top of the default furniture.</summary>
    private bool Furnish(EdificeRoom role, Area room, Random rng)
    {
        bool lordly = _kind is PlaceKind.Castle or PlaceKind.Palace or PlaceKind.Citadel;
        bool holy   = _kind is PlaceKind.Temple or PlaceKind.ImperialTemple;
        switch (role)
        {
            case EdificeRoom.GreatHall when lordly:
                room.PointsOfInterest.Add(new ThronePointOfInterest("High Seat", new() { "A carved high seat on a dais at the end of the hall" }, null, new[] { "carved", "raised" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "precedence"), ("contemplate", "statecraft")) });
                room.PointsOfInterest.Add(new TapestryPointOfInterest("Hall Tapestries", new() { "Tapestries of old victories hanging the length of the hall" }, Items(() => new DyedCloth()), new[] { "faded", "rich" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "lineage_lore"), ("contemplate", "pageantry")) });
                break;
            case EdificeRoom.GreatHall when holy:
                room.PointsOfInterest.Add(new AltarPointOfInterest("High Altar", new() { "A great altar of stone under a painted canopy" }, Items(() => new VotiveCandle(), () => new Censer()), new[] { "towering", "gilded" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "liturgy"), ("smell", "incensing"), ("contemplate", "devotion")) });
                room.PointsOfInterest.Add(new FontPointOfInterest("Font", new() { "A stone font by the door, the water in it still" }, Items(() => new HolyWater()), new[] { "cold", "still" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "anointing"), ("contemplate", "reverence")) });
                break;
            case EdificeRoom.Crypt when _kind is PlaceKind.Pyramid or PlaceKind.Temple or PlaceKind.ImperialTemple:
                room.PointsOfInterest.Add(new TombPointOfInterest("Sealed Tomb", new() { "A stone sarcophagus carved with the face of whoever lies in it" }, Items(() => new DeathMask(), () => new Scarab()), new[] { "sealed", "carved" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "embalming")) });
                break;
            case EdificeRoom.Chancery when _kind != PlaceKind.Commandery:
                room.PointsOfInterest.Add(new DeskPointOfInterest("Seal Press", new() { "A heavy press for the office seal, the wax still soft on the plate" }, Items(() => new SealingWax(), () => new Writ()), new[] { "heavy", "official" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "forgery")) });
                break;
            case EdificeRoom.Infirmary:
                room.PointsOfInterest.Add(new ShelfPointOfInterest("Physic Cabinet", new() { "A locked cabinet of stoppered jars, some marked with a skull" }, Items(() => new Poultice(), () => new Wormwood()), new[] { "locked", "pungent" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "poisoning"), ("smell", "apothecary_nose")) });
                break;
            case EdificeRoom.Solar:
                room.PointsOfInterest.Add(new TapestryPointOfInterest("Arras", new() { "A heavy hanging over the wall, a hand's breadth out from it, voices carrying from behind" }, null, new[] { "heavy", "muffling" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("listen", "eavesdropping"), ("examine", "intrigue")) });
                break;
            case EdificeRoom.Chapel when holy || _kind == PlaceKind.Monastery:
                room.PointsOfInterest.Add(new CenserPointOfInterest("Hanging Censer", new() { "A bronze censer on chains, still smoking from the last office" }, Items(() => new Incense()), new[] { "smoking", "swaying" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "incensing"), ("smell", "incensing")) });
                if (_kind == PlaceKind.ImperialTemple)
                {
                    room.PointsOfInterest.Add(new IconPointOfInterest("Icon Screen", new() { "A screen of painted icons, gold faces in rows" }, Items(() => new Icon()), new[] { "gilded", "staring" })
                        { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "iconography"), ("contemplate", "hagiography")) });
                    room.PointsOfInterest.Add(new ReliquaryPointOfInterest("Reliquary", new() { "A gilded reliquary behind a grille, a saint's bone inside" }, Items(() => new Relic()), new[] { "gilded", "guarded" })
                        { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "relic_lore"), ("contemplate", "hagiography")) });
                }
                break;
            case EdificeRoom.Battlements when holy || _kind == PlaceKind.Monastery:
                room.PointsOfInterest.Add(new BellPointOfInterest("Great Bell", new() { "A great bell hung in a frame of black timber, its rope dropping away below" }, null, new[] { "huge", "silent" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "bell_ringing"), ("listen", "bell_ringing")) });
                break;
            case EdificeRoom.Guardroom when _kind is PlaceKind.Fortress or PlaceKind.Commandery:
                room.PointsOfInterest.Add(new BunkPointOfInterest("Watch Bunks", new() { "Bunks in tiers where the watch off duty sleeps in its boots, spears racked at the foot" }, Items(() => new WarSpear()), new[] { "tiered", "cramped" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "soldiery")) });
                break;
            case EdificeRoom.Countinghouse when _kind == PlaceKind.Port:
                room.PointsOfInterest.Add(new CounterPointOfInterest("Customs Counter", new() { "A long counter where duties are weighed and paid" }, Items(() => new Scales(), () => new Ledgerbook()), new[] { "worn", "busy" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "assessment")) });
                break;
            case EdificeRoom.Chancery when _kind == PlaceKind.Commandery:
                room.PointsOfInterest.Add(new MapPointOfInterest("Sea Charts", new() { "Charts of the coast pinned across a table, pricked with routes" }, Items(() => new Chart(), () => new Compass()), new[] { "spread", "annotated" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "navigation")) });
                break;
        }
        return false;
    }

    // ── Open sites ────────────────────────────────────────────────────────────

    private Area Open(Func<string, string, string, List<string>, string[], Area> ctor, string name, string ctx, string text, string[] moods, params PointOfInterest[] pois)
    {
        var a = ctor(name, ctx, $"go to the {name.ToLowerInvariant()}", new() { text }, moods);
        foreach (var p in pois) a.PointsOfInterest.Add(p);
        _outdoors.Add(a);
        return a;
    }

    private void FinishOpenSite(Random rng, Scene scene, string sectionName, string text, FurnitureSubfactory.Setting setting)
    {
        var section = new Section(sectionName, new() { text }, seed => new NoisyGenerator { Seed = seed, Density = 0.84f });
        foreach (var a in _outdoors) section.Areas.Add(a);
        scene.Sections.Add(section);
        RegisterAll(scene, section);
        OutdoorLayout.Connect(scene, _outdoors, OutdoorLayout.RollShape(rng), "Path", rng);
        FurnitureSubfactory.AddSitSpots(rng, _outdoors, setting);
        FurnitureSubfactory.AddHidingPlaces(rng, _outdoors, setting);
        FurnitureSubfactory.AddExtractionPoints(rng, _outdoors, setting);
    }

    private void Lodge(Random rng, Scene scene, string name, string noun, List<NamedNpcArchetype> roster)
    {
        var (hall, bunk, beds) = HouseCrew(rng, scene, roster, _outdoors[0], _outdoors, name, noun);
        var work = _outdoors.ToList();
        var owns = bunk != null ? new[] { hall.Section, bunk.Section } : new[] { hall.Section };
        Crews.Add(new Crew(roster, beds, hall.PublicHall.ContextDescription,
            (i, _, bed, r) => i == 0 ? BuildingSchedule.ForWorker(bed, hall.PublicHall, work, r, awayPeriods: 1)
                                     : BuildingSchedule.ForHand(bed, work, r),
            owns, hall.PublicHall));
        AddRoofLandscapes(scene);
    }

    private void BuildMine(Random rng, Scene scene)
    {
        Open((n, c, t, d, m) => new YardArea(n, c, t, d, m), "Pithead", "at the pithead",
            "A trampled yard round the mine mouth, ore heaped in bays and carts standing", new[] { "dusty", "loud", "grey" },
            new OrePointOfInterest("Ore Bays", new() { "Bays of broken ore sorted by grade" }, Items(() => new IronOre(), () => new CopperOre(), () => new Coal()), new[] { "heaped", "heavy" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "veinsight")) });
        Open((n, c, t, d, m) => new ShaftArea(n, c, t, d, m), "Adit", "in the adit",
            "A timbered tunnel driven level into the hill, lamps hung on the props", new[] { "dark", "dripping", "close" },
            new LanternPointOfInterest("Prop Lamps", new() { "Lamps hung from the pit props, smoking" }, Items(() => new MinersLamp()), new[] { "smoky", "dim" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "delving")) });
        Open((n, c, t, d, m) => new SeamArea(n, c, t, d, m), "Ore Gallery", "in the ore gallery",
            "A low gallery following the seam, the walls scarred with pick-marks", new[] { "low", "scarred", "airless" },
            new OrePointOfInterest("Working Face", new() { "The face of the seam, glinting where the pick last bit" }, Items(() => new IronOre(), () => new TinOre(), () => new LeadOre()), new[] { "glinting", "hard" })
            { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "veinsight"), ("listen", "hollow_ear")) });
        Open((n, c, t, d, m) => new ScreeArea(n, c, t, d, m), "Spoil Heap", "on the spoil heap",
            "A grey slope of waste rock tipped from the carts, nothing growing on it", new[] { "barren", "loose", "grey" },
            new RubblePointOfInterest("Waste Rock", new() { "Broken rock with a little ore still in it" }, Items(() => new Rock(), () => new CopperOre()), new[] { "loose", "sharp" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "gleaning")) });
        FinishOpenSite(rng, scene, PlaceName, "The workings and the yard at their mouth", FurnitureSubfactory.Setting.Highland);
        var roster = new List<NamedNpcArchetype> { new MinerArchetype() };
        for (int i = 0, n = rng.Next(2, 5); i < n; i++) roster.Add(new MinerArchetype());
        if (rng.NextDouble() < 0.6) roster.Add(new ClerkArchetype());
        Lodge(rng, scene, "Miners' Lodge", "lodge", roster);
    }

    private void BuildSanctuary(Random rng, Scene scene)
    {
        Open((n, c, t, d, m) => new WalkArea(n, c, t, d, m), "Pilgrims' Way", "on the pilgrims' way",
            "A worn path lined with small cairns and ribbons tied to bushes", new[] { "worn", "hopeful", "quiet" },
            new CairnPointOfInterest("Prayer Cairns", new() { "Small cairns of stones, each one a prayer someone carried here" }, Items(() => new Rock(), () => new Ampulla()), new[] { "many", "small" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "pilgrimage"), ("contemplate", "devotion")) });
        Open((n, c, t, d, m) => new PoolArea(n, c, t, d, m), "Sacred Spring", "at the sacred spring",
            "A spring welling up into a stone basin, coins glinting on its floor", new[] { "clear", "cold", "holy" },
            new PoolPointOfInterest("Spring Basin", new() { "A stone basin brimming with cold clear water" }, Items(() => new HolyWater(), () => new OldCoin()), new[] { "brimming", "cold" })
            { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Teach(("listen", "water_voice"), ("contemplate", "mysticism")) });
        Open((n, c, t, d, m) => new SanctumArea(n, c, t, d, m), "Sanctum", "in the sanctum",
            "An open holy place ringed with standing stones, an altar at its heart", new[] { "hushed", "ringed", "ancient" },
            new ShrinePointOfInterest("Stone Altar", new() { "A rough altar stone heaped with offerings" }, Items(() => new Offering(), () => new VotiveCandle(), () => new Incense()), new[] { "rough", "heaped" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "liturgy"), ("smell", "incensing"), ("contemplate", "awe")) });
        FinishOpenSite(rng, scene, PlaceName, "A holy place in the open, kept by a few", FurnitureSubfactory.Setting.Settlement);
        Lodge(rng, scene, "Pilgrims' Lodge", "lodge", new List<NamedNpcArchetype> { new PriestArchetype(), new MonkArchetype() });
    }

    private void BuildBurialField(Random rng, Scene scene)
    {
        Open((n, c, t, d, m) => new GreenArea(n, c, t, d, m), "Barrow Field", "among the barrows",
            "Long green mounds in rows, sheep grazing between them", new[] { "rolling", "quiet", "old" },
            new GravePointOfInterest("Barrow", new() { "A long turf mound, a stone at its head" }, Items(() => new Clay(), () => new Potsherd()), new[] { "long", "grassed" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "archeology")) });
        Open((n, c, t, d, m) => new WalkArea(n, c, t, d, m), "Tomb Row", "along the tomb row",
            "A row of stone tombs with carved doors, some of them standing open", new[] { "carved", "silent", "lichened" },
            new TombPointOfInterest("Family Tomb", new() { "A carved tomb with a heavy stone door" }, Items(() => new BurialUrn(), () => new GraveRing()), new[] { "carved", "heavy" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "grave_robbing"), ("contemplate", "elegy")) });
        Open((n, c, t, d, m) => new GroundArea(n, c, t, d, m), "New Graves", "by the new graves",
            "Fresh graves with wooden markers, the earth still dark", new[] { "raw", "sad", "dark" },
            new GravePointOfInterest("Fresh Grave", new() { "A grave dug and filled within the month" }, Items(() => new Clay(), () => new Clay()), new[] { "raw", "soft" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "sextonry"), ("smell", "charnel_sense")) });
        FinishOpenSite(rng, scene, PlaceName, "Ground given to the dead", FurnitureSubfactory.Setting.Settlement);
        var roster = new List<NamedNpcArchetype> { new GravediggerArchetype() };
        if (rng.NextDouble() < 0.6) roster.Add(new PriestArchetype());
        Lodge(rng, scene, "Sexton's Lodge", "lodge", roster);
    }

    private void BuildWreck(Random rng, Scene scene)
    {
        Open((n, c, t, d, m) => new BeachArea(n, c, t, d, m), "Wreck Beach", "on the wreck beach",
            "A shingle beach strewn with what the sea threw up, gulls picking along the line", new[] { "strewn", "wind-scoured", "loud" },
            new DriftwoodPointOfInterest("Wrack Line", new() { "Timber, rope and broken casks along the tide line" }, Items(() => new Driftwood(), () => new Rope(), () => new Tar()), new[] { "tangled", "salt" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "salvage"), ("smell", "brine_sense")) });
        Open((n, c, t, d, m) => new ShoreArea(n, c, t, d, m), "Broken Hull", "beside the broken hull",
            "The ribs of a great ship heeled over on the rocks, its back broken", new[] { "broken", "dripping", "vast" },
            new HullPointOfInterest("Stove Hull", new() { "The hull, stove in below the waterline, weed hanging from it" }, Items(() => new Sailcloth(), () => new Plank()), new[] { "stove", "weeded" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "shipwrighting"), ("contemplate", "elegy")) });
        Open((n, c, t, d, m) => new ChamberArea(n, c, t, d, m), "Flooded Hold", "in the flooded hold",
            "The hold, half full of seawater, cargo floating in it", new[] { "dark", "sloshing", "cold" },
            new CratePointOfInterest("Sodden Cargo", new() { "Crates and casks bobbing in the hold" }, Items(() => new OldCoin(), () => new Chart(), () => new Compass(), () => new Amphora()), new[] { "sodden", "floating" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "salvage")) });
        FinishOpenSite(rng, scene, PlaceName, "What is left of a ship and what the sea gave back of it", FurnitureSubfactory.Setting.Water);
    }

    // ── People ────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        SpawnQueuedCrews(rng, scene);
        var outside = _outdoors;
        if (_town != null) TownAnimals(rng, scene, _town.All);
        switch (_kind)
        {
            case PlaceKind.Port:
                TrySpawnShallow(rng, scene, new SeagullArchetype(), outside, 0.9);
                TrySpawnShallow(rng, scene, new CormorantArchetype(), outside, 0.5);
                TrySpawnShallow(rng, scene, new ShipRatArchetype(), outside, 0.6);
                if (Biome == Glyph.Microworld.BiomeDatabase.HotSteppe) TrySpawnShallow(rng, scene, new PelicanArchetype(), outside, 0.5);
                break;
            case PlaceKind.Castle:
            case PlaceKind.Palace:
            case PlaceKind.Citadel:
                if (_edifice != null)
                {
                    TrySpawnShallow(rng, scene, new MastiffArchetype(), new[] { _edifice.Entrance }, 0.6);
                    TrySpawnShallow(rng, scene, new HoundArchetype(), _edifice.Of(EdificeRoom.GreatHall), 0.6);
                    TrySpawnShallow(rng, scene, new JackdawArchetype(), new[] { _edifice.Battlements }, 0.6);
                    if (_kind == PlaceKind.Palace) TrySpawnShallow(rng, scene, new PeacockArchetype(), outside, 0.7);
                }
                break;
            case PlaceKind.Temple:
            case PlaceKind.ImperialTemple:
                TrySpawnShallow(rng, scene, new WhiteDoveArchetype(), outside, 0.8);
                TrySpawnShallow(rng, scene, new CarpArchetype(), outside, 0.7);
                break;
            case PlaceKind.Monastery:
                TrySpawnShallow(rng, scene, new BeeArchetype(), outside, 0.8);
                TrySpawnShallow(rng, scene, new GoatArchetype(), outside, 0.4);
                break;
            case PlaceKind.Pyramid:
                TrySpawnShallow(rng, scene, new CobraArchetype(), outside, 0.5);
                TrySpawnShallow(rng, scene, new JackalArchetype(), outside, 0.5);
                if (_edifice != null) SprinkleSmallLife(rng, scene, _edifice.Of(EdificeRoom.Crypt), SmallLife.Tomb, 2, 4);
                break;
            case PlaceKind.Mine:
                TrySpawnShallow(rng, scene, new BatArchetype(), outside, 0.6);
                SprinkleSmallLife(rng, scene, outside, SmallLife.Subterranean, 1, 3);
                break;
            case PlaceKind.Sanctuary:
                TrySpawnShallow(rng, scene, new WhiteDoveArchetype(), outside, 0.7);
                if (Biome == Glyph.Microworld.BiomeDatabase.HotSteppe) TrySpawnShallow(rng, scene, new IbisArchetype(), outside, 0.6);
                break;
            case PlaceKind.BurialField:
                TrySpawnShallow(rng, scene, new RavenArchetype(), outside, 0.7);
                TrySpawnShallow(rng, scene, new BarnOwlArchetype(), outside, 0.4);
                TrySpawnShallow(rng, scene, new JackalArchetype(), outside, 0.3);
                SprinkleSmallLife(rng, scene, outside, SmallLife.Tomb, 2, 4);
                break;
            case PlaceKind.Wreck:
                TrySpawnShallow(rng, scene, new SeagullArchetype(), outside, 0.9);
                TrySpawnShallow(rng, scene, new CrabArchetype(), outside, 0.8);
                TrySpawnShallow(rng, scene, new CormorantArchetype(), outside, 0.5);
                break;
        }
        if (_edifice != null && _kind != PlaceKind.Pyramid)
            SprinkleSmallLife(rng, scene, _edifice.Rooms, SmallLife.Settlement, 2, 5);
    }
}
