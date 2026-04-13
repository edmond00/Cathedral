using System.Collections.Generic;

namespace Cathedral.Game.Dialogue.Affinity;

/// <summary>
/// Per-NPC table tracking affinity with each party member and other NPCs.
/// Stored by NpcId. Party member entries are added lazily on first dialogue.
/// The internal dictionary is shared with the scene's persistent affinity store so
/// changes written during a dialogue session survive across scene reloads.
/// </summary>
public class AffinityTable
{
    private readonly Dictionary<string, AffinityLevel> _table;

    public AffinityTable() => _table = new Dictionary<string, AffinityLevel>();

    /// <summary>Initialise from an existing (possibly persisted) dictionary — shares the reference.</summary>
    public AffinityTable(Dictionary<string, AffinityLevel> sharedData) => _table = sharedData;

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the affinity with <paramref name="partyMemberId"/>.
    /// Falls back to <see cref="AffinityLevel.Stranger"/> if no entry exists yet.
    /// </summary>
    public AffinityLevel GetLevel(string partyMemberId)
        => _table.TryGetValue(partyMemberId, out var level) ? level : AffinityLevel.Stranger;

    /// <summary>True when the party member has no entry — i.e. they are a Stranger.</summary>
    public bool IsStranger(string partyMemberId) => !_table.ContainsKey(partyMemberId);

    /// <summary>Returns a read-only snapshot of all recorded entries.</summary>
    public IReadOnlyDictionary<string, AffinityLevel> AllEntries => _table;

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>Sets affinity with <paramref name="partyMemberId"/> to a specific level.</summary>
    public void SetLevel(string partyMemberId, AffinityLevel level)
        => _table[partyMemberId] = level;

    /// <summary>
    /// Bumps affinity one step upward (or downward if <paramref name="delta"/> is -1),
    /// clamped to <paramref name="min"/>–<paramref name="max"/>.
    /// </summary>
    public void Adjust(string partyMemberId, int delta,
        AffinityLevel min = AffinityLevel.AnnoyingAcquaintance,
        AffinityLevel max = AffinityLevel.CloseFriend)
    {
        var current = GetLevel(partyMemberId);
        var next = delta > 0 ? current.Increment(max) : current.Decrement(min);
        _table[partyMemberId] = next;
    }

    /// <summary>
    /// Sets the party member to <see cref="AffinityLevel.DistantAcquaintance"/> if they are
    /// still a Stranger (no entry). Called at dialogue end as a "first contact" fallback.
    /// Does not override an entry already written by a dialogue outcome.
    /// </summary>
    public void MarkFirstContact(string partyMemberId)
    {
        if (!_table.ContainsKey(partyMemberId))
            _table[partyMemberId] = AffinityLevel.DistantAcquaintance;
    }

    /// <summary>Exposes the raw dictionary for save/load serialisation.</summary>
    public Dictionary<string, AffinityLevel> GetRawData() => _table;
}
