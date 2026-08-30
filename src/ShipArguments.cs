using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral;

/// <summary>
/// Strips development command-line options out of a shipped build.
///
/// <para><b>Why a filter and not 49 conditionals.</b> The option surface is large and grows every
/// time a feature turns out to be hard to reach — which the project encourages. Guarding each
/// handler would mean remembering to guard the next one, and the failure is silent: a debug flag
/// left reachable does not announce itself, it just works. Filtering the array once, before
/// anything reads it, means every handler downstream is unreachable by construction and no new
/// flag can leak by being forgotten.</para>
///
/// <para><b>Allow-list, not deny-list</b> — the same reasoning as the packaging payload. A flag
/// added tomorrow is excluded from shipped builds automatically, because nobody has to remember to
/// add it to a list of things to remove.</para>
///
/// <para>Inert outside a shipped build: <see cref="Filter"/> returns its input unchanged unless
/// SHIP is defined, so development keeps every option.</para>
/// </summary>
public static class ShipArguments
{
    /// <summary>
    /// The options a shipped build still answers. Every one of them exists to get a player out of
    /// trouble, not to change the game:
    ///
    /// <list type="bullet">
    /// <item><c>--cpu</c>, <c>--gpu</c> — override the detected compute device when the chosen one
    /// misbehaves. The first thing to ask someone whose game will not start.</item>
    /// <item><c>--no-llm-probe</c> — skip hardware detection when it is what hangs.</item>
    /// <item><c>--silent</c> — open no audio device. A machine whose MIDI device fails to open is
    /// a real failure mode, and this is the way past it.</item>
    /// <item><c>--help</c> — lists exactly this set, not the development options.</item>
    /// </list>
    ///
    /// <para>None of these take a value. If one ever does, <see cref="Filter"/> needs to learn
    /// about it — see the comment there.</para>
    /// </summary>
    public static readonly string[] Allowed =
    {
        "--cpu", "--gpu", "--no-llm-probe", "--silent", "--help", "-h"
    };

    // There is deliberately NO escape hatch that re-enables the development options in a shipped
    // build. It was considered and rejected: the CLI scripts are built on --seed, --skip-childhood,
    // --observe-only, --location-type and a dozen more, so anything that made them usable would put
    // most of the development surface back, and it would drift every time a script needed something
    // new. Gameplay is verified on the development build, where that surface exists in full; what
    // the shipped artifact has to prove — that it starts, finds its files, loads the model and is
    // locked down — needs no options at all. See "Verifying a shipped build" in CLAUDE.md.

    /// <summary>Whether options are being restricted, i.e. whether this is a shipped build.</summary>
    public static bool IsRestricted =>
#if SHIP
        true;
#else
        false;
#endif

    /// <summary>
    /// Returns the options a shipped build should act on. Unchanged in a development build.
    ///
    /// <para>Call it <b>before anything reads the arguments</b>, including the master seed, or a
    /// stripped option will already have taken effect by the time it is removed.</para>
    /// </summary>
    public static string[] Filter(string[] args)
    {
        if (!IsRestricted || args.Length == 0) return args;

        var allowed = new HashSet<string>(Allowed, StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>();
        bool previousWasAllowedFlag = false;
        int dropped = 0;

        foreach (var arg in args)
        {
            if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                if (allowed.Contains(arg))
                {
                    kept.Add(arg);
                    previousWasAllowedFlag = true;
                }
                else
                {
                    dropped++;
                    previousWasAllowedFlag = false;
                }
            }
            else
            {
                // A bare token: the value of whichever flag preceded it. Keep it only if that flag
                // survived, so that dropping "--seed 42" drops the 42 as well rather than leaving
                // it behind as a stray argument.
                if (previousWasAllowedFlag) kept.Add(arg);
                else dropped++;
            }
        }

        if (dropped > 0)
        {
            // Only visible when launched from a terminal — a shipped build owns no console
            // otherwise. Silence would be worse than noise here: somebody following stale
            // instructions needs to know why nothing happened.
            Console.WriteLine($"Ignored {dropped} development option(s); this build accepts: {string.Join(" ", Allowed)}");
        }

        return kept.ToArray();
    }
}
