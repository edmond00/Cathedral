using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A definitively-placed NPC in a <see cref="NarrationGraph"/>.
/// The entity is spawned once by the graph factory (RNG decides inclusion at build time,
/// not at visit time). The schedule governs which node the NPC occupies at each
/// <see cref="TimePeriod"/> — null means absent.
/// </summary>
public class GraphNpc
{
    /// <summary>The runtime NPC instance (anatomy, modiMentis, optional dialogue).</summary>
    public NpcEntity Entity { get; }

    /// <summary>Per-period node placement for this NPC.</summary>
    public NpcSchedule Schedule { get; }

    public GraphNpc(NpcEntity entity, NpcSchedule schedule)
    {
        Entity   = entity;
        Schedule = schedule;
    }
}
