using System;

namespace Cathedral.Fight.Actions;

/// <summary>
/// The active party fighter attempts to flee the fight.
/// Success is governed by <see cref="FightResolver.AttemptRunaway"/>.
/// On success, sets <see cref="FightState.Result"/> to <see cref="FightResult.PartyFled"/>.
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
        if (FightResolver.AttemptRunaway(_fighter, rng))
        {
            state.AddLog($"{_fighter.DisplayName} escapes the fight!");
            state.Result = FightResult.PartyFled;
        }
        else
        {
            state.AddLog($"{_fighter.DisplayName} tries to run but fails! ({_fighter.RunawayChancePercent}%)");
            state.Phase = TurnPhase.TurnEnding;
        }
    }
}
