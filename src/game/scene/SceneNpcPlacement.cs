using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// Period-aware placement of a scene's NPCs into the synthetic narration graph.
///
/// <para>One <see cref="SyntheticNpcObservationObject"/> is built per <see cref="SceneNpc"/> up
/// front and moved between area nodes each time the time period changes, following the NPC's
/// schedule (via <see cref="Scene.GetNpcsAt"/>). An NPC that is scheduled elsewhere — or absent —
/// for the current period simply is not present in a node, so it cannot be observed there: a farm
/// hand who goes home at Night is not in the field node at Night.</para>
///
/// <para>Presence here is decided by the exact same <see cref="Scene.GetNpcsAt"/> query the NPC
/// verb gates use, so a placed NPC always has its verbs (meet / attack / …) survive the subsequent
/// <c>RefreshSceneVerbs</c> re-expansion — the two can never disagree. Object identity is preserved
/// across repositions (the same instances are moved, not rebuilt), so keyword→outcome maps and any
/// stamped contextual labels stay valid.</para>
/// </summary>
public class SceneNpcPlacement
{
    private readonly Scene _scene;
    private readonly List<SyntheticNarrationNode> _nodes;
    private readonly Dictionary<System.Guid, SyntheticNpcObservationObject> _obsByNpcId;

    public SceneNpcPlacement(Scene scene, IEnumerable<NarrationNode> nodes)
    {
        _scene = scene;
        _nodes = nodes.OfType<SyntheticNarrationNode>().Where(n => n.Area != null).ToList();
        _obsByNpcId = _scene.Npcs.ToDictionary(
            npc => npc.Id,
            npc => new SyntheticNpcObservationObject(npc));
    }

    /// <summary>
    /// Removes every scene NPC observation from all nodes, then re-inserts each NPC that is present
    /// at <paramref name="period"/> into its scheduled area's node. Dead and absent NPCs are left
    /// out (they are filtered by <see cref="Scene.GetNpcsAt"/>). Safe to call repeatedly — it is the
    /// single entry point for both a period change and after a fight removes a slain NPC.
    /// </summary>
    public void PlaceForPeriod(TimePeriod period)
    {
        foreach (var node in _nodes)
            node.PossibleOutcomes.RemoveAll(o => o is SyntheticNpcObservationObject);

        foreach (var node in _nodes)
            foreach (var npc in _scene.GetNpcsAt(node.Area!, period))
                if (_obsByNpcId.TryGetValue(npc.Id, out var obs))
                    node.PossibleOutcomes.Add(obs);
    }
}
