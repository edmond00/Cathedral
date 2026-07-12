using System;

namespace Cathedral;

/// <summary>
/// Central deterministic randomness source for a single playthrough.
///
/// One master seed controls every gameplay RNG that is <b>not</b> already derived
/// from stable world data. Systems seeded from an NPC id, a location id or a
/// fight-area id are intentionally left alone: they are already reproducible given
/// the same world, and the world itself is now a function of this master seed.
///
/// Subsystems never share a <see cref="Random"/> instance. Each asks for its own
/// via <see cref="For"/>, seeded from the master seed mixed with a stable, textual
/// per-subsystem tag. That keeps reproducibility independent of the order in which
/// subsystems happen to run — which matters because movement, hover-pathfinding and
/// the LLM all run on background threads and would otherwise interleave their draws.
///
/// The LLM is deliberately out of scope: llama.cpp sampling is nondeterministic and
/// its requests carry no seed, so a run never reproduces the generated narrative text
/// regardless of what we do here. Everything else — world layout, spawn point, dice,
/// travel jitter — is covered.
/// </summary>
public static class GameRng
{
    private static int _masterSeed;
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// The resolved master seed actually in use. Printed at startup so a time-based
    /// run can be pinned afterwards by passing this value back via <c>--seed</c>.
    /// Accessing it before <see cref="Initialize"/> lazily resolves it from
    /// <see cref="Config.Rng.Seed"/>.
    /// </summary>
    public static int MasterSeed
    {
        get
        {
            if (!_initialized) Initialize(Config.Rng.Seed);
            return _masterSeed;
        }
    }

    /// <summary>
    /// Resolves and locks in the master seed for the whole run. A null
    /// <paramref name="seed"/> produces a fresh time-based seed (worlds differ every
    /// launch); a non-null value replays the exact same run. Call once at startup;
    /// later calls are ignored so the seed cannot drift mid-run.
    /// </summary>
    public static void Initialize(int? seed)
    {
        lock (_lock)
        {
            if (_initialized) return;
            _masterSeed = seed ?? Environment.TickCount;
            _initialized = true;
        }
        Console.WriteLine($"[RNG] Master seed: {_masterSeed} ({(seed.HasValue ? "fixed" : "time-based")})");
    }

    /// <summary>
    /// A fresh <see cref="Random"/> for <paramref name="subsystem"/>, seeded
    /// deterministically from the master seed combined with the subsystem name.
    /// Two calls with the same tag return independent generators that produce the
    /// same sequence, so give each distinct consumer its own tag.
    /// </summary>
    public static Random For(string subsystem) => new Random(DerivedSeed(subsystem));

    /// <summary>
    /// The integer seed <see cref="For"/> would use for <paramref name="subsystem"/> —
    /// for consumers that must hand a plain seed to something else (e.g. an existing
    /// <c>Seed</c> property or a per-element hash salt).
    /// </summary>
    public static int DerivedSeed(string subsystem) => unchecked(MasterSeed ^ StableHash(subsystem));

    /// <summary>
    /// FNV-1a hash. Deliberately not <see cref="string.GetHashCode"/>, which is
    /// randomized per process on .NET and would therefore break reproducibility
    /// across launches.
    /// </summary>
    private static int StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261u;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619u;
            }
            return (int)h;
        }
    }
}
