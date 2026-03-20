namespace Cathedral.Game.Narrative;

/// <summary>
/// An enemy combatant — a full <see cref="PartyMember"/> subclass used in combat.
/// Has a configurable display name and is initialised with a random selection of modiMentis.
/// Call <see cref="PartyMember.InitializeModiMentis"/> after construction to populate skills.
/// </summary>
public class EnemyCombatant : PartyMember
{
    private readonly string _displayName;
    public override string DisplayName => _displayName;

    /// <summary>
    /// Create an enemy with <paramref name="displayName"/>, using <paramref name="species"/> anatomy.
    /// After construction, call <see cref="PartyMember.InitializeModiMentis"/> to assign modiMentis.
    /// </summary>
    public EnemyCombatant(string displayName, Species species) : base(species)
    {
        _displayName = displayName;
    }
}
