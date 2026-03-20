using System;

namespace Cathedral.Fight.Actions;

/// <summary>
/// Ends the current fighter's turn without doing anything else.
/// </summary>
public class EndTurnAction : IFightAction
{
    private readonly Fighter _fighter;

    public EndTurnAction(Fighter fighter)
    {
        _fighter = fighter;
    }

    public void Execute(FightState state, Random rng)
    {
        _fighter.HasActedThisTurn = true;
        state.AddLog($"{_fighter.DisplayName} ends their turn.");
        state.Phase = TurnPhase.TurnEnding;
    }
}
