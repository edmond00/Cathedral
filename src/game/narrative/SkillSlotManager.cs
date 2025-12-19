using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Centralized manager for skill-to-slot mappings.
/// Ensures that each skill reuses the same LLM slot regardless of which function (observation, thinking, action) is being used.
/// </summary>
public class SkillSlotManager
{
    private readonly LlamaServerManager _llmManager;
    private readonly Dictionary<string, int> _skillToSlot = new(); // skill ID -> slot ID
    
    public SkillSlotManager(LlamaServerManager llmManager)
    {
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
    }
    
    /// <summary>
    /// Gets or creates a slot for the given skill.
    /// If the skill already has a slot assigned, returns that slot.
    /// Otherwise creates a new slot with the skill's persona prompt.
    /// </summary>
    public async Task<int> GetOrCreateSlotForSkillAsync(Skill skill)
    {
        if (_skillToSlot.TryGetValue(skill.SkillId, out int existingSlot))
        {
            Console.WriteLine($"SkillSlotManager: Reusing slot {existingSlot} for {skill.DisplayName}");
            return existingSlot;
        }
        
        // Create new slot with the skill's persona prompt
        int slotId = await _llmManager.CreateInstanceAsync(skill.PersonaPrompt!);
        _skillToSlot[skill.SkillId] = slotId;
        
        Console.WriteLine($"SkillSlotManager: Created slot {slotId} for {skill.DisplayName}");
        
        return slotId;
    }
    
    /// <summary>
    /// Checks if a skill already has a slot assigned.
    /// </summary>
    public bool HasSlot(string skillId)
    {
        return _skillToSlot.ContainsKey(skillId);
    }
    
    /// <summary>
    /// Gets the slot ID for a skill, or null if no slot is assigned.
    /// </summary>
    public int? GetSlot(string skillId)
    {
        return _skillToSlot.TryGetValue(skillId, out int slot) ? slot : null;
    }
}
