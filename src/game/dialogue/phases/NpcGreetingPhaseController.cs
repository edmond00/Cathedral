using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Executors;

namespace Cathedral.Game.Dialogue.Phases;

/// <summary>
/// Generates and pushes the NPC's opening text for the current subject node.
/// </summary>
public class NpcGreetingPhaseController
{
    private readonly NpcGreetingExecutor _executor;

    public NpcGreetingPhaseController(NpcGreetingExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Generates greeting text and returns it to be rendered by the UI.
    /// </summary>
    public async Task<string> ExecuteAsync(NpcInstance npc, CancellationToken cancellationToken = default)
    {
        return await _executor.GenerateGreetingAsync(npc, cancellationToken);
    }
}
