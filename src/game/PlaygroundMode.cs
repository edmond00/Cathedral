using System;
using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Playground mode bypasses all LLM calls and replaces them with lightweight
/// placeholder responses. Activated by the --playground CLI flag.
///
/// Slot management: fake sequential slot IDs (starting at 1000) are handed out
/// without touching the LLM server, so all modusMentis-dependent components
/// work normally while skipping every actual HTTP call.
/// </summary>
public static class PlaygroundMode
{
    /// <summary>Whether playground mode is active.</summary>
    public static bool IsActive { get; set; } = false;

    private static readonly Random _rng = new();
    private static int _nextFakeSlotId = 1000;
    private static readonly Dictionary<string, int> _modusMentisIdToFakeSlot = new();

    // ── Fake slot management ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a fake slot ID for <paramref name="modusMentisId"/>, creating one on first call.
    /// </summary>
    public static int GetOrCreateFakeSlot(string modusMentisId, string displayName)
    {
        if (_modusMentisIdToFakeSlot.TryGetValue(modusMentisId, out int existing))
            return existing;

        int slotId = _nextFakeSlotId++;
        _modusMentisIdToFakeSlot[modusMentisId] = slotId;
        Console.WriteLine($"PlaygroundMode: Created fake slot {slotId} for '{displayName}'");
        return slotId;
    }

    // ── Random helpers ─────────────────────────────────────────────────────────

    /// <summary>Returns a random element from <paramref name="list"/>.</summary>
    public static T Pick<T>(IList<T> list) => list[_rng.Next(list.Count)];

    /// <summary>Shared Random instance for playground stubs.</summary>
    public static Random Rng => _rng;
}
