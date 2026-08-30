using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Test;

/// <summary>
/// A location that exists only to be tested against: every kind of thing the game has, in one place,
/// under names that never change.
///
/// <para><b>Why this exists.</b> The CLI suite used to run against the real factories, and every
/// script named what it was aiming at — <c>--start-area "Alehouse Store"</c>,
/// <c>--observe-only "Shelving Rack"</c>. Those names are rolled. Adding one
/// <c>rng.NextDouble()</c> to <c>BuildingFactory</c> — a single new content roll, in the middle of a
/// method — re-seeded every draw after it, renamed the buildings and the people in them, and broke
/// six unrelated tests. Moving that roll to the end of the method fixes it exactly once, until the
/// next piece of content.</para>
///
/// <para>So the tests come here instead. <b>Nothing in this factory is rolled</b>: every area, every
/// point of interest and every description is written out. The <c>rng</c> handed to
/// <c>BuildSections</c> is deliberately untouched, so the names hold no matter what changes
/// elsewhere, and a script pinned to "Test Yard" stays pinned to it forever.</para>
///
/// <para><b>What this does NOT test.</b> Real content. A verb test asks "does this verb work"; the
/// scaffolding it stands on is not the subject, and scaffolding that drifts is noise. Whether the
/// real factories actually place a lockable door, an ore seam or a sleeping NPC is the job of
/// <c>--verb-audit</c> and <c>--building-audit</c>, which sweep all nine real factories across
/// dozens of location ids and would notice the day one stops. Keep it that way: if a verb becomes
/// reachable ONLY here, the audit's dead-verb warning is the thing that should say so.</para>
///
/// <para><b>Not reachable in play.</b> Registered under the name <c>test</c> and attached to no
/// biome, so <c>--location-type test</c> is the only way in.</para>
///
/// <para><b>Adding a verb?</b> Add what it needs here, and its <c>cli/&lt;verb&gt;/</c> folder. The
/// two go together — a verb with no test is a verb nobody will notice breaking.</para>
/// </summary>
public sealed class TestSceneFactory : SceneFactory
{
    /// <summary>The name <c>--location-type</c> takes. Also the folder tests point at.</summary>
    public const string TypeName = "test";

    public TestSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // rng is deliberately never touched: see the class summary. Anything drawn from it here
        // would make these names move the next time a shared subfactory changed.

        // ── Outdoor areas ─────────────────────────────────────────────────────
        var yard  = Area_("Test Yard",  "yard",  "in the test yard",
                          "walk back out into the yard",
                          "A flat square of beaten earth with everything else opening off it");
        var field = Area_("Test Field", "field", "out in the test field",
                          "step out into the field",
                          "A worked strip of earth, half of it turned and half of it standing crop");
        var wood  = Area_("Test Wood",  "wood",  "among the test wood",
                          "go in under the trees",
                          "A stand of old trees close enough together to lose the sky");
        // The wolf lives here and nowhere else. A hostile beast OPENS the next observation phase
        // wherever it stands, ahead of --observe-only, so a wolf in the wood hijacked every phase
        // that started there — cut_wood, scale_up and stalk all ran `appease` on it instead of the
        // verb they name. Giving it a room of its own means only the scripts that want it go there.
        var den   = Area_("Test Den",   "den",   "at the mouth of the test den",
                          "follow the track to the den",
                          "A scrape under a fallen trunk, worn smooth by something that lives in it");
        var shore = Area_("Test Shore", "shore", "on the test shore",
                          "go down to the water",
                          "A bank of shingle running down into slow green water");
        var rock  = Area_("Test Rock",  "rock",  "at the foot of the test rock",
                          "cross to the rock face",
                          "A face of bare stone with a seam of ore running across it at head height");

        // ── High areas, reached only by climbing ──────────────────────────────
        var ledge = Area_("Test Ledge", "ledge", "up on the test ledge",
                          "pull yourself onto the ledge",
                          "A shelf of rock standing clear of everything around it");
        var crown = Area_("Test Crown", "crown", "up in the test crown",
                          "haul yourself into the crown",
                          "A platform of limbs near the top of the great tree, the whole place below");

        // ── The building ──────────────────────────────────────────────────────
        var hall = Area_("Test Hall", "hall", "in the test hall",
                         "step into the hall",
                         "A single public room with a counter down one side");
        var room = Area_("Test Room", "room", "in the test room",
                         "go through into the back room",
                         "A back room with a bed in it and a chest at the foot of the bed");
        var roof = Area_("Test Roof", "roof", "up on the test roof",
                         "pull yourself over the eaves",
                         "A pitch of thatch with the whole yard laid out below it");
        room.IsPrivate = true;

        var outdoors = new Section("Test Grounds", new List<string> { "the open ground of a test location" },
                                   seed => new NoisyGenerator { Seed = seed, Density = 0.5f });
        foreach (var a in new[] { yard, field, wood, den, shore, rock, ledge, crown })
            outdoors.Areas.Add(a);

        var building = new Section("Test Building", new List<string> { "a plain stone building" },
                                   seed => new RoomsGenerator { Seed = seed })
        { IsInterior = true };
        foreach (var a in new[] { hall, room, roof }) building.Areas.Add(a);

        scene.Sections.Add(outdoors);
        scene.Sections.Add(building);

        // ── Ways between them ─────────────────────────────────────────────────
        // Paths carry a graph edge (they are walks, not gates); everything else is a gate and must
        // not, or MoveToAreaVerb walks around it — BuildingAudit enforces exactly that.
        foreach (var (a, b, name, desc) in new[]
        {
            (yard, field, "Yard–Field Track", "A trodden line between the yard and the field"),
            (yard, wood,  "Yard–Wood Track",  "A path going in under the trees"),
            (yard, shore, "Yard–Shore Track", "A path down to the water"),
            (yard, rock,  "Yard–Rock Track",  "A path across to the rock face"),
            (wood, den,   "Wood–Den Track",   "A narrow run through the undergrowth, pressed flat by use"),
        })
        {
            new PathPointOfInterest(a, b, name, new List<string> { desc }).AttachTo(scene);
            scene.ConnectAreasBidirectional(a, b);
        }

        // Gates: a door, a cliff, two climbs, a ford, a crossing.
        new DoorPointOfInterest(yard, hall, "Test Door",
            new List<string> { "A plank door with a heavy lock on it" }, DoorState.Locked)
            .AttachTo(scene);

        new DoorPointOfInterest(hall, room, "Test Room Door",
            new List<string> { "An inner door standing ajar" }, DoorState.Unlocked)
            .AttachTo(scene);

        new CliffPointOfInterest(rock, ledge, "Test Cliff",
            new List<string> { "A broken run of rock going up to the ledge, holds good most of the way" })
            .AttachTo(scene);

        Scale_(scene, wood, crown, ScaleKind.Tree, "Test Giant Tree",
               "A trunk too wide to reach round, its bark broken into holds the whole way up");
        Scale_(scene, yard, roof, ScaleKind.Wall, "Test Wall",
               "The building's outside wall, coursed stone with gaps enough for fingers and boot-toes");

        // HoldsFish is the gate for `fish`, and like every extraction verb it also needs items left.
        new WaterCrossingPointOfInterest(shore, field, WaterKind.River, "Test Ford",
            new List<string> { "A slow green river, shallow enough to try and wide enough to matter" },
            items: new List<ItemElement> { new(new Peat()), new(new Peat()) })
        { HoldsFish = true }.AttachTo(scene);

        new CrossingPointOfInterest(field, wood, CrossingKind.MudPuddle, "Test Mud",
            new List<string> { "Churned ground that swallows a boot, wide enough to have to cross rather than skirt" })
            .AttachTo(scene);

        new StairPointOfInterest(hall, roof, "Test Stair",
            new List<string> { "A boxed stair going up from the corner of the hall to a hatch in the roof" })
            .AttachTo(scene);

        new SlipIntoPointOfInterest(roof, hall, SlipKind.Chimney, "Test Chimney",
            new List<string> { "A smoke-hole wide enough at the head to take a body, sooted the whole way" })
            .AttachTo(scene);

        // ── What can be seen from the high places ─────────────────────────────
        AddLandscapes(scene, ledge, new[] { yard, field, wood, shore, rock });
        AddLandscapes(scene, crown, new[] { yard, field, shore, rock });
        AddLandscapes(scene, roof,  new[] { yard, field, wood, shore, rock });

        // ── Things to act on ──────────────────────────────────────────────────
        AddContent(scene, yard, field, wood, shore, rock, hall, room);

        RegisterAll(scene, outdoors);
        RegisterAll(scene, building);

        AddPeople(scene, yard, hall, room, wood, den);

        Console.WriteLine($"TestSceneFactory: built the test location — {scene.AllAreas.Count} areas, "
                          + $"{scene.Npcs.Count} people, nothing rolled");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Area Area_(string name, string lemma, string context, string transition, string description)
        => new(name, lemma, context, transition, new List<string> { description },
               new[] { "plain", "even", "unremarkable" });

    private static void Scale_(Scene scene, Area bottom, Area top, ScaleKind kind, string name, string desc)
        => new ScalePointOfInterest(bottom, top, kind, name, new List<string> { desc },
                                    new[] { "sheer", "weathered", "hand-worn" })
        {
            Senses = SensoryProfile.Examinable,
            VerbModiMentis = new Dictionary<string, string> { ["examine"] = "architecture" },
        }.AttachTo(scene);

    private static PointOfInterest Poi_(string name, string lemma, string description,
                                        List<ItemElement>? items = null, bool natural = true)
        => new(name, lemma, new List<string> { description }, items,
               new[] { "plain", "ordinary" }, natural)
        { Senses = SensoryProfile.Examinable };

    /// <summary>
    /// One target for every verb family that acts on a thing rather than a person.
    ///
    /// <para>The types matter more than the names. A verb gates on a <i>subclass</i> or on a
    /// reference lemma — <c>dig</c> wants a <see cref="DiggableGroundPointOfInterest"/>, <c>mine</c>
    /// an <see cref="OreVeinPointOfInterest"/>, <c>cut_wood</c> a <see cref="TreePointOfInterest"/>
    /// or one of its three companions, and
    /// every extraction verb also wants the target to still hold items. A plain
    /// <c>PointOfInterest</c> named "Test Ore Seam" looks right and is offered to nobody, which is
    /// how the first draft of this file covered 25 verbs instead of 53.</para>
    /// </summary>
    private static void AddContent(Scene scene, Area yard, Area field, Area wood, Area shore, Area rock,
                                   Area hall, Area room)
    {
        // Extraction: each needs its own subclass AND items left on it.
        field.PointsOfInterest.Add(new DiggableGroundPointOfInterest("Test Peat Cut", "peat",
            new List<string> { "A cut face of black peat, soft enough to spade" },
            new List<ItemElement> { new(new Peat()), new(new Peat()) }));

        rock.PointsOfInterest.Add(new OreVeinPointOfInterest("Test Ore Seam", "seam",
            new List<string> { "A seam of dull metal running through the stone" },
            new List<ItemElement> { new(new IronOre()), new(new IronOre()) }));

        // cut_wood accepts four wooded KINDS — a tree, a log, a stump or a deadfall. It gated on the
        // reference lemma once; a plain PointOfInterest carrying the word "tree" is offered to nobody
        // now, which is exactly what a type is for.
        wood.PointsOfInterest.Add(new TreePointOfInterest("Test Timber Tree",
            new List<string> { "A straight tree of a size worth felling" },
            new List<ItemElement> { new(new Log()), new(new Log()) },
            isNatural: true) { Senses = SensoryProfile.Examinable });

        // gather / grab: something growing, holding something loose. A BUSH rather than a plain PoI
        // so GATHER's berrying lesson is reachable here too.
        field.PointsOfInterest.Add(new BushPointOfInterest("Test Berry Bush",
            new List<string> { "A bramble heavy with fruit" },
            new List<ItemElement> { new(new HawthornBerry()), new(new HawthornBerry()) },
            isNatural: true) { Senses = SensoryProfile.Examinable });

        // Sitting, hiding, breaking — each its own type, and each what its verb looks for.
        yard.PointsOfInterest.Add(new SitSpotPointOfInterest("Test Bench", "bench",
            new List<string> { "A plank bench worn smooth by sitting" }));

        yard.PointsOfInterest.Add(new HidingPointOfInterest("Test Hay Pile", "hay",
            new List<string> { "A heap of hay big enough to get inside and not be seen" },
            isNatural: true));

        hall.PointsOfInterest.Add(new BreakablePointOfInterest("Test Crate", "crate",
            new List<string> { "A slat crate stacked against the counter" },
            Poi_("Test Broken Crate", "wreckage", "Splintered slats and what was inside them",
                 new List<ItemElement> { new(new Log()) }, natural: false)));

        // A private room to steal from, and something in it worth taking.
        room.PointsOfInterest.Add(Poi_("Test Chest", "chest",
            "A banded chest at the foot of the bed",
            new List<ItemElement> { new(new IronBar()) }, natural: false));

        // The bed, and it must be a PalletPointOfInterest — BuildingRooms.BedsIn selects that TYPE,
        // and SceneNpc.IsSleeping refuses to put anybody to bed in a room BedsIn calls empty. Without
        // it the weaver stands in this room all night and murder / wake_up are offered on nobody.
        room.PointsOfInterest.Add(new PalletPointOfInterest("Test Pallet",
            new List<string> { "A straw pallet along the wall, blankets thrown back" })
            { Senses = SensoryProfile.Examinable });

        // grab wants a MADE thing in a PUBLIC area — natural is gather's, private is steal's. The
        // chest above is neither, being in the private room.
        yard.PointsOfInterest.Add(Poi_("Test Rack", "rack",
            "A tool rack against the yard wall, its pegs mostly full",
            new List<ItemElement> { new(new IronBar()) }, natural: false));

        // Something to listen to and something to smell: listen and smell gate on the sensory
        // profile, so a thing with the wrong one is prose and nothing else.
        yard.PointsOfInterest.Add(new BellPointOfInterest("Test Bell",
            new List<string> { "A bell on a post, swinging and sounding in any wind at all" },
            null, new[] { "ringing", "bright" }, false)
        { Senses = SensoryProfile.Audible });

        yard.PointsOfInterest.Add(new MiddenPointOfInterest("Test Midden",
            new List<string> { "A heap of kitchen waste going over, and announcing it" },
            null, new[] { "rank", "warm" }, true)
        { Senses = SensoryProfile.Odorous });
    }

    /// <summary>
    /// The people. Spawned from a dedicated fixed <see cref="Random"/> so they are the same two every
    /// build — but a test should still target them by <b>archetype id</b> (<c>--observe-only
    /// brewer</c>), never by the generated name: names come from the name generator, which is content
    /// like any other and free to change.
    /// </summary>
    private static void AddPeople(Scene scene, Area yard, Area hall, Area room, Area wood, Area den)
    {
        var names = new Random(20260809);

        Person_(scene, names, new BrewerArchetype(), hall, NpcSchedule.Always(hall));

        // The one person who MOVES. `stalk` plans against the next period the quarry relocates in, so
        // against a location where everybody stands still all day it is offered on nobody — which is
        // what the first draft of this file did, with three Always schedules and no way to tell.
        // The move must be to somewhere with public ground beside it, so yard→hall would not do:
        // hall is reached through a locked door.
        Person_(scene, names, new FarmerArchetype(), yard, NpcSchedule.Roaming(new Dictionary<TimePeriod, Area?>
        {
            [TimePeriod.Dawn]      = yard,
            [TimePeriod.Morning]   = wood,
            [TimePeriod.Noon]      = wood,
            [TimePeriod.Afternoon] = yard,
            [TimePeriod.Evening]   = yard,
            [TimePeriod.Night]     = yard,
        }));

        // Somebody asleep in their own bed, at every period — murder and wake_up need one, and a
        // schedule that only sleeps at night makes those two testable one period in six.
        Person_(scene, names, new WeaverArchetype(), room, NpcSchedule.Always(room));

        // A go-between and somebody to be taken to. `introduce_me` is offered only where an NPC
        // declares CanIntroduceToArchetypes AND the archetype it names is standing in the scene —
        // only PeasantArchetype (→ reeve) and Apprentice (→ their own master) declare one at all — the
        // plowman is a peasant — so without
        // this pair the verb is offered on nobody and its test runs `attack` instead.
        Person_(scene, names, new PlowmanArchetype(), yard, NpcSchedule.Always(yard));
        Person_(scene, names, new ReeveArchetype(),   hall, NpcSchedule.Always(hall));

        // A tiny thing (catch, crush), an animal worth killing (slay, and then cut its body), and a
        // beast that counts you an enemy from the start (appease, and once appeased, tame).
        Creature_(scene, names, new ButterflyArchetype(), yard);
        Creature_(scene, names, new ChickenArchetype(),   yard);
        var wolf = Creature_(scene, names, new WolfArchetype(), den);

        // Sign of the wolf, one area away from the wolf. `track` reads the quarry off the print, so
        // this has to name the individual that is actually in the scene, not a wolf in the abstract.
        yard.PointsOfInterest.Add(new FootprintPointOfInterest(wolf, "Test Wolf Print",
            new List<string> { "Four toes and a pad, pressed into the mud and pointing at the wood" }));
    }

    private static SceneNpc Creature_(Scene scene, Random rng, ShallowNpcArchetype archetype, Area area)
    {
        var sceneNpc = new SceneNpc(archetype.Spawn(rng, area.DisplayName.ToLowerInvariant()));
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area);
        return sceneNpc;
    }

    private static SceneNpc Creature_(Scene scene, Random rng, NamedNpcArchetype archetype, Area area)
    {
        var sceneNpc = new SceneNpc(archetype.Spawn(rng, area.ContextDescription));
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area);
        return sceneNpc;
    }

    private static void Person_(Scene scene, Random rng, NamedNpcArchetype archetype, Area area, NpcSchedule schedule)
    {
        var sceneNpc = new SceneNpc(archetype.Spawn(rng, area.ContextDescription));
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = schedule;
    }
}
