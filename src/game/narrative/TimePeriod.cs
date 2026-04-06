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
}
