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
    private readonly Dictionary<Type, ModusMentis> _modiMentisByType = new();
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
        
        // Only auto-register templates that can be created with a parameterless constructor.
        // MMs built from runtime state (e.g. ChildhoodMemoryModusMentis, SyntheticItemModusMentis)
        // take constructor arguments and are constructed manually — skip them silently here rather
        // than letting Activator.CreateInstance throw and log a spurious failure.
        var modusMentisTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && modusMentisType.IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) != null)
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

        ValidateDialogueModiMentis();
    }

    /// <summary>
    /// Every modusMentis that can speak in dialogue must supply a <see cref="ModusMentis.PersonaTone"/>:
    /// the branch selector uses it as the character's perspective, with no fallback. Fail fast at
    /// startup rather than surfacing an empty perspective mid-conversation.
    /// </summary>
    private void ValidateDialogueModiMentis()
    {
        var missing = _modiMentisById.Values
            .Where(m => m.Functions.Contains(ModusMentisFunction.Speaking)
                        && string.IsNullOrWhiteSpace(m.PersonaTone))
            .Select(m => m.ModusMentisId)
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Dialogue modiMentis missing a PersonaTone: " + string.Join(", ", missing));
    }
    
    private void RegisterModusMentis(ModusMentis modusMentis)
    {
        _modiMentisById[modusMentis.ModusMentisId] = modusMentis;
        _modiMentisByType[modusMentis.GetType()]   = modusMentis;
    }

    public ModusMentis? GetModusMentis(string modusMentisId)
    {
        return _modiMentisById.GetValueOrDefault(modusMentisId);
    }

    /// <summary>
    /// Looks up the registered template by concrete type. Preferred over the id overload at
    /// call sites that can name the type directly (e.g. the reminescence catalog), since the
    /// compiler then guarantees the modusMentis exists.
    /// </summary>
    public ModusMentis? GetModusMentis(Type modusMentisType)
    {
        return _modiMentisByType.GetValueOrDefault(modusMentisType);
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

    public List<ModusMentis> GetSpeakingModiMentis()
    {
        return GetModiMentisByFunction(ModusMentisFunction.Speaking);
    }
}
