using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Central registry of all available modiMentis in the game.
/// Provides methods to query modiMentis by function, ID, etc.
/// </summary>
public class ModusMentisRegistry
{
    private readonly Dictionary<string, ModusMentis> _modiMentisById = new();
    private static ModusMentisRegistry? _instance;
    
    public static ModusMentisRegistry Instance => _instance ??= new ModusMentisRegistry();
    
    private ModusMentisRegistry()
    {
        RegisterAllModiMentis();
    }
    
    private void RegisterAllModiMentis()
    {
        // Use reflection to automatically discover and register all modusMentis types
        var modusMentisType = typeof(ModusMentis);
        var assembly = Assembly.GetExecutingAssembly();
        
        var modusMentisTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && modusMentisType.IsAssignableFrom(t))
            .ToList();
        
        foreach (var type in modusMentisTypes)
        {
            try
            {
                // Create instance using parameterless constructor
                if (Activator.CreateInstance(type) is ModusMentis modusMentis)
                {
                    RegisterModusMentis(modusMentis);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ModusMentisRegistry: Failed to register modusMentis type {type.Name}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"ModusMentisRegistry: Registered {_modiMentisById.Count} modiMentis automatically via reflection");
    }
    
    private void RegisterModusMentis(ModusMentis modusMentis)
    {
        _modiMentisById[modusMentis.ModusMentisId] = modusMentis;
    }
    
    public ModusMentis? GetModusMentis(string modusMentisId)
    {
        return _modiMentisById.GetValueOrDefault(modusMentisId);
    }
    
    public List<ModusMentis> GetAllModiMentis()
    {
        return _modiMentisById.Values.ToList();
    }
    
    public List<ModusMentis> GetModiMentisByFunction(ModusMentisFunction function)
    {
        return _modiMentisById.Values
            .Where(s => s.Functions.Contains(function))
            .ToList();
    }
    
    public List<ModusMentis> GetObservationModiMentis()
    {
        return GetModiMentisByFunction(ModusMentisFunction.Observation);
    }
    
    public List<ModusMentis> GetThinkingModiMentis()
    {
        return GetModiMentisByFunction(ModusMentisFunction.Thinking);
    }
    
    public List<ModusMentis> GetActionModiMentis()
    {
        return GetModiMentisByFunction(ModusMentisFunction.Action);
    }
}
