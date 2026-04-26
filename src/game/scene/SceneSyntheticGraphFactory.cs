using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Reminescence;
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

    protected override NarrationNode BuildNodes(Random rng, int locationId)
    {
        var firstArea = _scene.AllAreas.FirstOrDefault();
        if (firstArea == null)
            throw new InvalidOperationException("Scene has no areas — cannot build synthetic graph");

        // Create a synthetic NarrationNode for each area, keyed by Guid for graph wiring
        var byGuid = new Dictionary<Guid, SyntheticNarrationNode>();
        foreach (var area in _scene.AllAreas)
        {
            var node = CreateNodeForArea(area);
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
                var verbView = new VerbView(new MoveToAreaVerb(), toArea.TransitionDescription, toArea);
                var entry    = new SceneViewEntry(toArea, new List<VerbView> { verbView });
                fromNode.PossibleOutcomes.Add(new SyntheticAreaObservationObject(toArea, entry));
            }
        }

        return byGuid[firstArea.Id];
    }

    private SyntheticNarrationNode CreateNodeForArea(Area area)
    {
        SyntheticNarrationNode node;
        if (_scene.Phase == NarrationPhase.ChildhoodReminescence
            && _protagonist != null
            && _scene.CurrentReminescenceId != null
            && ReminescenceRegistry.Get(_scene.CurrentReminescenceId) is { } data)
        {
            node = new ReminescenceNarrationNode(
                area.DisplayName.ToLowerInvariant().Replace(' ', '_'),
                area.ContextDescription,
                area.TransitionDescription,
                area,
                _protagonist,
                data);
        }
        else
        {
            node = new SyntheticNarrationNode(
                area.DisplayName.ToLowerInvariant().Replace(' ', '_'),
                area.ContextDescription,
                area.TransitionDescription,
                area);
        }

        var pov = new PoV(area, TimePeriod.Morning);

        // Add points of interest as synthetic ObservationObjects
        foreach (var poi in area.PointsOfInterest)
        {
            var entry = new SceneViewEntry(poi,
                _scene.Verbs
                    .Where(v => v.IsPossible(_scene, pov, poi))
                    .Select(v => new VerbView(v, v.Verbatim(_scene, pov, poi), poi))
                    .ToList());

            // Build item sub-entries so item verbs (e.g. "grab the apple") fold into the PoI SubOutcomes.
            var itemSubEntries = poi.Items
                .Select(ie => new SceneViewEntry(ie,
                    _scene.Verbs
                        .Where(v => v.IsPossible(_scene, pov, ie))
                        .Select(v => new VerbView(v, v.Verbatim(_scene, pov, ie), ie))
                        .ToList()))
                .ToList();

            node.PossibleOutcomes.Add(new SyntheticObservationObject(poi, entry, itemSubEntries));
        }

        // Add spots as synthetic enterable sub-locations
        foreach (var spot in area.Spots)
        {
            var entry = new SceneViewEntry(spot,
                _scene.Verbs
                    .Where(v => v.IsPossible(_scene, pov, spot))
                    .Select(v => new VerbView(v, v.Verbatim(_scene, pov, spot), spot))
                    .ToList());

            node.PossibleOutcomes.Add(new SyntheticSpotObject(spot, entry));
        }

        // Add NPCs as ObservationObjects with verb SubOutcomes (attack, slay, meet, etc.)
        foreach (var npc in _scene.GetNpcsAt(area, pov.When))
        {
            var entry = new SceneViewEntry(npc,
                _scene.Verbs
                    .Where(v => v.IsPossible(_scene, pov, npc))
                    .Select(v => new VerbView(v, v.Verbatim(_scene, pov, npc), npc))
                    .ToList());

            node.PossibleOutcomes.Add(new SyntheticNpcObservationObject(npc, entry));
        }

        return node;
    }
}
