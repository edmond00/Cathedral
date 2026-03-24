using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Centralized manager for modusMentis-to-slot mappings.
/// Ensures that each modusMentis reuses the same LLM slot regardless of which function (observation, thinking, action) is being used.
/// </summary>
public class ModusMentisSlotManager
{
    private readonly LlamaServerManager _llmManager;
    private readonly Dictionary<string, int> _modusMentisToSlot = new(); // modusMentis ID -> slot ID
    
    public ModusMentisSlotManager(LlamaServerManager llmManager)
    {
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
    }
    
    /// <summary>
    /// Gets or creates a slot for the given modusMentis.
    /// If the modusMentis already has a slot assigned, returns that slot.
    /// Otherwise creates a new slot with the modusMentis's persona prompt.
    /// </summary>
    public async Task<int> GetOrCreateSlotForModusMentisAsync(ModusMentis modusMentis)
    {
        if (_modusMentisToSlot.TryGetValue(modusMentis.ModusMentisId, out int existingSlot))
        {
            Console.WriteLine($"ModusMentisSlotManager: Reusing slot {existingSlot} for {modusMentis.DisplayName}");
            return existingSlot;
        }
        
        // Create new slot with the modusMentis's persona prompt
        int slotId = await _llmManager.CreateInstanceAsync(modusMentis.PersonaPrompt!);
        _modusMentisToSlot[modusMentis.ModusMentisId] = slotId;
        
        Console.WriteLine($"ModusMentisSlotManager: Created slot {slotId} for {modusMentis.DisplayName}");
        
        return slotId;
    }
    
    /// <summary>
    /// Checks if a modusMentis already has a slot assigned.
    /// </summary>
    public bool HasSlot(string modusMentisId)
    {
        return _modusMentisToSlot.ContainsKey(modusMentisId);
    }
    
    /// <summary>
    /// Gets the slot ID for a modusMentis, or null if no slot is assigned.
    /// </summary>
    public int? GetSlot(string modusMentisId)
    {
        return _modusMentisToSlot.TryGetValue(modusMentisId, out int slot) ? slot : null;
    }
}
