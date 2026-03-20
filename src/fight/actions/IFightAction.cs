namespace Cathedral.Fight.Actions;

/// <summary>
/// Any action a fighter can take during their turn.
/// Actions are created during decision-making and then <see cref="Execute"/>d by the window.
/// </summary>
public interface IFightAction
{
    /// <summary>
    /// Apply the action's effects to <paramref name="state"/>.
    /// Implementations should call <see cref="FightState.AddLog"/> for meaningful events.
    /// </summary>
    void Execute(FightState state, Random rng);
}
