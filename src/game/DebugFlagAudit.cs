using System;
using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Records a narrowing debug flag that did not narrow anything.
///
/// <para><c>--start-area</c>, <c>--observe-only</c> and <c>--goal-only</c> all exist to make one
/// specific thing reachable, and all three <b>fall back</b> when they match nothing: open where the
/// factory did, offer the whole scene, draw from every goal. That is the right behaviour for a person
/// poking at the game — a flag that aborted the run because a room was named differently would be
/// worse than useless — and it is exactly wrong for a test, which then proceeds to exercise something
/// other than what it was written for and reports whatever that did.</para>
///
/// <para>It is not a hypothetical: of the first generated verb suite, <b>59 of the scripts that
/// reported success never ran their verb</b>. <c>--observe-only</c> matched nothing, the phase opened
/// on a path connector instead, the character followed the path, and <c>expect SUCCESS</c> — which
/// matches the outcome banner of <i>any</i> action — passed. The suite was green and testing almost
/// nothing, and nothing anywhere said so.</para>
///
/// <para>So the fallback stays, and the miss is recorded. Under <c>--cli</c> the driver fails the run
/// on any recorded miss, which turns a silently-wrong test into a loudly-red one naming the flag and
/// what it wanted. Outside <c>--cli</c> this is a log line and nothing more.</para>
/// </summary>
public static class DebugFlagAudit
{
    private static readonly List<string> _misses = new();

    /// <summary>Every miss recorded this run, newest last.</summary>
    public static IReadOnlyList<string> Misses => _misses;

    /// <summary>
    /// Records that <paramref name="flag"/> asked for <paramref name="wanted"/>, found nothing, and
    /// fell back to <paramref name="fellBackTo"/>. Also prints it, as these always did.
    /// </summary>
    public static void Miss(string flag, string wanted, string fellBackTo)
    {
        var line = $"{flag} '{wanted}': nothing matched — fell back to {fellBackTo}";
        _misses.Add(line);
        Console.WriteLine($"[debug] {line}");
    }

    /// <summary>Forgets every recorded miss. For a driver that wants to scope them to one script.</summary>
    public static void Clear() => _misses.Clear();
}
