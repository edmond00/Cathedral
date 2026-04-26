using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// Static registry of all childhood reminescences. Populated once at startup by
/// <see cref="ReminescenceCatalog.Build"/> in the Catalog file. Keyed by reminescence id
/// (e.g. "sound_in_the_dark").
/// </summary>
public static class ReminescenceRegistry
{
    private static readonly Dictionary<string, ReminescenceData> _byId =
        new(StringComparer.OrdinalIgnoreCase);

    private static bool _initialised;
    private static readonly object _lock = new();

    /// <summary>The reminescence id the player starts at when entering the reminescence phase.</summary>
    public const string EntryReminescenceId = "sound_in_the_dark";

    /// <summary>Sentinel value meaning "the reminescence phase ends after this fragment".</summary>
    public const string EndSentinel = "<END>";

    private static void EnsureInitialised()
    {
        if (_initialised) return;
        lock (_lock)
        {
            if (_initialised) return;
            ReminescenceCatalog.Build(_byId);
            _initialised = true;
            Console.WriteLine($"ReminescenceRegistry: {_byId.Count} reminescence(s) loaded");
        }
    }

    /// <summary>Returns the reminescence with the given id, or null when unknown.</summary>
    public static ReminescenceData? Get(string id)
    {
        EnsureInitialised();
        return _byId.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>Returns the entry reminescence (sound_in_the_dark).</summary>
    public static ReminescenceData GetEntry()
    {
        var entry = Get(EntryReminescenceId)
            ?? throw new InvalidOperationException(
                $"ReminescenceRegistry has no entry reminescence id '{EntryReminescenceId}'.");
        return entry;
    }

    /// <summary>True when the given id is the &lt;END&gt; sentinel.</summary>
    public static bool IsEnd(string id) =>
        string.Equals(id, EndSentinel, StringComparison.OrdinalIgnoreCase);
}
