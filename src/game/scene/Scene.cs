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
        => NpcSchedules.TryGetValue(npc.Id, out var schedule) ? schedule.GetArea(period) : null;

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

    // ── Dynamic spot management ───────────────────────────────────────────────

    /// <summary>
    /// Corpses spawned since the narration last opened a phase, in the order they fell. Drained by
    /// <c>NarrativeController.GenerateObservationsAsync</c>, which opens the following phase by
    /// observing exactly these — the body you just made is what you look at next.
    ///
    /// <para>Recorded here rather than by each caller because <see cref="AddSpotToArea"/> is the one
    /// door every corpse comes through: a slay verb applying <c>NpcSlaynOutcome</c> and a won fight
    /// spawning one body per dead enemy both end up in it.</para>
    /// </summary>
    public List<Npc.Corpse.CorpseSpot> PendingCorpseObservations { get; } = new();

    /// <summary>
    /// Adds a spot to an area at runtime and registers it (and its PoIs/items) in the scene.
    /// Used for temporary spots such as corpses that are spawned by verbs, not by the factory.
    ///
    /// <para>The narration graph is built once, so a spot added here is not yet a node outcome and
    /// cannot be observed until <c>NarrativeController.RefreshSceneVerbs</c> syncs it in — which it
    /// does before every observation phase and every thinking request.</para>
    /// </summary>
    public void AddSpotToArea(Area area, Spot spot)
    {
        area.Spots.Add(spot);
        RegisterSpot(spot);

        // Virtual replay works on a throwaway scene; queueing an observation off it would make the
        // real narration open on a corpse that was only ever validated, never made.
        if (spot is Npc.Corpse.CorpseSpot corpse && !IsVirtualReplay)
            PendingCorpseObservations.Add(corpse);
    }

    private void RegisterSpot(Spot spot)
    {
        RegisterElement(spot);
        foreach (var poi in spot.PointsOfInterest)
        {
            RegisterElement(poi);
            foreach (var item in poi.Items)
                RegisterElement(item);
        }
    }

    // ── View (frontend output) ────────────────────────────────────────────────

    /// <summary>
    /// Produces a <see cref="SceneView"/> for the given point of view.
    ///
    /// When <c>pov.InSpot != null</c> (player is inside a spot):
    ///   Shows the spot and its PoIs/items. Movement to areas is blocked; Leave verb is offered.
    ///
    /// Otherwise (player is in an area):
    ///   Shows the area, its PoIs/items, its spots, alive NPCs, and reachable areas.
    /// </summary>
    public SceneView View(PoV pov, Protagonist? actor = null)
    {
        var entries = new List<SceneViewEntry>();

        if (pov.InSpot != null)
        {
            // ── Inside a spot ──────────────────────────────────────────────
            entries.Add(BuildEntry(pov.InSpot, pov, actor));

            foreach (var poi in pov.InSpot.PointsOfInterest)
            {
                entries.Add(BuildEntry(poi, pov, actor));
                foreach (var itemElement in poi.Items)
                    entries.Add(BuildEntry(itemElement, pov, actor));
            }
        }
        else
        {
            // ── In an area ─────────────────────────────────────────────────

            // 1. Current area
            entries.Add(BuildEntry(pov.Where, pov, actor));

            // 2. Points of interest in current area
            foreach (var poi in pov.Where.PointsOfInterest)
            {
                entries.Add(BuildEntry(poi, pov, actor));
                foreach (var itemElement in poi.Items)
                    entries.Add(BuildEntry(itemElement, pov, actor));
            }

            // 3. Spots in current area (shown as enterable sub-locations)
            foreach (var spot in pov.Where.Spots)
                entries.Add(BuildEntry(spot, pov, actor));

            // 4. NPCs present at current area and time
            foreach (var npc in GetNpcsAt(pov.Where, pov.When))
                entries.Add(BuildEntry(npc, pov, actor));

            // 5. Reachable areas (for movement verbs)
            foreach (var reachable in GetReachableAreas(pov.Where))
                entries.Add(BuildEntry(reachable, pov, actor));
        }

        // Always include focused element if not already listed
        if (pov.Focus != null && entries.All(e => e.Source.Id != pov.Focus.Id))
            entries.Add(BuildEntry(pov.Focus, pov, actor));

        return new SceneView(pov.Where, pov.When, entries, pov.Focus);
    }

    private SceneViewEntry BuildEntry(Element element, PoV pov, Protagonist? actor = null)
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
