namespace Cathedral.Game.Dialogue.Affinity;

/// <summary>
/// What kind of crime a witness saw. Derived from the verb at the moment of the catch and used to
/// word the confrontation — what the witness accuses you of, at every level of the tree.
///
/// <para>Not stored. A per-NPC criminal record existed here once and was written, escalated and
/// cleared without a single reader; the consequence of being caught is the confrontation and what it
/// leads to, which is a fight and a lasting enmity, not a label filed against your name.</para>
/// </summary>
public enum CriminalAffinityType
{
    /// <summary>No witnessed crime.</summary>
    None,

    /// <summary>Witnessed stealing an item from a private area or owned object.</summary>
    Thief,

    /// <summary>Witnessed trespassing in a private area without consent.</summary>
    Intruder,

    /// <summary>Witnessed attacking or slaying an innocent.</summary>
    Murderer,

    /// <summary>
    /// Witnessed wrecking someone's property. Distinct from <see cref="Thief"/> because nothing was
    /// taken and the damage stays visible: a witness to a broken loom is looking at the broken loom
    /// for as long as it stands there.
    /// </summary>
    Vandal,
}
