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
    ChildhoodReminescence
}
