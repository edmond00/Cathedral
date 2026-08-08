using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// The complete scene for a location: sections/areas/spots hierarchy,
/// area connectivity graph, NPCs with schedules, applicable verbs, and state changes.
/// Provides <see cref="View(PoV)"/> to produce a frontend-consumable snapshot
/// filtered to the current point of view.
/// </summary>
public class Scene
{
    /// <summary>All registered elements, keyed by UUID for fast lookup.</summary>
    public Dictionary<Guid, Element> Elements { get; } = new();

    /// <summary>All sections in this scene.</summary>
    public List<Section> Sections { get; } = new();

    /// <summary>
    /// Directed area accessibility graph.
    /// Key = source area UUID, Value = set of reachable area UUIDs.
    /// </summary>
    public Dictionary<Guid, HashSet<Guid>> AreaGraph { get; } = new();

    /// <summary>All NPCs in this scene.</summary>
    public List<SceneNpc> Npcs { get; } = new();

    /// <summary>NPC UUID → schedule (which area at which time period).</summary>
    public Dictionary<Guid, NpcSchedule> NpcSchedules { get; } = new();

    /// <summary>Verbs applicable in this scene (subset from global VerbRegistry).</summary>
    public List<Verb> Verbs { get; } = new();

    /// <summary>State changes accumulated since scene creation (delta from factory initial state).</summary>
    public StateChangeSet StateChanges { get; set; } = new();

    /// <summary>
    /// Item-depletion timestamps for this location: <see cref="ItemElement.DepletionKey"/> → the
    /// <c>Protagonist.GameTimeDays</c> at which that slot was last picked. Pointed at the owning
    /// <c>LocationInstanceState.ItemDepletions</c> (shared backing store) so picks persist across
    /// visits without an explicit save step. An item is depleted while <c>now − pickedAt &lt; RegenDays</c>.
    /// </summary>
    public Dictionary<string, double> ItemDepletions { get; set; } = new();

    /// <summary>
    /// NPCs gone from this location for good, by <see cref="Npc.INpcEntity.PersistentId"/>. Pointed
    /// at the owning <c>LocationInstanceState.DepartedNpcs</c> (shared backing store, handed over by
    /// <c>LocationInstanceState.AttachTo</c>) so a departure recorded mid-visit survives the rebuild
    /// that happens on the next arrival. Written only through <see cref="RemoveNpcFromPlay"/>.
    /// </summary>
    public HashSet<string> DepartedNpcs { get; set; } = new();

    /// <summary>
    /// True while this scene is a throwaway used for routine <i>virtual</i> replay. Picking verbs must
    /// not mutate real state (inventory, depletion timestamps) when set.
    /// </summary>
    public bool IsVirtualReplay { get; set; }

    /// <summary>
    /// Which narration phase this scene belongs to. Defaults to <see cref="NarrationPhase.Exploration"/>.
    /// Special phases (e.g. <see cref="NarrationPhase.ChildhoodReminescence"/>) opt out of critic checks
    /// and noetic-point consumption and use phase-specific prompt contexts.
    /// </summary>
    public Cathedral.Game.Narrative.NarrationPhase Phase { get; set; }
        = Cathedral.Game.Narrative.NarrationPhase.Exploration;

    /// <summary>
    /// When <see cref="Phase"/> is <see cref="Cathedral.Game.Narrative.NarrationPhase.ChildhoodReminescence"/>,
    /// the id of the current reminescence (e.g. "sound_in_the_dark"). Null otherwise.
    /// Used by REMEMBER to record fragments in <see cref="Cathedral.Game.Narrative.ChildhoodHistory"/>.
    /// </summary>
    public string? CurrentReminescenceId { get; set; }

    /// <summary>
    /// Set by <c>RememberVerb.Execute()</c>; consumed by <see cref="NarrativeController"/>
    /// on the next frame to either rebuild the scene as the next reminescence or exit the phase.
    /// </summary>
    public ReminescenceTransitionRequest? PendingReminescenceTransition { get; set; }

    /// <summary>
    /// Takes an NPC out of play for good: killed, tamed into the party, or talked into joining it.
    ///
    /// <para><b>The one door every departure comes through</b> — the slay/murder verbs, a won fight,
    /// <c>TameVerb</c>'s <c>RecruitedOutcome</c> and the <c>propose_to_join</c> tree all end here. It
    /// does the three things that make someone gone from <i>this</i> visit (marks them not alive, so
    /// <see cref="GetNpcsAt"/> and with it every verb gate stops seeing them; drops them from the NPC
    /// list and the schedule table) and the fourth that makes them gone from every <i>later</i> one:
    /// records the persistent id, which the next build reads to leave them out.</para>
    ///
    /// <para>That fourth write is the whole point of routing these together. Without it the rebuild —
    /// a pure function of the location id — hands back the individual who left, so a tamed wolf pads
    /// beside you and waits in the clearing at the same time, and anyone killed is alive on the next
    /// visit. It is exactly the kind of step a fifth departure route would forget.</para>
    ///
    /// <para>Virtual replay is exempt from the persistent half: a throwaway scene must not be able to
    /// empty a real location.</para>
    /// </summary>
    public void RemoveNpcFromPlay(SceneNpc npc)
    {
        npc.Entity.IsAlive = false;
        Npcs.Remove(npc);
        NpcSchedules.Remove(npc.Id);
        DisplacedNpcs.Remove(npc.Id);
        PendingArrivalObservations.Remove(npc);

        if (!IsVirtualReplay)
            DepartedNpcs.Add(npc.Entity.PersistentId);
    }

    // ── Element registration ──────────────────────────────────────────────────

    /// <summary>Registers an element in this scene's dictionary. Called by <see cref="Element.Register"/>.</summary>
    public void RegisterElement(Element element)
    {
        Elements[element.Id] = element;
    }

    // ── Area graph helpers ────────────────────────────────────────────────────

    /// <summary>Adds a directed edge from one area to another.</summary>
    public void ConnectAreas(Area from, Area to)
    {
        if (!AreaGraph.TryGetValue(from.Id, out var targets))
        {
            targets = new HashSet<Guid>();
            AreaGraph[from.Id] = targets;
        }
        targets.Add(to.Id);
    }

    /// <summary>Adds bidirectional edges between two areas.</summary>
    public void ConnectAreasBidirectional(Area a, Area b)
    {
        ConnectAreas(a, b);
        ConnectAreas(b, a);
    }

    /// <summary>Returns areas reachable from the given area.</summary>
    public List<Area> GetReachableAreas(Area from)
    {
        if (!AreaGraph.TryGetValue(from.Id, out var targets))
            return new();

        return targets
            .Where(id => Elements.TryGetValue(id, out var el) && el is Area)
            .Select(id => (Area)Elements[id])
            .ToList();
    }

    // ── NPC schedule helpers ──────────────────────────────────────────────────

    /// <summary>Returns alive NPCs present at the given area during the given time period.</summary>
    public List<SceneNpc> GetNpcsAt(Area area, TimePeriod period)
    {
        var result = new List<SceneNpc>();
        foreach (var npc in Npcs)
        {
            if (!npc.IsAlive) continue;

            // A displaced NPC has left their day behind and stands where they were drawn to, at every
            // period, until the visit ends. Checked before the schedule so the override is total —
            // they are here and they are not also still at the forge.
            if (DisplacedNpcs.TryGetValue(npc.Id, out var displaced))
            {
                if (displaced.Id == area.Id) result.Add(npc);
                continue;
            }

            if (!NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;

            var scheduled = schedule.GetArea(period);
            if (scheduled != null && scheduled.Id == area.Id)
                result.Add(npc);
        }
        return result;
    }

    /// <summary>
    /// Where <paramref name="npc"/> is at <paramref name="period"/>, or null when they are absent
    /// then (or have no schedule at all).
    ///
    /// <para>The reverse of <see cref="GetNpcsAt"/>, and the one the verbs that reason about
    /// <i>somebody's day</i> need — tracking a beast to where it is now, following a person to where
    /// they will be next, working out whether someone is asleep in their own bed.</para>
    /// </summary>
    public Area? GetAreaOf(SceneNpc npc, TimePeriod period)
        => DisplacedNpcs.TryGetValue(npc.Id, out var displaced)
            ? displaced
            : NpcSchedules.TryGetValue(npc.Id, out var schedule) ? schedule.GetArea(period) : null;

    /// <summary>
    /// An NPC's whole day, in period order: where they are at each, null where they are away.
    /// Ordered by the enum rather than by dictionary order, because every caller reads it as a
    /// timeline.
    /// </summary>
    public IEnumerable<(TimePeriod Period, Area? Area)> DayOf(SceneNpc npc)
    {
        foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
            yield return (period, GetAreaOf(npc, period));
    }

    /// <summary>
    /// The next period, walking forward from <paramref name="from"/>, at which <paramref name="npc"/>
    /// is somewhere other than where they are now — or null if they stay put all day.
    ///
    /// <para>Wraps through the end of the day and stops before returning to <paramref name="from"/>,
    /// so following someone can never wait more than a full day round to the same moment.</para>
    /// </summary>
    public TimePeriod? NextRelocation(SceneNpc npc, TimePeriod from)
    {
        var current = GetAreaOf(npc, from);

        for (int step = 1; step < TimePeriodExtensions.PeriodsPerDay; step++)
        {
            var period = from.Advance(step);
            var there  = GetAreaOf(npc, period);

            bool moved = (current == null) != (there == null)
                         || (current != null && there != null && current.Id != there.Id);
            if (moved) return period;
        }

        return null;
    }

    /// <summary>
    /// Every alive NPC scheduled somewhere in the location at <paramref name="period"/> — the roster
    /// of who is <i>here at all</i>, as opposed to who is in one area. What a verb watching for
    /// somebody arriving or leaving compares between periods.
    /// </summary>
    public List<SceneNpc> PresentAt(TimePeriod period)
        => Npcs.Where(n => n.IsAlive && GetAreaOf(n, period) != null).ToList();

    // ── All areas flattened ───────────────────────────────────────────────────

    /// <summary>Returns all areas across all sections.</summary>
    public List<Area> AllAreas => Sections.SelectMany(s => s.Areas).ToList();

    /// <summary>
    /// Every area that is not inside a building — the open part of the location. What outdoor scene
    /// furnishing is scattered across, and what a shortcut may join: a crossing between two rooms of
    /// different houses is both nonsense and a way past two locked doors.
    /// </summary>
    public List<Area> OutdoorAreas =>
        Sections.Where(s => !s.IsInterior).SelectMany(s => s.Areas).ToList();

    /// <summary>Gets an area by its UUID.</summary>
    public Area? GetArea(Guid id) => Elements.TryGetValue(id, out var el) && el is Area a ? a : null;

    // ── Runtime point-of-interest management ──────────────────────────────────

    /// <summary>
    /// Corpses spawned since the narration last opened a phase, in the order they fell. Drained by
    /// <c>NarrativeController.GenerateObservationsAsync</c>, which opens the following phase by
    /// observing exactly these — the body you just made is what you look at next.
    ///
    /// <para>Recorded here rather than by each caller because <see cref="AddPointOfInterestToArea"/>
    /// is the one door every corpse comes through: a slay verb applying <c>NpcSlaynOutcome</c> and a
    /// won fight spawning one body per dead enemy both end up in it.</para>
    /// </summary>
    public List<Npc.Corpse.CorpsePointOfInterest> PendingCorpseObservations { get; } = new();

    /// <summary>
    /// NPCs standing somewhere their schedule does not put them, by <c>SceneNpc.Id</c> → where they
    /// actually are. Written when somebody within earshot hears an action go wrong and comes to look
    /// (see <see cref="ProximityModel"/>), which is the only thing in the game that moves a person
    /// off their day.
    ///
    /// <para><b>This is what makes the approach real</b>, and it has to live here rather than in the
    /// caller. Position is resolved from <see cref="NpcSchedules"/> by <see cref="GetNpcsAt"/>, and
    /// <c>SceneNpcPlacement.PlaceForPeriod</c> re-derives every NPC observation object from that on
    /// <i>every</i> segment — so a person moved any other way is put back one observation phase
    /// later, exactly as a tamed beast's stale observation object is. Consulted here instead, one
    /// override in one lookup, and the placement, the verb gates, the witness and threat selectors
    /// and the exit button all follow without knowing anything about it.</para>
    ///
    /// <para>One visit's business, like corpses: not in <c>LocationInstanceState</c>. What outlasts
    /// the visit is whatever the confrontation turned into — an enmity, which is persisted.</para>
    /// </summary>
    public Dictionary<System.Guid, Area> DisplacedNpcs { get; } = new();

    /// <summary>
    /// NPCs who have just walked in and have not yet been narrated, in arrival order. Drained by
    /// <c>NarrativeController.GenerateObservationsAsync</c>, which opens the next phase on them —
    /// ahead of a corpse and ahead of the standing-threat opener, because somebody crossing a room to
    /// reach you is both the newest thing to have happened and the most expensive to ignore.
    /// </summary>
    public List<SceneNpc> PendingArrivalObservations { get; } = new();

    /// <summary>
    /// Moves <paramref name="npc"/> into <paramref name="area"/> for the rest of the visit and queues
    /// them to open the next observation phase. Idempotent: somebody already standing there is not
    /// announced twice.
    /// </summary>
    public void DrawNpcTo(SceneNpc npc, Area area)
    {
        if (DisplacedNpcs.TryGetValue(npc.Id, out var already) && already.Id == area.Id) return;

        DisplacedNpcs[npc.Id] = area;
        if (!PendingArrivalObservations.Contains(npc)) PendingArrivalObservations.Add(npc);
        Console.WriteLine($"Scene: '{npc.Entity.DisplayName}' comes to {area.DisplayName} to see what the noise was.");
    }

    /// <summary>
    /// Adds a point of interest to an area at runtime and registers it (and its items) in the scene.
    /// Used for what the game spawns during play — corpses — rather than what a factory built.
    ///
    /// <para>The narration graph is built once, from the areas as the factory left them, so a PoI
    /// added here is not yet a node outcome and cannot be observed until
    /// <c>NarrativeController.SyncSpawnedObservations</c> reconciles it in — which happens before
    /// every observation phase and every thinking request.</para>
    ///
    /// <para>Unlike <c>SceneFactory</c>, this deliberately does <b>not</b> merge a same-named PoI:
    /// two dead pigs are two bodies, and they behave like any other pair of identically-named
    /// objects — the observation choice list collapses them to one representative per phase, and the
    /// ledger retires instances, so the second becomes observable once the first has been seen.</para>
    /// </summary>
    public void AddPointOfInterestToArea(Area area, PointOfInterest poi)
    {
        area.PointsOfInterest.Add(poi);

        RegisterElement(poi);
        foreach (var item in poi.Items)
            RegisterElement(item);

        // Virtual replay works on a throwaway scene; queueing an observation off it would make the
        // real narration open on a corpse that was only ever validated, never made.
        if (poi is Npc.Corpse.CorpsePointOfInterest corpse && !IsVirtualReplay)
            PendingCorpseObservations.Add(corpse);
    }

    // ── View (frontend output) ────────────────────────────────────────────────

    /// <summary>
    /// Produces a <see cref="SceneView"/> for the given point of view: the area, its PoIs and their
    /// items, the NPCs present at this hour, and the areas reachable from here.
    /// </summary>
    public SceneView View(PoV pov, PartyMember? actor = null)
    {
        var entries = new List<SceneViewEntry>();

        // 1. Current area
        entries.Add(BuildEntry(pov.Where, pov, actor));

        // 2. Points of interest in current area
        foreach (var poi in pov.Where.PointsOfInterest)
        {
            entries.Add(BuildEntry(poi, pov, actor));
            foreach (var itemElement in poi.Items)
                entries.Add(BuildEntry(itemElement, pov, actor));
        }

        // 3. NPCs present at current area and time
        foreach (var npc in GetNpcsAt(pov.Where, pov.When))
            entries.Add(BuildEntry(npc, pov, actor));

        // 4. Reachable areas (for movement verbs)
        foreach (var reachable in GetReachableAreas(pov.Where))
            entries.Add(BuildEntry(reachable, pov, actor));

        // Always include focused element if not already listed
        if (pov.Focus != null && entries.All(e => e.Source.Id != pov.Focus.Id))
            entries.Add(BuildEntry(pov.Focus, pov, actor));

        return new SceneView(pov.Where, pov.When, entries, pov.Focus);
    }

    private SceneViewEntry BuildEntry(Element element, PoV pov, PartyMember? actor = null)
    {
        var verbs    = new List<VerbView>();

        foreach (var verb in Verbs)
            verbs.AddRange(verb.ExpandViews(this, pov, element, actor));

        return new SceneViewEntry(element, verbs);
    }

    // ── Pending get-up transition (set by GetUpVerb on success) ─────────────

    /// <summary>
    /// Set to <c>true</c> by <see cref="GetUpTransitionOutcome"/>; consumed by
    /// <see cref="NarrativeController"/> on the next Continue click to signal that the
    /// Get-Up phase has ended and world travel should begin.
    /// </summary>
    public bool PendingGetUpTransition { get; set; }

    // ── Pending dialogue request (set by dialogue verbs) ─────────────────────

    /// <summary>
    /// Set by a dialogue verb's <c>Execute()</c>; consumed by <see cref="NarrativeController"/>
    /// on the next frame to start a dialogue session.
    /// </summary>
    public DialogueRequest? PendingDialogueRequest { get; set; }

    // ── Pending fight request (set by attack verb) ────────────────────────────

    /// <summary>
    /// Set by <c>AttackVerb.Execute()</c>; consumed by <see cref="NarrativeController"/>
    /// on the next frame to start a fight.
    /// </summary>
    public FightRequest? PendingFightRequest { get; set; }
}

/// <summary>
/// Set by a dialogue verb's Execute(); consumed by NarrativeController on the next frame
/// to start a dialogue session using the specified tree.
/// </summary>
public record DialogueRequest(NpcEntity Npc, string TreeId);

/// <summary>
/// Set by AttackVerb.Execute(); consumed by NarrativeController on the next frame
/// to start a fight against the specified NPC.
/// </summary>
public record FightRequest(NpcEntity Npc);

/// <summary>
/// Set by <c>RememberVerb.Execute()</c>; consumed by <see cref="NarrativeController"/>
/// on the next frame. When <see cref="NextReminescenceId"/> is "&lt;END&gt;" the controller
/// transitions out of the childhood-reminescence phase; otherwise it rebuilds the scene
/// as the next reminescence.
/// </summary>
public record ReminescenceTransitionRequest(
    string OriginReminescenceId,
    string FromReminescenceId,
    string NextReminescenceId,
    string FragmentName)
{
    public ReminescenceTransitionRequest(string FromReminescenceId, string NextReminescenceId, string FragmentName)
        : this(FromReminescenceId, FromReminescenceId, NextReminescenceId, FragmentName) { }
}
