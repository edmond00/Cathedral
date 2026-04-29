namespace Cathedral.Game.Narrative;

/// <summary>
/// The kind of narration the protagonist is currently engaged in.
/// Different phases share the observation/thinking/action structure but apply
/// different prompt contexts and rules (critic skipping, noetic-point cost, etc.).
/// </summary>
public enum NarrationPhase
{
    /// <summary>Normal scene exploration at a real-world location.</summary>
    Exploration,

    /// <summary>
    /// Childhood reminescence: the protagonist is sitting at the foot of a tree
    /// recalling fuzzy memories. Fragments are POI-like observations and the only
    /// available action is REMEMBER (auto-success, no critic, no noetic cost).
    /// </summary>
    ChildhoodReminescence,

    /// <summary>
    /// Get Up: the protagonist rests exhausted under a lone tree on a plain, body spent after
    /// the reminescence. Observations focus on physical and emotional exhaustion. The only
    /// available action is GET UP (difficulty forced to 1, no critic malus, no noetic cost).
    /// Failure loops back to the same scene; success transitions to world travel.
    /// </summary>
    GetUp
}
