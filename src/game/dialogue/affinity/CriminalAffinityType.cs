namespace Cathedral.Game.Dialogue.Affinity;

/// <summary>
/// Tracks what kind of crime a party member has committed as witnessed by an NPC.
/// Stored per-NPC in <see cref="AffinityTable"/> so witnesses remember who did what.
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
}
