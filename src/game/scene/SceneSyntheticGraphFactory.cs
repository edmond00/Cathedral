using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Reminescence;
using Cathedral.Game.Scene.GetUp;
using Cathedral.Game.Scene.Reminescence;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// A synthetic <see cref="NarrationGraphFactory"/> that wraps a <see cref="Scene"/>
/// to produce a <see cref="NarrationGraph"/> compatible with the existing LLM pipeline.
/// The graph's entry node is a synthetic NarrationNode derived from the scene's first area.
/// </summary>
public class SceneSyntheticGraphFactory : NarrationGraphFactory
{
    private readonly Cathedral.Game.Scene.Scene _scene;
    private readonly int _locationId;
    private readonly Dictionary<string, NarrationNode> _areaNodes = new();
    private readonly Protagonist? _protagonist;

    public SceneSyntheticGraphFactory(Cathedral.Game.Scene.Scene scene, int locationId, Protagonist? protagonist = null)
        : base(sessionPath: null)
    {
        _scene      = scene;
        _locationId = locationId;
        _protagonist = protagonist;
    }

    protected override IReadOnlyDictionary<string, NarrationNode> CollectAllNodes(NarrationNode entry)
        => _areaNodes;

    /// <summary>
    /// The area narration opens in: the first one the factory built, or — under
    /// <c>--start-area &lt;name&gt;</c> — the first whose display name contains that name. Inert at its
    /// default, and inert again when nothing matches, so a location without the named room behaves
    /// exactly as it always did. The PoV is built from this same helper, so the two cannot disagree.
    /// </summary>
    public static Area? ResolveEntryArea(Cathedral.Game.Scene.Scene scene)
    {
        var wanted = Config.Debug.StartArea;
        if (!string.IsNullOrWhiteSpace(wanted))
        {
            // Exact name first, substring only as a fallback — the same
            // whole-thing-before-partial rule --observe-only uses, and for the same reason. Rooms
            // nest by name ("Alehouse", "Alehouse Store", "Alehouse Bedroom"), so a plain substring
            // match on "Alehouse" lands in whichever the factory happened to build first. A test
            // aimed at the taproom quietly opened in the storeroom, found none of the people it
            // named, and went on to exercise something else entirely.
            var match = scene.AllAreas.FirstOrDefault(
                            a => a.DisplayName.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                     ?? scene.AllAreas.FirstOrDefault(
                            a => a.DisplayName.Contains(wanted, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            Cathedral.Game.DebugFlagAudit.Miss("--start-area", wanted, "wherever the factory opened");
            Console.Error.WriteLine($"[debug]   areas here: {string.Join(", ", scene.AllAreas.Select(a => a.DisplayName))}");
        }
        return scene.AllAreas.FirstOrDefault();
    }

    protected override NarrationNode BuildNodes(Random rng, int locationId)
    {
        var firstArea = ResolveEntryArea(_scene);
        if (firstArea == null)
            throw new InvalidOperationException("Scene has no areas — cannot build synthetic graph");

        // Create a synthetic NarrationNode for each area, keyed by Guid for graph wiring.
        // Node ids are display-name slugs and are only unique by convention, so they are
        // disambiguated here: a duplicate would overwrite the earlier node in _areaNodes and leave a
        // whole room with no node — unplaceable by SceneNpcPlacement and unreachable by transition.
        var byGuid = new Dictionary<Guid, SyntheticNarrationNode>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var area in _scene.AllAreas)
        {
            var node = CreateNodeForArea(area, UniqueNodeId(area, usedIds));
            byGuid[area.Id] = node;
            _areaNodes[node.NodeId] = node;
        }

        // Wire area-to-area transitions via MoveToAreaVerb (no verb bypass)
        foreach (var (fromId, toIds) in _scene.AreaGraph)
        {
            if (!byGuid.TryGetValue(fromId, out var fromNode)) continue;
            foreach (var toId in toIds)
            {
                if (!byGuid.TryGetValue(toId, out var toNode) || toNode.Area == null) continue;
                var toArea   = toNode.Area;
                var verbView = new VerbAction(new MoveToAreaVerb(), toArea.TransitionDescription, toArea);
                var entry    = new SceneViewEntry(toArea, new List<VerbAction> { verbView });
                fromNode.PossibleOutcomes.Add(new SyntheticAreaObservationObject(toArea, entry));
            }
        }

        return byGuid[firstArea.Id];
    }

    /// <summary>
    /// A scene-unique node id for <paramref name="area"/>: the display-name slug, suffixed on
    /// collision. Two buildings each holding a "Hall" would otherwise produce one id.
    /// </summary>
    private static string UniqueNodeId(Area area, HashSet<string> used)
    {
        var baseId = area.DisplayName.ToLowerInvariant().Replace(' ', '_');
        var id     = baseId;
        for (int n = 2; !used.Add(id); n++)
            id = $"{baseId}_{n}";
        return id;
    }

    private SyntheticNarrationNode CreateNodeForArea(Area area, string nodeId)
    {
        SyntheticNarrationNode node;
        if (_scene.Phase == NarrationPhase.ChildhoodReminescence
            && _protagonist != null
            && _scene.CurrentReminescenceId != null
            && ReminescenceRegistry.Get(_scene.CurrentReminescenceId) is { } data)
        {
            node = new ReminescenceNarrationNode(
                nodeId,
                area.ContextDescription,
                area.TransitionDescription,
                area,
                _protagonist,
                data);
        }
        else if (_scene.Phase == NarrationPhase.GetUp)
        {
            node = new GetUpNarrationNode(
                nodeId,
                area.ContextDescription,
                area.TransitionDescription,
                area);
        }
        else
        {
            node = new SyntheticNarrationNode(
                nodeId,
                area.ContextDescription,
                area.TransitionDescription,
                area);
        }

        // Any period serves for this initial expansion; RefreshSceneVerbs re-gates every verb live at
        // the real period before anything is shown, and stamps that period onto the observations too.
        // Doors ARE period-dependent (an entry door is shut at night), so this bake is provisional —
        // do not read verb availability off it. NPC observation objects are NOT baked here: they are
        // placed per period by SceneNpcPlacement so a scene only shows the NPCs actually present now.
        var pov = new PoV(area, TimePeriod.Morning);

        // Add points of interest as synthetic ObservationObjects
        foreach (var poi in area.PointsOfInterest)
        {
            var entry = new SceneViewEntry(poi,
                _scene.Verbs
                    .SelectMany(v => v.ExpandViews(_scene, pov, poi))
                    .ToList());

            // Build item sub-entries so item verbs (e.g. "grab the apple") fold into the PoI SubOutcomes.
            var itemSubEntries = poi.Items
                .Select(ie => new SceneViewEntry(ie,
                    _scene.Verbs
                        .SelectMany(v => v.ExpandViews(_scene, pov, ie))
                        .ToList()))
                .ToList();

            // The viewing area is what lets a connector PoI describe itself per side: a door lives in
            // both areas' PoI lists, so this is the only thing distinguishing the two observations.
            node.PossibleOutcomes.Add(new SyntheticObservationObject(poi, entry, itemSubEntries, area));
        }

        // Anything the game spawns later — a corpse — is reconciled in by
        // NarrativeController.SyncSpawnedObservations, which runs before every observation phase.

        return node;
    }
}
