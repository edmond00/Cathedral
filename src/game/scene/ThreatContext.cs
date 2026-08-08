using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// How a nearby enemy can threaten the player during an action.
/// </summary>
public enum ThreatLevel
{
    /// <summary>No enemy is close enough to pose a threat.</summary>
    None,

    /// <summary>An enemy is in an adjacent area — noise from a failed action might draw them.</summary>
    Audio,

    /// <summary>An enemy is in the same area — any action gives them an opportunity to attack.</summary>
    Visual,
}

/// <summary>
/// Captures the nearest enemy and how they threaten the player right now.
/// Computed once per action and threaded through the evaluation pipeline.
/// </summary>
public record ThreatContext(ThreatLevel Level, NpcEntity? Threat)
{
    public static readonly ThreatContext None = new(ThreatLevel.None, null);
}
