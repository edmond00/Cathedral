using System;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Manages observation Modus Mentis LLM slot lifecycle (acquisition + history reset). The actual
/// text generation is handled by <see cref="PersonaRewriter"/> on the acquired slot.
/// </summary>
public class ObservationExecutor
{
    private readonly LlamaServerManager _llamaServer;
    private readonly ModusMentisSlotManager _slotManager;

    public ObservationExecutor(LlamaServerManager llamaServer, ModusMentisSlotManager slotManager)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
    }

    /// <summary>Acquires (creating if needed) the LLM slot for the given observation Modus Mentis.</summary>
    public Task<int> GetOrCreateSlotForModusMentisPublicAsync(ModusMentis modusMentis)
        => _slotManager.GetOrCreateSlotForModusMentisAsync(modusMentis);

    /// <summary>
    /// Resets a Modus Mentis slot's conversation history before a new observation batch.
    /// Preserves the cached persona system prompt.
    /// </summary>
    public void ResetSlot(int slotId)
    {
        _llamaServer.ResetInstance(slotId);
        Console.WriteLine($"ObservationExecutor: Reset slot {slotId} for new observation batch");
    }
}
