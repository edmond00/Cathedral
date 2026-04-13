namespace Cathedral.Game.Narrative;

/// <summary>
/// Indicates the ethical alignment of a <see cref="ModusMentis"/> — used during
/// illegal-action plausibility checks to weight which skills support or oppose an action.
/// </summary>
public enum MoralLevel
{
    /// <summary>Openly amoral — supports deception, theft, violence.</summary>
    Low,

    /// <summary>Pragmatic — neither actively encourages nor condemns minor transgressions.</summary>
    Medium,

    /// <summary>Principled — resists illegal or dishonest actions.</summary>
    High,
}
