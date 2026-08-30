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
        SeedDebugAffinity();
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
        SeedDebugAffinity();
    }

    /// <summary>
    /// <c>--npc-affinity</c> and <c>--npc-hostile</c>: start the protagonist at a given level with
    /// this NPC, and/or as its declared enemy, rather than as a so a test can reach the six verbs gated on already knowing somebody without holding
    /// a conversation to earn it first.
    ///
    /// <para>Here rather than in <c>LocationInstanceState.AffinityFor</c> because that is only one of
    /// the two ways a table is made: a scene built without a persistent state — which is what the
    /// audits and <c>--verb-probe</c> do — never goes through it, so the flag appeared to do nothing
    /// exactly where it was being used to find situations.</para>
    ///
    /// <para>Only fills a gap, never overwrites: a relationship actually built in play, or restored
    /// from a save, wins. Inert unless the flag is passed.</para>
    /// </summary>
    private void SeedDebugAffinity()
    {
        if (Config.Debug.NpcHostile) _enemies.Add(Narrative.Protagonist.AffinityKeyConstant);

        if (Config.Debug.NpcAffinity is not { } seeded) return;
        if (_table.ContainsKey(Narrative.Protagonist.AffinityKeyConstant)) return;
        _table[Narrative.Protagonist.AffinityKeyConstant] = seeded;
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
    /// clamped to <paramref name="min"/>–<paramref name="max"/>. A <paramref name="delta"/> of 0
    /// changes nothing, including whether an entry exists at all.
    ///
    /// <para>That last part is not pedantry. The test was <c>delta &gt; 0 ? up : down</c>, so 0 read
    /// as a step DOWN — and a step down from Stranger clamps to the <c>min</c>, which is Annoying
    /// Acquaintance. A conversation declaring "this leaves the relationship where it was" therefore
    /// filed the player as an irritant on first meeting, which then offered <c>reconcile</c> against
    /// somebody there had never been a quarrel with.</para>
    /// </summary>
    public void Adjust(string partyMemberId, int delta,
        AffinityLevel min = AffinityLevel.AnnoyingAcquaintance,
        AffinityLevel max = AffinityLevel.CloseFriend)
    {
        if (delta == 0) return;

        var current = GetLevel(partyMemberId);

        // Suspicious sits OFF the ladder (6, above CloseFriend), so ±1 ARITHMETIC on it is
        // meaningless: Decrement would read max(6-1, 1) and quietly promote a suspicious NPC to
        // Close Friend for having walked out of a conversation with them. It is still a state you
        // must be able to leave, though — refusing to move it at all made it a dead end nothing in
        // the game could ever improve, so an NPC talked down out of hostility stayed permanently
        // unimprovable and every later conversation with them changed nothing. Name both
        // neighbours explicitly instead: up is the civil "we have met" rung, down is the irritant
        // one. (Suspicious is deliberately NOT between them — it grants no dice, and being wary of
        // somebody is not a milder form of finding them annoying.)
        var next = current == AffinityLevel.Suspicious
            ? (delta > 0 ? AffinityLevel.DistantAcquaintance : AffinityLevel.AnnoyingAcquaintance)
            : (delta > 0 ? current.Increment(max) : current.Decrement(min));

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
