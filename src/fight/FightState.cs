using System.Collections.Generic;
using Cathedral.Fight.Actions;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// Category for a fight log entry — controls the color in the bottom panel.
/// </summary>
public enum LogEntryType
{
    Normal,
    Attack,
    Miss,
    Wound,
    SpecialEffect,
    Learning,
    Defense,
}

/// <summary>
/// Which player-facing phase the fight is currently in.
/// Controls which inputs are valid and what the UI highlights.
/// </summary>
public enum TurnPhase
{
    /// <summary>Active fighter is choosing what to do.</summary>
    SelectingAction,
    /// <summary>Player tapped Move: waiting for a destination cell click.</summary>
    TargetingMovement,
    /// <summary>Player chose a PlayerChooses skill: waiting for body-part selection.</summary>
    WaitingForBodyPartChoice,
    /// <summary>Dice animation is playing.</summary>
    AnimatingDice,
    /// <summary>Dice animation finished; showing result, waiting for "Continue".</summary>
    WaitingForDiceComplete,
    /// <summary>Turn is transitioning to next fighter.</summary>
    TurnEnding,
    /// <summary>Fighter is stepping through a movement path one tile per animation frame.</summary>
    AnimatingMovement,
    /// <summary>Player is attempting to learn an unknown skill — learning dice roll is playing.</summary>
    SkillLearningRoll,
    /// <summary>Learning dice finished; showing result, waiting for "Continue".</summary>
    WaitingForLearningComplete,
    /// <summary>
    /// A buff's vital-heat cost is being drawn, one humor at a time, with the consumption box up.
    /// Purely informational — it plays out and hands the turn back, with no Continue button and no
    /// decision to make, which is why there is no waiting-for-complete twin phase.
    /// </summary>
    AnimatingVitalHeat,
}

/// <summary>
/// Full mutable state of an in-progress fight.
/// The window owns this object and passes it to UI-rendering and resolver helpers.
/// </summary>
public class FightState
{
    // ── Who is fighting ──────────────────────────────────────────────
    /// <summary>All fighters, sorted descending by InitiativeRoll at fight start.</summary>
    public List<Fighter> Fighters { get; }
    public int ActiveFighterIndex { get; set; }
    public Fighter? ActiveFighter => Fighters.Count > 0 ? Fighters[ActiveFighterIndex] : null;

    // ── Arena ────────────────────────────────────────────────────────
    public FightArea Area { get; }

    // ── Result ───────────────────────────────────────────────────────
    public FightResult Result { get; set; } = FightResult.Ongoing;
    public bool IsOver => Result != FightResult.Ongoing;

    // ── Turn phase & pending intent ───────────────────────────────────
    public TurnPhase Phase { get; set; } = TurnPhase.SelectingAction;

    /// <summary>Skill the player or AI has committed to using this turn.</summary>
    public FightingSkill? PendingSkill { get; set; }
    /// <summary>Target fighter for the pending skill.</summary>
    public Fighter? PendingTarget { get; set; }
    /// <summary>
    /// Where the blow is aimed, resolved before the dice for <em>every</em> targeting mode: chosen
    /// by the player, fixed by the skill, or pre-rolled for a Random skill. Either a bare body-part
    /// id or the overlay's <c>"organ_part_id,body_part_id"</c> pair.
    /// </summary>
    public string? PendingBodyPartId { get; set; }

    /// <summary>
    /// The top-level body part <see cref="PendingBodyPartId"/> falls inside — the section whose
    /// armour is charged. Null when the blow lands nowhere in particular (a wildcard wound), which
    /// no garment can turn.
    /// </summary>
    public string? PendingArmorSection { get; set; }
    /// <summary>Whether the player is selecting a movement destination.</summary>
    public bool IsMovementMode { get; set; }

    // ── Movement animation ────────────────────────────────────────────
    public List<(int X, int Y)>? MovementPath { get; set; }
    public Fighter? MovingFighter { get; set; }
    public int MovementPathIndex { get; set; }

    // ── Dice roll state ──────────────────────────────────────────────
    public int DiceNumberOfDice { get; set; }
    public int DiceDifficulty { get; set; }
    public bool IsDiceRolling { get; set; }
    public int[]? DiceFinalValues { get; set; }
    /// <summary>Secondary (defense) dice count for the two-roll attack flow. 0 = single-roll mode.</summary>
    public int DiceSecondaryNumberOfDice { get; set; }
    public int[]? DiceSecondaryFinalValues { get; set; }
    /// <summary>True while a runaway dice roll is in progress (need at least one six to flee).</summary>
    public bool PendingRunaway { get; set; }

    /// <summary>True while a knockdown-recovery dice roll is in progress (need at least one six to act this turn).</summary>
    public bool PendingKnockdownRecovery { get; set; }

    /// <summary>
    /// (medium-key, skill-id) pairs the active fighter has already committed to this turn.
    /// Reset by <see cref="AdvanceToNextFighter"/>. Used to enforce the once-per-turn rule
    /// for every action except MOVE; a skill that appears under two medium tabs (e.g. the
    /// same weapon in both hands) tracks each tab independently because the medium key
    /// includes the equipment anchor.
    /// </summary>
    public HashSet<(string MediumKey, string SkillId)> UsedActionsThisTurn { get; } = new();

    /// <summary>
    /// Whether <paramref name="fighter"/> has already spent this (medium, skill) pair this turn.
    ///
    /// <para>
    /// Go through here rather than testing <see cref="UsedActionsThisTurn"/> directly: an effect
    /// may lift the rule (Iron Nerves lets a fighter repeat a skill), and that exemption has to
    /// live in exactly one place or it will hold in some of the dozen call sites and not others.
    /// </para>
    /// </summary>
    public bool IsActionUsed(Fighter fighter, string mediumKey, string skillId)
    {
        if (!UsedActionsThisTurn.Contains((mediumKey, skillId))) return false;
        return !fighter.ActiveEffects.Any(e => e.BypassesUsedActions);
    }

    /// <summary>Record that <paramref name="fighter"/> has spent this (medium, skill) pair this turn.</summary>
    public void MarkActionUsed(Fighter fighter, string mediumKey, string skillId)
        => UsedActionsThisTurn.Add((mediumKey, skillId));

    /// <summary>Undo a <see cref="MarkActionUsed"/> — used when an action is cancelled before it resolves.</summary>
    public void UnmarkActionUsed(Fighter fighter, string mediumKey, string skillId)
        => UsedActionsThisTurn.Remove((mediumKey, skillId));

    /// <summary>True once the active fighter has attempted to flee this turn.</summary>
    public bool RunUsedThisTurn { get; set; }

    // ── Vital-heat consumption (buff skills) ──────────────────────────
    // Travel already shows the player every humor it burns, one at a time, against a filling bar.
    // A buff spending up to ten of them was the odd one out, so it is paid the same way: the cost
    // is NOT taken up front — it is drawn humor by humor while the box is on screen, which is what
    // gives the bar something to fill and the flash something to show.

    /// <summary>Fighter paying, or null when no consumption box is up.</summary>
    public Fighter? VitalHeatFighter { get; private set; }
    /// <summary>Name of the skill being paid for.</summary>
    public string VitalHeatSkillName { get; private set; } = "";
    /// <summary>How much heat the skill asked for.</summary>
    public int VitalHeatRequired { get; private set; }

    private readonly List<BodyHumor> _vitalHeatDrawn = new();
    /// <summary>The humors drawn so far, in rotation order.</summary>
    public IReadOnlyList<BodyHumor> VitalHeatDrawn => _vitalHeatDrawn;

    /// <summary>The humor drawn most recently — what the box flashes.</summary>
    public BodyHumor? VitalHeatLatest { get; private set; }

    /// <summary>True once the queues ran dry before the cost was met.</summary>
    public bool VitalHeatExhausted { get; private set; }

    /// <summary>Nothing left to draw: the cost is met, or the body has no more heat to give.</summary>
    public bool VitalHeatComplete =>
        VitalHeatFighter == null || VitalHeatExhausted || _vitalHeatDrawn.Count >= VitalHeatRequired;

    /// <summary>
    /// Arm the consumption box. The heat is spent by <see cref="DrawNextVitalHeat"/> as the
    /// animation runs, not here.
    /// </summary>
    public void BeginVitalHeatConsumption(Fighter fighter, string skillName, int required)
    {
        VitalHeatFighter   = fighter;
        VitalHeatSkillName = skillName;
        VitalHeatRequired  = required;
        VitalHeatExhausted = false;
        VitalHeatLatest    = null;
        _vitalHeatDrawn.Clear();
        Phase              = TurnPhase.AnimatingVitalHeat;
    }

    /// <summary>
    /// Burn one more humor toward the cost. Returns false when there is nothing left to draw —
    /// either the cost is paid or every queue has gone critical.
    /// </summary>
    public bool DrawNextVitalHeat(Random rng)
    {
        if (VitalHeatFighter == null || VitalHeatExhausted) return false;
        if (_vitalHeatDrawn.Count >= VitalHeatRequired) return false;

        var humor = VitalHeatFighter.Member.HumorQueues.ConsumeCycled(VitalHeatFighter.Member, rng);
        if (humor == null) { VitalHeatExhausted = true; return false; }

        _vitalHeatDrawn.Add(humor);
        VitalHeatLatest = humor;
        return true;
    }

    /// <summary>Tear the consumption box down.</summary>
    public void ClearVitalHeatConsumption()
    {
        VitalHeatFighter   = null;
        VitalHeatSkillName = "";
        VitalHeatRequired  = 0;
        VitalHeatExhausted = false;
        VitalHeatLatest    = null;
        _vitalHeatDrawn.Clear();
    }

    // ── VerbAction log ───────────────────────────────────────────────────
    private const int MaxLogLines = 200;
    public List<(string Text, LogEntryType Type)> ActionLog { get; } = new();
    public void AddLog(string line, LogEntryType type = LogEntryType.Normal)
    {
        ActionLog.Add((line, type));
        if (ActionLog.Count > MaxLogLines)
            ActionLog.RemoveAt(0);
    }

    // ── Skill learning state ──────────────────────────────────────────
    /// <summary>The unknown skill the player is attempting to learn this turn.</summary>
    public FightingSkill? PendingLearnSkill { get; set; }
    /// <summary>Number of dice for the learning roll (from fight_learning derived stat).</summary>
    public int LearningDiceCount { get; set; }
    /// <summary>Difficulty threshold for the learning roll (index of skill in medium list).</summary>
    public int LearningDifficulty { get; set; }
    /// <summary>Final dice values from the learning roll animation.</summary>
    public int[]? LearningDiceValues { get; set; }
    /// <summary>Result of the learning roll. Null while not yet resolved.</summary>
    public bool? LearningSucceeded { get; set; }

    // ── Constructor ───────────────────────────────────────────────────
    public FightState(FightArea area, List<Fighter> fighters)
    {
        Area = area;
        Fighters = fighters;
        ActiveFighterIndex = 0;
    }

    // ── Turn management ───────────────────────────────────────────────
    /// <summary>
    /// Move to the next living fighter in initiative order.
    /// Skips dead fighters. Calls <see cref="Fighter.StartTurn(FightState, Random)"/> on the new active fighter.
    /// </summary>
    public void AdvanceToNextFighter(Random rng)
    {
        // Clear pending intent
        PendingSkill = null;
        PendingTarget = null;
        PendingBodyPartId = null;
        PendingArmorSection = null;
        PendingLearnSkill = null;
        LearningDiceValues = null;
        LearningSucceeded = null;
        IsMovementMode = false;
        Phase = TurnPhase.SelectingAction;
        IsDiceRolling = false;
        DiceFinalValues = null;
        DiceSecondaryFinalValues = null;
        DiceSecondaryNumberOfDice = 0;
        PendingRunaway = false;
        PendingKnockdownRecovery = false;
        UsedActionsThisTurn.Clear();
        RunUsedThisTurn = false;
        MovementPath = null;
        MovingFighter = null;
        MovementPathIndex = 0;

        // Find next alive fighter in round-robin order
        int start = ActiveFighterIndex;
        for (int i = 1; i <= Fighters.Count; i++)
        {
            int next = (start + i) % Fighters.Count;
            if (Fighters[next].IsAlive)
            {
                ActiveFighterIndex = next;
                Fighters[next].StartTurn(this, rng);
                return;
            }
        }
        // No living fighter found — fight should already be over
    }

    /// <summary>
    /// Evaluate whether the fight has ended and set <see cref="Result"/> accordingly.
    /// </summary>
    public void CheckFightEnd()
    {
        bool anyPartyAlive = Fighters.Any(f => f.Faction == FighterFaction.Party && f.IsAlive);
        bool anyEnemyAlive = Fighters.Any(f => f.Faction == FighterFaction.Enemy && f.IsAlive);

        if (!anyPartyAlive && !anyEnemyAlive)
            Result = FightResult.EnemyWon;   // Mutual kill edge case → enemy wins
        else if (!anyEnemyAlive)
            Result = FightResult.PartyWon;
        else if (!anyPartyAlive)
            Result = FightResult.EnemyWon;
        // else still Ongoing
    }

    // ── Spatial helpers ───────────────────────────────────────────────
    public Fighter? GetFighterAt(int x, int y) =>
        Fighters.FirstOrDefault(f => f.IsAlive && f.X == x && f.Y == y);
}
