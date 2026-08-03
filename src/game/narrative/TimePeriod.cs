using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A coarse time-of-day period used by the NPC schedule system to determine
/// which nodes NPCs occupy when a narration session begins.
/// </summary>
public enum TimePeriod
{
    Dawn,
    Morning,
    Noon,
    Afternoon,
    Evening,
    Night,
}

public static class TimePeriodExtensions
{
    private static readonly TimePeriod[] All = (TimePeriod[])Enum.GetValues(typeof(TimePeriod));

    /// <summary>Picks a random time period using the supplied RNG.</summary>
    public static TimePeriod Random(Random rng) => All[rng.Next(All.Length)];

    /// <summary>Human-readable label (e.g. "Dawn", "Night").</summary>
    public static string Label(this TimePeriod p) => p.ToString();

    /// <summary>
    /// The period after this one, wrapping from Night back round to Dawn. The unit of every waiting
    /// verb: sitting on a bench moves you one of these forward.
    /// </summary>
    public static TimePeriod Next(this TimePeriod p) => All[((int)p + 1) % All.Length];

    /// <summary>
    /// The period <paramref name="steps"/> after this one, wrapping. Used by the verbs that wait for
    /// something to happen rather than for a fixed span, so they can walk the day forward one period
    /// at a time and stop when it does.
    /// </summary>
    public static TimePeriod Advance(this TimePeriod p, int steps)
        => All[(((int)p + steps) % All.Length + All.Length) % All.Length];

    /// <summary>How many periods there are in a day — the ceiling on how long any verb may wait.</summary>
    public static int PeriodsPerDay => All.Length;
}
