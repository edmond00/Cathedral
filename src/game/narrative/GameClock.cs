namespace Cathedral.Game.Narrative;

/// <summary>
/// The run's global in-game clock, measured in days.
///
/// <para>
/// The day is the only unit of time the game models. The world keeps its own calendar and has no
/// hours, weeks, months or years — anything that needs to express a longer span does so as a count
/// of days.
/// </para>
///
/// <para>
/// The clock only ever moves forward, and only when the fiction says a meaningful stretch of time
/// has passed: arriving at the end of a journey (<c>LocationTravelGameController.OnTravelCompleted</c>)
/// and finishing a work stint (<c>WorkMenuRenderer</c>). Narration, fights and dialogue are free.
/// </para>
///
/// <para>
/// It is a run-scoped global rather than a field on the protagonist because it is read far from the
/// party: scene factories timestamp item depletion with it, NPC spawns stamp birth times against it,
/// and the body panel measures every member's age from it. <see cref="Reset"/> must be called when a
/// new run begins — see <c>LocationTravelGameController.ResetGameState</c>.
/// </para>
/// </summary>
public static class GameClock
{
    /// <summary>Days elapsed since the run began. Zero at the start of a new run.</summary>
    public static double Days { get; private set; }

    /// <summary>Moves the clock forward. Negative or zero deltas are ignored — time never runs back.</summary>
    public static void Advance(double days)
    {
        if (days > 0) Days += days;
    }

    /// <summary>Rewinds the clock to zero for a new run.</summary>
    public static void Reset() => Days = 0;

    /// <summary>
    /// Sets the clock to a saved reading when a run is loaded. The one exception to "time never runs
    /// back" — restoring a save is not the clock moving, it is the clock being told which run it is
    /// in.
    ///
    /// <para>Mandatory for a save, and easy to underestimate: nothing stores an age, a wound's
    /// progress or an item depletion. All three are differences measured against this value, so a
    /// run reloaded with the clock at zero would have every character younger than they were, every
    /// wound freshly dealt, and every stripped bush regrown.</para>
    ///
    /// <para>Call only at a run boundary, never mid-run.</para>
    /// </summary>
    public static void Restore(double days) => Days = days < 0 ? 0 : days;
}
