using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The complete narration graph for a location: the node network plus the NPCs
/// that inhabit it according to a time-of-day schedule.
///
/// <para>
/// After construction, call <see cref="TimeUpdate"/> once (at the start of a
/// narration session) to inject <see cref="NpcObservationObject"/> instances into
/// the correct nodes for the current <see cref="TimePeriod"/>.  The graph remains
/// valid until the player leaves the location.
/// </para>
///
/// <para>
/// When an NPC dies in combat, call <see cref="NotifyNpcDead"/> to immediately
/// remove its observation from all nodes.
/// </para>
/// </summary>
public class NarrationGraph
{
    private readonly IReadOnlyList<GraphNpc> _npcs;
    private readonly IReadOnlyDictionary<string, NarrationNode> _allNodes;

    /// <summary>The entry node players start from in this location.</summary>
    public NarrationNode EntryNode { get; }

    /// <summary>All NPCs registered in this graph (alive or dead).</summary>
    public IReadOnlyList<GraphNpc> Npcs => _npcs;

    /// <summary>All reachable nodes, keyed by NodeId.</summary>
    public IReadOnlyDictionary<string, NarrationNode> AllNodes => _allNodes;

    /// <summary>The time period set by the last <see cref="TimeUpdate"/> call.</summary>
    public TimePeriod CurrentPeriod { get; private set; }

    public NarrationGraph(
        NarrationNode entryNode,
        IReadOnlyList<GraphNpc> npcs,
        IReadOnlyDictionary<string, NarrationNode> allNodes)
    {
        EntryNode = entryNode;
        _npcs     = npcs;
        _allNodes = allNodes;
    }

    // ── Time simulation ───────────────────────────────────────────────────────

    /// <summary>
    /// Repositions all NPCs according to the given time period.
    /// Removes any existing <see cref="NpcObservationObject"/> entries from every
    /// node, then re-inserts them for NPCs that are scheduled to be present.
    /// Dead NPCs (HP ≤ 0) are silently skipped.
    /// </summary>
    public void TimeUpdate(TimePeriod period)
    {
        // Remove all existing NPC observations from every node
        foreach (var node in _allNodes.Values)
            node.PossibleOutcomes.RemoveAll(o => o is NpcObservationObject);

        // Re-insert based on new period
        foreach (var gnpc in _npcs)
        {
            if (!gnpc.Entity.IsAlive) continue;

            var nodeId = gnpc.Schedule.GetNodeId(period);
            if (nodeId == null) continue;               // absent this period

            if (_allNodes.TryGetValue(nodeId, out var node))
                node.PossibleOutcomes.Add(new NpcObservationObject(gnpc.Entity));
        }

        CurrentPeriod = period;
        Console.WriteLine($"NarrationGraph: TimeUpdate({period}) — {CountActiveNpcs(period)} NPC(s) placed");
    }

    // ── NPC death ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately removes the dead NPC's observation from all nodes.
    /// Called after a fight where the NPC's HP reached zero.
    /// </summary>
    public void NotifyNpcDead(NpcEntity npc)
    {
        foreach (var node in _allNodes.Values)
            node.PossibleOutcomes.RemoveAll(o =>
                o is NpcObservationObject obs && obs.Npc.NpcId == npc.NpcId);

        Console.WriteLine($"NarrationGraph: NPC '{npc.DisplayName}' removed from all nodes");
    }

    // ── Query helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all GraphNpcs that are scheduled to appear at <paramref name="nodeId"/>
    /// during at least one time period.  Used by the debug viewer.
    /// </summary>
    public IEnumerable<GraphNpc> GetNpcsScheduledForNode(string nodeId)
        => _npcs.Where(n => n.Schedule.ActivePeriods.Any(p => p.NodeId == nodeId));

    private int CountActiveNpcs(TimePeriod period)
        => _npcs.Count(n => n.Entity.IsAlive && n.Schedule.GetNodeId(period) != null);
}
