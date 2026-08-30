using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The complete narration graph for a location: the network of nodes the player moves between.
///
/// <para>
/// Which NPCs inhabit which node at the current time of day is owned by the scene layer
/// (<c>SceneNpcPlacement</c>), not the graph: the graph only tracks the active
/// <see cref="CurrentPeriod"/> and exposes its nodes so the placement can reposition scene NPCs.
/// The graph remains valid until the player leaves the location.
/// </para>
/// </summary>
public class NarrationGraph
{
    private readonly IReadOnlyDictionary<string, NarrationNode> _allNodes;

    /// <summary>The entry node players start from in this location.</summary>
    public NarrationNode EntryNode { get; }

    /// <summary>All reachable nodes, keyed by NodeId.</summary>
    public IReadOnlyDictionary<string, NarrationNode> AllNodes => _allNodes;

    /// <summary>The time period set by the last <see cref="SetCurrentPeriod"/> call.</summary>
    public TimePeriod CurrentPeriod { get; private set; }

    public NarrationGraph(
        NarrationNode entryNode,
        IReadOnlyDictionary<string, NarrationNode> allNodes)
    {
        EntryNode = entryNode;
        _allNodes = allNodes;
    }

    /// <summary>
    /// Records the active time period. NPC repositioning for the period is driven separately by the
    /// scene layer (<c>SceneNpcPlacement</c>), which reads <see cref="CurrentPeriod"/>.
    /// </summary>
    public void SetCurrentPeriod(TimePeriod period) => CurrentPeriod = period;
}
