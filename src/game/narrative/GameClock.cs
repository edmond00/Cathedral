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
}
