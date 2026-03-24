using System;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// Simplified LLM wrapper that provides LlamaServerManager access.
/// Mode 6 uses NarrativeController + ThinkingExecutor + ObservationExecutor + OutcomeNarrator instead of Director/Narrator.
/// </summary>
public class LLMActionExecutor
{
    private readonly LlamaServerManager _llamaServer;

    public LLMActionExecutor(LlamaServerManager llamaServer, SimpleActionExecutor fallbackExecutor)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        // fallbackExecutor is kept for compatibility but not used in Mode 6
    }

    /// <summary>
    /// Initializes the executor. Mode 6 doesn't need to create any slots here.
    /// </summary>
    public Task InitializeAsync(bool skipDirectorNarrator = false)
    {
        Console.WriteLine("LLMActionExecutor: Ready (Mode 6 uses NarrativeController architecture)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the underlying LlamaServerManager (needed for NarrativeController initialization).
    /// </summary>
    public LlamaServerManager GetLlamaServerManager()
    {
        return _llamaServer;
    }
}
