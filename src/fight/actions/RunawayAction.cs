using System;

namespace Cathedral.Fight.Actions;

/// <summary>
/// The active fighter attempts to flee the fight.
/// Costs 1 CP and triggers a dice roll: <see cref="Fighter.RunawayDiceCount"/> d6, need at
/// least one six to flee. The dice animation runs in the regular dice flow; success
/// (any six) sets <see cref="FightState.Result"/> to <see cref="FightResult.PartyFled"/>
/// in <c>FightModeAdapter.FinishRunawayRoll</c>.
/// </summary>
public class RunawayAction : IFightAction
{
    private readonly Fighter _fighter;

    public RunawayAction(Fighter fighter)
    {
        _fighter = fighter;
    }

    public void Execute(FightState state, Random rng)
    {
        // Deduct CP
        const int cost = 1;
        _fighter.CurrentCineticPoints = Math.Max(0, _fighter.CurrentCineticPoints - cost);
        state.AddLog($"{_fighter.DisplayName} attempts to flee (rolling {_fighter.RunawayDiceCount}d, need 1 six).  [-{cost} CP]");

        // Set up the dice roll — single-roll, success on >=1 six
        state.PendingRunaway        = true;
        state.PendingSkill          = null;
        state.PendingTarget         = null;
        state.PendingLearnSkill     = null;
        state.DiceNumberOfDice      = _fighter.RunawayDiceCount;
        state.DiceDifficulty        = 1; // need at least one six
        state.DiceSecondaryNumberOfDice = 0;
        state.DiceFinalValues       = null;
        state.DiceSecondaryFinalValues = null;
        state.IsDiceRolling         = true;
        state.Phase                 = TurnPhase.AnimatingDice;
    }
}
