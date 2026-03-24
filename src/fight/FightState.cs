using System.Collections.Generic;
using Cathedral.Fight.Actions;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

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
    AnimatingMovement
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
    /// <summary>Body-part id chosen by the player for a PlayerChooses skill.</summary>
    public string? PendingBodyPartId { get; set; }
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

    // ── Action log ───────────────────────────────────────────────────
    private const int MaxLogLines = 200;
    public List<string> ActionLog { get; } = new();
    public void AddLog(string line)
    {
        ActionLog.Add(line);
        if (ActionLog.Count > MaxLogLines)
            ActionLog.RemoveAt(0);
    }

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
    /// Skips dead fighters.  Calls <see cref="Fighter.StartTurn"/> on the new active fighter.
    /// </summary>
    public void AdvanceToNextFighter()
    {
        // Clear pending intent
        PendingSkill = null;
        PendingTarget = null;
        PendingBodyPartId = null;
        IsMovementMode = false;
        Phase = TurnPhase.SelectingAction;
        IsDiceRolling = false;
        DiceFinalValues = null;
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
                Fighters[next].StartTurn();
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
