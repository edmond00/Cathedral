namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// A stable, rebuild-independent reference to a scene element targeted by a routine step.
///
/// Scene <see cref="Cathedral.Game.Scene.Element.Id"/> values are fresh GUIDs assigned each
/// time a scene is built from its factory, so they cannot be persisted. Instead a routine
/// records the element's <see cref="Kind"/> plus a stable string <see cref="Key"/>
/// (e.g. <c>Area.ReferenceLemma</c>), and <see cref="RoutineTargetResolver"/> re-resolves the
/// live element in a freshly built scene at replay time.
/// </summary>
public class RoutineTargetRef
{
    public RoutineTargetKind Kind { get; set; } = RoutineTargetKind.None;

    /// <summary>Stable identity key — ReferenceLemma for Area/PoI, display name for NPCs.</summary>
    public string Key { get; set; } = "";

    /// <summary>Human-readable display name captured at record time (for UI / debugging).</summary>
    public string DisplayName { get; set; } = "";

    public RoutineTargetRef() { }

    public RoutineTargetRef(RoutineTargetKind kind, string key, string displayName)
    {
        Kind        = kind;
        Key         = key;
        DisplayName = displayName;
    }
}
