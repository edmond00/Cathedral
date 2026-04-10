using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

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

    public SceneSyntheticGraphFactory(Cathedral.Game.Scene.Scene scene, int locationId)
        : base(sessionPath: null)
    {
        _scene      = scene;
        _locationId = locationId;
    }

    protected override NarrationNode BuildNodes(Random rng, int locationId)
    {
        var firstArea = _scene.AllAreas.FirstOrDefault();
        if (firstArea == null)
            throw new InvalidOperationException("Scene has no areas — cannot build synthetic graph");

        // Create a synthetic NarrationNode for each area
        var areaNodes = new Dictionary<Guid, SyntheticNarrationNode>();
        foreach (var area in _scene.AllAreas)
        {
            var node = CreateNodeForArea(area);
            areaNodes[area.Id] = node;
        }

        // Connect nodes based on scene's AreaGraph
        foreach (var (fromId, toIds) in _scene.AreaGraph)
        {
            if (!areaNodes.TryGetValue(fromId, out var fromNode)) continue;
            foreach (var toId in toIds)
            {
                if (areaNodes.TryGetValue(toId, out var toNode))
                    fromNode.PossibleOutcomes.Add(toNode);
            }
        }

        return areaNodes[firstArea.Id];
    }

    private SyntheticNarrationNode CreateNodeForArea(Area area)
    {
        var node = new SyntheticNarrationNode(
            area.DisplayName.ToLowerInvariant().Replace(' ', '_'),
            area.ContextDescription,
            area.TransitionDescription,
            area.Keywords,
            area);

        var pov = new PoV(area, TimePeriod.Morning);

        // Add points of interest as synthetic ObservationObjects
        foreach (var poi in area.PointsOfInterest)
        {
            var entry = new SceneViewEntry(poi, poi.Keywords,
                _scene.Verbs
                    .Where(v => v.IsPossible(_scene, pov, poi))
                    .Select(v => new VerbView(v, v.Verbatim(_scene, pov, poi), poi))
                    .ToList());

            node.PossibleOutcomes.Add(new SyntheticObservationObject(poi, entry));
        }

        // Add spots as synthetic enterable sub-locations
        foreach (var spot in area.Spots)
        {
            var entry = new SceneViewEntry(spot, spot.Keywords,
                _scene.Verbs
                    .Where(v => v.IsPossible(_scene, pov, spot))
                    .Select(v => new VerbView(v, v.Verbatim(_scene, pov, spot), spot))
                    .ToList());

            node.PossibleOutcomes.Add(new SyntheticSpotObject(spot, entry));
        }

        // Add NPCs present at this area as NpcElementOutcomes
        foreach (var npc in _scene.GetNpcsAt(area, pov.When))
        {
            var entry = new SceneViewEntry(npc, npc.Keywords,
                _scene.Verbs
                    .Where(v => v.IsPossible(_scene, pov, npc))
                    .Select(v => new VerbView(v, v.Verbatim(_scene, pov, npc), npc))
                    .ToList());

            node.PossibleOutcomes.Add(new NpcElementOutcome(npc, entry));
        }

        return node;
    }
}
