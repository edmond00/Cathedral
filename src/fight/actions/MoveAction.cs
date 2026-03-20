using System;
using System.Collections.Generic;

namespace Cathedral.Fight.Actions;

/// <summary>
/// Moves a fighter along a pre-computed BFS path and deducts cinetic points.
/// </summary>
public class MoveAction : IFightAction
{
    private readonly Fighter _mover;
    private readonly List<(int X, int Y)> _path;

    public MoveAction(Fighter mover, List<(int X, int Y)> path)
    {
        _mover = mover;
        _path  = path;
    }

    public void Execute(FightState state, Random rng)
    {
        if (_path.Count == 0) return;

        int cost = FightResolver.MovementCineticCost(_path.Count, _mover);
        _mover.CurrentCineticPoints = Math.Max(0, _mover.CurrentCineticPoints - cost);

        var dest = _path[^1];
        state.AddLog($"{_mover.DisplayName} moves to ({dest.X},{dest.Y})  [-{cost} CP]");

        // Set up tile-by-tile animation; window will advance MovementPathIndex each frame
        state.MovingFighter     = _mover;
        state.MovementPath      = _path;
        state.MovementPathIndex = 0;
        state.Phase             = TurnPhase.AnimatingMovement;
    }
}
