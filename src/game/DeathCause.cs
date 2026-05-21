namespace Cathedral.Game;

/// <summary>
/// Categorises the cause of the protagonist's death.
/// </summary>
public enum DeathCause
{
    /// <summary>Vital heat exhausted during travel.</summary>
    Starvation,

    /// <summary>Succumbed to wounds received in combat.</summary>
    Wounds,

    /// <summary>Died of old age.</summary>
    OldAge,
}

/// <summary>
/// Maps each <see cref="DeathCause"/> to the message shown on the death screen.
/// </summary>
public static class DeathMessages
{
    public static string GetTitle(DeathCause cause) => cause switch
    {
        DeathCause.Starvation => "STARVATION",
        DeathCause.Wounds     => "MORTAL WOUNDS",
        DeathCause.OldAge     => "OLD AGE",
        _                     => "UNKNOWN"
    };

    public static string GetMessage(DeathCause cause) => cause switch
    {
        DeathCause.Starvation =>
            "Your vital heat ran dry on the road.\n" +
            "The body, stripped of blood's warmth,\n" +
            "stopped along a nameless path.",

        DeathCause.Wounds =>
            "Your wounds were too many.\n" +
            "The blood that flowed outward\n" +
            "could not be called back.",

        DeathCause.OldAge =>
            "A long life, fully spent.\n" +
            "The organs ceased their labour,\n" +
            "one by one, in the quiet dark.",

        _ =>
            "The body gave out.\n" +
            "The cause is unknown."
    };
}
