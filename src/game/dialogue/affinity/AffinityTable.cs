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
    private readonly Dictionary<string, AffinityLevel>       _table;
    private readonly Dictionary<string, CriminalAffinityType> _criminalRecord;
    private readonly HashSet<string>                          _enemies;

    public AffinityTable()
    {
        _table          = new Dictionary<string, AffinityLevel>();
        _criminalRecord = new Dictionary<string, CriminalAffinityType>();
        _enemies        = new HashSet<string>();
    }

    /// <summary>Initialise from an existing (possibly persisted) dictionary — shares the reference.</summary>
    public AffinityTable(Dictionary<string, AffinityLevel> sharedData)
    {
        _table          = sharedData;
        _criminalRecord = new Dictionary<string, CriminalAffinityType>();
        _enemies        = new HashSet<string>();
    }

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

    // ── Enemy tracking ────────────────────────────────────────────────────────

    /// <summary>True when this NPC currently considers <paramref name="partyMemberId"/> an enemy.</summary>
    public bool IsEnemy(string partyMemberId) => _enemies.Contains(partyMemberId);

    /// <summary>Marks <paramref name="partyMemberId"/> as an enemy of this NPC.</summary>
    public void SetEnemy(string partyMemberId) => _enemies.Add(partyMemberId);

    /// <summary>Clears the enemy flag (e.g. after a successful reconciliation).</summary>
    public void ClearEnemy(string partyMemberId) => _enemies.Remove(partyMemberId);

    // ── Criminal record ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the worst crime this NPC has witnessed the given party member commit.
    /// Falls back to <see cref="CriminalAffinityType.None"/> when nothing is recorded.
    /// </summary>
    public CriminalAffinityType GetCrime(string partyMemberId)
        => _criminalRecord.TryGetValue(partyMemberId, out var crime) ? crime : CriminalAffinityType.None;

    /// <summary>Records (or upgrades) a witnessed crime for the given party member.</summary>
    public void RecordCrime(string partyMemberId, CriminalAffinityType crime)
    {
        // Only escalate, never downgrade the criminal record.
        if (!_criminalRecord.TryGetValue(partyMemberId, out var current) || crime > current)
            _criminalRecord[partyMemberId] = crime;
    }

    /// <summary>Clears a previously recorded crime (e.g. after an accepted apology or pardon).</summary>
    public void ClearCrime(string partyMemberId)
        => _criminalRecord.Remove(partyMemberId);
}
