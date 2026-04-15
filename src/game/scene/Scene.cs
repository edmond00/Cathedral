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

            var nodeId = schedule.GetNodeId(period);
            if (nodeId != null && string.Equals(nodeId, area.DisplayName, StringComparison.OrdinalIgnoreCase))
                result.Add(npc);
        }
        return result;
    }

    // ── All areas flattened ───────────────────────────────────────────────────

    /// <summary>Returns all areas across all sections.</summary>
    public List<Area> AllAreas => Sections.SelectMany(s => s.Areas).ToList();

    /// <summary>Gets an area by its UUID.</summary>
    public Area? GetArea(Guid id) => Elements.TryGetValue(id, out var el) && el is Area a ? a : null;

    // ── Dynamic spot management ───────────────────────────────────────────────

    /// <summary>
    /// Adds a spot to an area at runtime and registers it (and its PoIs/items) in the scene.
    /// Used for temporary spots such as corpses that are spawned by verbs, not by the factory.
    /// </summary>
    public void AddSpotToArea(Area area, Spot spot)
    {
        area.Spots.Add(spot);
        RegisterSpot(spot);
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
        var keywords = element.Keywords;
        var verbs    = new List<VerbView>();

        foreach (var verb in Verbs)
        {
            if (verb.IsPossible(this, pov, element, actor))
                verbs.Add(new VerbView(verb, verb.Verbatim(this, pov, element), element));
        }

        return new SceneViewEntry(element, keywords, verbs);
    }

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
