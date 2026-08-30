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

    /// <summary>
    /// Coarse identity key — ReferenceLemma for Area/PoI, ItemId for items, display name for NPCs.
    /// For Area/PoI this only <i>categorises</i> (every path is "path", every door "door"), so
    /// <see cref="RoutineTargetResolver"/> matches <see cref="DisplayName"/> first and falls back here.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Display name captured at record time. For Area/PoI targets this is the <b>identifying</b> field
    /// the resolver matches on, not merely UI text — areas are uniquely named scene-wide and
    /// <c>SceneFactory</c> merges same-named PoIs within an area.
    /// </summary>
    public string DisplayName { get; set; } = "";

    public RoutineTargetRef() { }

    public RoutineTargetRef(RoutineTargetKind kind, string key, string displayName)
    {
        Kind        = kind;
        Key         = key;
        DisplayName = displayName;
    }
}
