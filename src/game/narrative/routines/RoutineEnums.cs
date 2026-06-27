namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// The kind of scene <see cref="Cathedral.Game.Scene.Element"/> a routine step targets.
/// Used together with a stable string key to re-resolve the live element in a freshly
/// built scene at replay time (scene element GUIDs are not stable across rebuilds).
/// </summary>
public enum RoutineTargetKind
{
    None,
    Area,
    PointOfInterest,
    Spot,
    Npc,
    Item,
}

/// <summary>
/// What phase a routine step transitions into when it succeeds. Only the LAST step of a
/// routine may carry a non-<see cref="None"/> value; recording stops once a step triggers a
/// phase other than narration (fight/dialogue/…) because those phases cannot be recorded.
/// </summary>
public enum RoutinePhaseKind
{
    /// <summary>The step does not start a new phase (stays in / returns to travel after replay).</summary>
    None,

    /// <summary>The step starts a fresh narration phase (e.g. moving to a new area).</summary>
    Narration,

    /// <summary>The step starts a fight (e.g. attacking an NPC).</summary>
    Fight,

    /// <summary>The step starts a dialogue (e.g. meeting/speaking to an NPC).</summary>
    Dialogue,
}
