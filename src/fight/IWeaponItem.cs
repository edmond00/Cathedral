namespace Cathedral.Fight;

/// <summary>
/// Marker interface for items that can be used as a weapon medium in fighting skills.
/// Classes implementing this should be weapon-type <see cref="Cathedral.Game.Narrative.Item"/> subclasses.
/// </summary>
public interface IWeaponItem
{
    /// <summary>Weapon proficiency level — number of bonus dice added when this weapon is the active medium.</summary>
    int Level { get; }

    /// <summary>
    /// Single weapon-type category key (e.g. "sword", "axe", "bow", "shield").
    /// Used to match the weapon to fighting skills and to label the action-menu tab.
    /// </summary>
    string WeaponCategory { get; }
}
