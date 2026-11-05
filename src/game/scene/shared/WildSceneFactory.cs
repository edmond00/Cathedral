using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// The common half of every wilderness factory — the six temperate ones (plain, forest, cave, coast,
/// mountain, peak) and the eight of the hot and cold country (see <c>ClimateRule</c>).
///
/// <para>Each factory decides everything that makes the place what it is: its identity roll, its areas
/// and their contents, its climbs and waters, its beasts. What it gets from here is how those are put
/// together, so that two places of one kind come out as two different places:</para>
///
/// <list type="bullet">
/// <item><b>Drawn, not listed.</b> A factory offers a pool of areas with weights (<see cref="Draw"/>)
/// and a count range, so which areas a visit holds is a draw and not a fixed list with a coin or two
/// flipped beside it.</item>
/// <item><b>Woven, not chained.</b> The areas of a section are joined in a rolled shape
/// (<see cref="WeaveSection"/>) — a line, a hub, a branching tree, a loop, a fan — so the same areas
/// make a different map. Every wilderness place used to be one corridor.</item>
/// <item><b>Tiered.</b> A factory states its <see cref="SceneFactory.Profile"/> and gives each section
/// a <see cref="Section.Tier"/>; <see cref="JoinSections"/> joins sections where the profile says they
/// meet, and <see cref="Furnish"/> keeps the rolled shortcuts from jumping a tier. <c>--biome-audit</c>
/// holds every build to the profile.</item>
/// </list>
///
/// <para>Uninhabited by design: nothing here spawns a person.</para>
/// </summary>
public abstract class WildSceneFactory : SceneFactory
{
    /// <summary>Wilderness: its open ground is nobody's (see <see cref="AreaTenure.Wild"/>).</summary>
    protected override bool IsWilderness => true;

    protected WildSceneFactory(string? sessionPath) : base(sessionPath) { }

    /// <summary>Every walkable area, in the order the sections were built — the beasts' range.</summary>
    protected readonly List<Area> _allAreas = new();

    /// <summary>The tier of every area placed so far, for the shortcut filter.</summary>
    private readonly Dictionary<Guid, int> _tierOf = new();

    /// <summary>Builds a section around <paramref name="areas"/>, registers it, and adds them to the range.</summary>
    protected Section AddSection(Scene scene, string name, string description,
                                 Func<int, IFightAreaGenerator> generator, IEnumerable<Area> areas, int tier = 0)
    {
        var section = new Section(name, new() { description }, generator) { Tier = tier };
        section.Areas.AddRange(areas);
        scene.Sections.Add(section);
        RegisterAll(scene, section);
        _allAreas.AddRange(section.Areas);
        foreach (var a in section.Areas) _tierOf[a.Id] = tier;
        return section;
    }

    /// <summary>
    /// Adds <paramref name="area"/> to an existing section after the fact — a climbed-to top, a store —
    /// keeping the partition whole. Not added to the beasts' range unless <paramref name="roamed"/>.
    /// </summary>
    protected void AddToSection(Scene scene, Section section, Area area, bool roamed = false)
    {
        section.Areas.Add(area);
        RegisterAll(scene, area);
        _tierOf[area.Id] = section.Tier;
        if (roamed) _allAreas.Add(area);
    }

    // ── Drawing areas ─────────────────────────────────────────────────────────

    /// <summary>One area a factory may put in a section, and how likely it is to be drawn.</summary>
    protected readonly record struct Option(Func<Area> Build, double Weight = 1.0);

    /// <summary>
    /// Draws <paramref name="count"/> distinct options from <paramref name="pool"/> by weight, in the
    /// order drawn — which is the order <see cref="WeaveSection"/> lays them, so the first drawn is the
    /// hub or the head of the line.
    /// </summary>
    protected static List<Area> Draw(Random rng, IReadOnlyList<Option> pool, int count)
    {
        var left  = pool.ToList();
        var drawn = new List<Area>();
        while (drawn.Count < count && left.Count > 0)
        {
            double total = left.Sum(o => o.Weight);
            double roll  = rng.NextDouble() * total;
            int pick = 0;
            for (double acc = left[0].Weight; acc < roll && pick < left.Count - 1; acc += left[++pick].Weight) { }
            drawn.Add(left[pick].Build());
            left.RemoveAt(pick);
        }
        return drawn;
    }

    /// <summary>A count in <c>[min, max]</c>, inclusive.</summary>
    protected static int Between(Random rng, int min, int max) => rng.Next(min, max + 1);

    /// <summary>Shuffles <paramref name="list"/> in place (Fisher–Yates on the build generator).</summary>
    protected static void Shuffle<T>(Random rng, IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── Weaving a section ─────────────────────────────────────────────────────

    /// <summary>The shape a section's areas are joined in.</summary>
    protected enum Weave
    {
        /// <summary>Each to the next: a valley, a shore, a gully.</summary>
        Line,
        /// <summary>All off the first: a clearing, a waterhole, a basin with the rest around it.</summary>
        Hub,
        /// <summary>Each to one drawn before it: country that branches.</summary>
        Branching,
        /// <summary>A line whose ends meet: open ground with two ways round.</summary>
        Loop,
        /// <summary>A hub with one cross-link between two of its spokes.</summary>
        Fan,
    }

    /// <summary>Rolls a weave; a section of one or two areas is a line whatever is rolled.</summary>
    protected static Weave RollWeave(Random rng, params Weave[] allowed)
    {
        if (allowed.Length == 0) allowed = (Weave[])Enum.GetValues(typeof(Weave));
        return allowed[rng.Next(allowed.Length)];
    }

    /// <summary>
    /// Joins the areas of one section in <paramref name="weave"/>, with <c>areas[0]</c> as its hub or
    /// head. <paramref name="name"/> picks the kind of way from the two ends.
    /// </summary>
    protected static void WeaveSection(Random rng, Scene scene, IReadOnlyList<Area> areas, Weave weave,
                                       Func<Area, Area, string> name, string surface, string[] moods)
    {
        void J(Area a, Area b) => OutdoorLayout.Link(scene, a, b, name(a, b), moods, surface);
        if (areas.Count < 2) return;
        if (areas.Count == 2) { J(areas[0], areas[1]); return; }

        switch (weave)
        {
            case Weave.Line:
            case Weave.Loop:
                for (int i = 0; i < areas.Count - 1; i++) J(areas[i], areas[i + 1]);
                if (weave == Weave.Loop) J(areas[^1], areas[0]);
                break;
            case Weave.Hub:
            case Weave.Fan:
                for (int i = 1; i < areas.Count; i++) J(areas[0], areas[i]);
                if (weave == Weave.Fan)
                {
                    int a = Between(rng, 1, areas.Count - 2);
                    J(areas[a], areas[a + 1]);
                }
                break;
            case Weave.Branching:
                for (int i = 1; i < areas.Count; i++) J(areas[rng.Next(i)], areas[i]);
                break;
        }
    }

    /// <summary>
    /// Joins two sections by <paramref name="links"/> walks between areas drawn from each — the way
    /// down off a shoulder, the track from the shore into the dunes. Two links make a loop through
    /// both sections.
    /// </summary>
    protected static void JoinSections(Random rng, Scene scene, IReadOnlyList<Area> a, IReadOnlyList<Area> b,
                                       int links, Func<Area, Area, string> name, string surface, string[] moods)
    {
        var used = new HashSet<(Guid, Guid)>();
        for (int n = 0, tries = 0; n < links && tries < 12; tries++)
        {
            var x = a[rng.Next(a.Count)];
            var y = b[rng.Next(b.Count)];
            if (!used.Add((x.Id, y.Id))) continue;
            OutdoorLayout.Link(scene, x, y, name(x, y), moods, surface);
            n++;
        }
    }

    /// <summary>
    /// Joins consecutive areas of <paramref name="chain"/>. <paramref name="name"/> picks the kind of
    /// way from the two ends, which names any path later laid along it.
    /// </summary>
    protected static void ConnectChain(Scene scene, IReadOnlyList<Area> chain, Func<Area, Area, string> name,
                                       string surface, string[] moods)
    {
        for (int i = 0; i < chain.Count - 1; i++)
            OutdoorLayout.Link(scene, chain[i], chain[i + 1], name(chain[i], chain[i + 1]), moods, surface);
    }

    // ── Ways that are not a walk ──────────────────────────────────────────────

    /// <summary>
    /// A climb from <paramref name="bottom"/> to <paramref name="top"/> — the only way up, so no path
    /// joins them — and from the top a road to every other area of the place. The top must already be
    /// in a section and carry something, or the climb arrives nowhere.
    /// </summary>
    protected static void AddViewpoint(Scene scene, Area bottom, Area top, string climbName,
                                       string climbDescription, string[] moods, bool icy = false,
                                       string? lesson = null)
    {
        Climb(scene, bottom, top, climbName, climbDescription, moods, icy, lesson);
        AddLandscapes(scene, top, scene.AllAreas);
    }

    /// <summary>
    /// A climb with no view at its top: into an alcove, down a shaft, up into a crevice.
    /// <paramref name="lesson"/>, when given, is what climbing this one teaches in place of the verb's own.
    /// </summary>
    protected static void Climb(Scene scene, Area bottom, Area top, string name, string description,
                                string[] moods, bool icy = false, string? lesson = null)
        => new CliffPointOfInterest(bottom, top, name, new() { description }, icyCliff: icy, moods: moods)
           { VerbModiMentis = lesson == null ? null : new Dictionary<string, string> { ["climb_up"] = lesson } }
           .AttachTo(scene);

    /// <summary>
    /// Water between two areas that is crossed by swimming it. <paramref name="lesson"/>, when given, is
    /// what swimming this one teaches in place of the verb's own.
    /// </summary>
    protected static void Swim(Scene scene, Area a, Area b, WaterKind kind, string name, string description,
                               string[] moods, List<ItemElement>? items = null, string? lesson = null)
        => new WaterCrossingPointOfInterest(a, b, kind, name, new() { description }, moods, items)
           { VerbModiMentis = lesson == null ? null : new Dictionary<string, string> { ["swim_across"] = lesson } }
           .AttachTo(scene);

    /// <summary>An obstacle between two areas that is crossed: a trunk, a choke of boulders, a snow bridge.</summary>
    protected static void Cross(Scene scene, Area a, Area b, CrossingKind kind, string name, string description,
                                string[] moods, string examineLesson)
        => new CrossingPointOfInterest(a, b, kind, name, new() { description }, moods,
               verbModiMentis: new Dictionary<string, string> { ["examine"] = examineLesson })
           { Senses = SensoryProfile.Examinable }.AttachTo(scene);

    /// <summary>A gap a body squeezes through into somewhere enclosed: a crevice, a crawlway, a hollow.</summary>
    protected static void SlipInto(Scene scene, Area outside, Area inside, SlipKind kind, string name,
                                   string description, string[] moods, string lesson)
        => new SlipIntoPointOfInterest(outside, inside, kind, name, new() { description }, moods)
           { VerbModiMentis = new Dictionary<string, string> { ["slip_into"] = lesson } }.AttachTo(scene);

    /// <summary>Something scaled to stand on top of it: a tree, a stack, a boulder.</summary>
    protected static void Scale(Scene scene, Area bottom, Area top, ScaleKind kind, string name,
                                string description, string[] moods)
        => new ScalePointOfInterest(bottom, top, kind, name, new() { description }, moods).AttachTo(scene);

    // ── Furnishing ────────────────────────────────────────────────────────────

    /// <summary>
    /// The sit spots, hiding places, shortcuts and diggable ground every location gets.
    /// <paramref name="climbedTo"/> are the areas only a climb reaches: they may be furnished, but no
    /// shortcut may end on one, or the climb would have a way round it. A shortcut never joins two
    /// areas more than one tier apart, which would walk past the bands a tiered place is made of.
    /// </summary>
    protected void Furnish(Random rng, Scene scene, FurnitureSubfactory.Setting setting, params Area?[] climbedTo)
    {
        var outdoors = scene.OutdoorAreas;
        var ground = outdoors.Where(a => !climbedTo.Any(c => c != null && c.Id == a.Id)).ToList();
        FurnitureSubfactory.AddSitSpots(rng, outdoors, setting);
        FurnitureSubfactory.AddHidingPlaces(rng, outdoors, setting);
        FurnitureSubfactory.AddShortcuts(rng, scene, ground, setting, allowed: NeighbouringTiers);
        FurnitureSubfactory.AddExtractionPoints(rng, ground, setting);
    }

    /// <summary>
    /// Whether a shortcut may join <paramref name="a"/> and <paramref name="b"/>: in a tiered place only
    /// within a tier or between neighbouring ones, and in a hubbed or spined place only where the two
    /// sections already meet, so a shortcut never adds a border the map does not have.
    /// </summary>
    private bool NeighbouringTiers(Area a, Area b)
    {
        if (!_tierOf.TryGetValue(a.Id, out int ta) || !_tierOf.TryGetValue(b.Id, out int tb)) return true;
        return Profile switch
        {
            LayoutProfile.Pyramid or LayoutProfile.Descent => Math.Abs(ta - tb) <= 1,
            LayoutProfile.Radial or LayoutProfile.Spine    => ta == tb || Math.Min(ta, tb) == 0,
            _                                              => true,
        };
    }

    // ── Life ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rolls for a named beast and, if it lands, gives it a roaming day over <paramref name="range"/>
    /// (every area by default). Affinity persists per NPC through the location state's resolver, so
    /// a beast appeased here is still appeased on the next arrival.
    /// </summary>
    protected void TrySpawnBeast(Random rng, Scene scene, NamedNpcArchetype archetype, double chance,
                                 IReadOnlyList<Area>? range = null)
    {
        if (rng.NextDouble() > chance) return;
        range ??= _allAreas;
        if (range.Count == 0) return;

        var area = range[rng.Next(range.Count)];
        var entity = archetype.Spawn(rng, area.ContextDescription,
            _locationState != null ? _locationState.AffinityFor : null);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = RoamingSchedule(rng, range);
    }

    /// <summary>
    /// The generator the great reptiles are rolled from. They were added beside beasts whose rolls
    /// come off the build generator, and drawing from that one would shift every creature, item and
    /// sign rolled after them — so a location id players and scripts know would grow different
    /// wildlife for the sake of a crocodile. Same idea as <c>ExtrasRandom</c>, its own stream.
    /// </summary>
    protected static Random ReptileRandom(int locationId) => new(unchecked(locationId * 0x27D4EB2F + 0x5E9));

    /// <summary>Rolls for a shallow creature pinned to one area of <paramref name="range"/> (every area by default).</summary>
    protected void TrySpawnCreature(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance,
                                    IReadOnlyList<Area>? range = null)
        => TrySpawnShallow(rng, scene, archetype, range ?? _allAreas, chance);

    /// <summary>Rolls for a shallow creature that lives only in areas of kind <typeparamref name="TArea"/>.</summary>
    protected void TrySpawnCreatureIn<TArea>(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
        where TArea : Area
    {
        var range = _allAreas.OfType<TArea>().Cast<Area>().ToList();
        if (range.Count > 0) TrySpawnShallow(rng, scene, archetype, range, chance);
    }
}
