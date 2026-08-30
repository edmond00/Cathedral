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

    /// <summary>
    /// Sleeper observations currently standing in for a person and their bed, keyed by NPC.
    /// Held so the swap can be undone: each entry knows which bed it displaced and which area it
    /// displaced it from.
    /// </summary>
    private readonly Dictionary<System.Guid, SleepingNpcPointOfInterest> _sleepers = new();

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
        // Put every bed back before deciding anything. Cheaper and far safer than working out which
        // sleepers changed: the hour may have moved by one period or by five, somebody may have been
        // woken, and a stale sleeper left behind would be a person visible in two places at once.
        ClearSleepers();

        foreach (var node in _nodes)
            node.PossibleOutcomes.RemoveAll(o => o is SyntheticNpcObservationObject);

        foreach (var node in _nodes)
            foreach (var npc in _scene.GetNpcsAt(node.Area!, period))
            {
                if (TryPutToBed(npc, node.Area!, period)) continue;

                if (_obsByNpcId.TryGetValue(npc.Id, out var obs))
                    node.PossibleOutcomes.Add(obs);
            }
    }

    /// <summary>
    /// Replaces an NPC and the bed they are lying in with a single sleeping-person observation, when
    /// they are in fact asleep. Returns true when the swap happened, so the caller knows not to place
    /// the ordinary NPC observation as well.
    ///
    /// <para>The bed is taken out of the area's PoI list rather than merely hidden, so it stops being
    /// observable and its verbs stop being offered — searching a bed with its owner in it is not the
    /// same act as searching an empty one, and the sleeper observation carries the verbs that do
    /// apply.</para>
    /// </summary>
    private bool TryPutToBed(SceneNpc npc, Area area, TimePeriod period)
    {
        var probe = new PoV(area, period);
        if (!npc.IsSleeping(_scene, probe)) return false;

        var bed = Building.BuildingRooms.BedsIn(area).FirstOrDefault();
        if (bed == null) return false;   // IsSleeping already checks this; belt and braces

        var sleeper = new SleepingNpcPointOfInterest(npc, bed);
        area.PointsOfInterest.Remove(bed);
        area.PointsOfInterest.Add(sleeper);
        sleeper.Register(_scene);

        _sleepers[npc.Id] = sleeper;
        return true;
    }

    /// <summary>Undoes every sleeper swap, putting the beds back where they were.</summary>
    private void ClearSleepers()
    {
        foreach (var sleeper in _sleepers.Values)
            foreach (var area in _scene.AllAreas)
            {
                int index = area.PointsOfInterest.IndexOf(sleeper);
                if (index >= 0) area.PointsOfInterest[index] = sleeper.Bed;
            }

        _sleepers.Clear();
    }
}
