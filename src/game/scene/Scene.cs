using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
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

    /// <summary>
    /// Registers an element in this scene's dictionary.
    /// Called by <see cref="Element.Register"/>.
    /// </summary>
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

    /// <summary>Returns NPCs present at the given area during the given time period.</summary>
    public List<SceneNpc> GetNpcsAt(Area area, TimePeriod period)
    {
        var result = new List<SceneNpc>();
        foreach (var npc in Npcs)
        {
            if (!npc.IsAlive) continue;
            if (!NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;

            var nodeId = schedule.GetNodeId(period);
            // NpcSchedule uses string nodeIds — match against area DisplayName (lowered)
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

    // ── View (frontend output) ────────────────────────────────────────────────

    /// <summary>
    /// Produces a <see cref="SceneView"/> for the given point of view:
    /// only elements and verbs relevant to the current area, time, and focus.
    /// </summary>
    public SceneView View(PoV pov)
    {
        var entries = new List<SceneViewEntry>();

        // 1. Current area itself
        entries.Add(BuildEntry(pov.Where, pov));

        // 2. Points of interest in current area
        foreach (var poi in pov.Where.PointsOfInterest)
        {
            entries.Add(BuildEntry(poi, pov));

            // Items within each point of interest (as ItemElement wrappers)
            foreach (var itemElement in poi.Items)
                entries.Add(BuildEntry(itemElement, pov));
        }

        // 3. NPCs present at current area and time
        foreach (var npc in GetNpcsAt(pov.Where, pov.When))
            entries.Add(BuildEntry(npc, pov));

        // 4. Reachable areas (for movement verbs)
        foreach (var reachable in GetReachableAreas(pov.Where))
            entries.Add(BuildEntry(reachable, pov));

        // 5. If there's a focused element not already in the list, add it
        if (pov.Focus != null && entries.All(e => e.Source.Id != pov.Focus.Id))
            entries.Add(BuildEntry(pov.Focus, pov));

        return new SceneView(pov.Where, pov.When, entries, pov.Focus);
    }

    private SceneViewEntry BuildEntry(Element element, PoV pov)
    {
        var keywords = element.Keywords;
        var verbs = new List<VerbView>();

        foreach (var verb in Verbs)
        {
            // Check each potential target
            if (verb.IsPossible(this, pov, element))
            {
                var verbatim = verb.Verbatim(this, pov, element);
                verbs.Add(new VerbView(verb, verbatim, element));
            }
        }

        return new SceneViewEntry(element, keywords, verbs);
    }
}
