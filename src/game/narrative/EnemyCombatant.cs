namespace Cathedral.Game.Narrative;

/// <summary>
/// An enemy combatant — a full <see cref="PartyMember"/> subclass used in combat.
/// Has a configurable display name and is initialised with a random selection of modiMentis.
/// Call <see cref="PartyMember.InitializeModiMentis"/> after construction to populate skills.
/// </summary>
public class EnemyCombatant : PartyMember
{
    private string _displayName;
    public override string DisplayName => _displayName;

    /// <summary>
    /// Create an enemy with <paramref name="displayName"/>, using <paramref name="species"/> anatomy.
    /// After construction, call <see cref="PartyMember.InitializeModiMentis"/> to assign modiMentis.
    /// </summary>
    public EnemyCombatant(string displayName, Species species) : base(species)
    {
        _displayName = displayName;
    }

    /// <summary>
    /// Overwrite the display name after construction. Used by the name generator, which must build
    /// the combatant first (to read its seeded gender) before it can produce a gendered name.
    /// </summary>
    public void SetDisplayName(string displayName) => _displayName = displayName;

    /// <summary>
    /// The archetype this combatant was generated from, and its stable per-NPC id — both empty for a
    /// combatant that was never a scene NPC.
    ///
    /// <para>Recruiting a companion moves the <see cref="EnemyCombatant"/> into the party and drops
    /// the <c>NpcEntity</c> that wrapped it, which is where both ids lived. Without them a companion
    /// is anonymous the moment they join: they cannot be matched against a location's
    /// <c>DepartedNpcs</c>, and a save cannot say who they were, only what their body is.</para>
    ///
    /// <para>Carried, not enforced. Nothing rebuilds a companion from the archetype — everything
    /// since recruitment (experience, wounds, what they are carrying) has left that seed behind.</para>
    /// </summary>
    public string ArchetypeId { get; set; } = "";

    /// <inheritdoc cref="ArchetypeId"/>
    public string PersistentId { get; set; } = "";
}
