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
    private readonly HashSet<string>                   _enemies;

    public AffinityTable()
    {
        _table   = new Dictionary<string, AffinityLevel>();
        _enemies = new HashSet<string>();
    }

    /// <summary>
    /// Initialise from existing (possibly persisted) stores — shares both references, so everything
    /// written during a visit lands in the location's saved state with no save step.
    ///
    /// <para>Both stores must be shared, not one. Affinity alone used to persist while enmity was
    /// re-minted per visit, so someone you fled from mid-fight greeted you as a mild acquaintance on
    /// your next arrival: the grudge existed for exactly as long as the scene did.</para>
    /// </summary>
    public AffinityTable(Dictionary<string, AffinityLevel> sharedData, HashSet<string> sharedEnemies)
    {
        _table   = sharedData;
        _enemies = sharedEnemies;
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

    /// <summary>
    /// Whether this creature has been talked or gentled down out of hostility: no longer an enemy,
    /// and left at the wary <see cref="AffinityLevel.Suspicious"/> that both <c>appease</c> and the
    /// reconcile tree put it at.
    ///
    /// <para>Named here because the pair of conditions is the definition of "appeased" and was
    /// already being spelled out in two places. <c>TameVerb</c> is the third, and taming a beast that
    /// still wants to kill you should not be a thing anyone has to remember to prevent.</para>
    /// </summary>
    public bool IsAppeased(string partyMemberId)
        => !IsEnemy(partyMemberId) && GetLevel(partyMemberId) == AffinityLevel.Suspicious;

    /// <summary>
    /// Marks <paramref name="partyMemberId"/> as an enemy of this NPC. Persistent: the grudge is
    /// filed in the location's state and survives the scene being torn down and rebuilt, so walking
    /// out of a fight ends the fight and not the enmity.
    /// </summary>
    public void SetEnemy(string partyMemberId) => _enemies.Add(partyMemberId);

    /// <summary>Clears the enemy flag (e.g. after a successful reconciliation).</summary>
    public void ClearEnemy(string partyMemberId) => _enemies.Remove(partyMemberId);

    /// <summary>Exposes the raw enemy set for save/load serialisation.</summary>
    public HashSet<string> GetRawEnemies() => _enemies;
}
