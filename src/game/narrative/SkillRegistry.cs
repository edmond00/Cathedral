using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative.Skills;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Central registry of all available skills in the game.
/// Provides methods to query skills by function, ID, etc.
/// </summary>
public class SkillRegistry
{
    private readonly Dictionary<string, Skill> _skillsById = new();
    private static SkillRegistry? _instance;
    
    public static SkillRegistry Instance => _instance ??= new SkillRegistry();
    
    private SkillRegistry()
    {
        RegisterAllSkills();
    }
    
    private void RegisterAllSkills()
    {
        // Use reflection to automatically discover and register all skill types
        var skillType = typeof(Skill);
        var assembly = Assembly.GetExecutingAssembly();
        
        var skillTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && skillType.IsAssignableFrom(t))
            .ToList();
        
        foreach (var type in skillTypes)
        {
            try
            {
                // Create instance using parameterless constructor
                if (Activator.CreateInstance(type) is Skill skill)
                {
                    RegisterSkill(skill);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SkillRegistry: Failed to register skill type {type.Name}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"SkillRegistry: Registered {_skillsById.Count} skills automatically via reflection");
    }
    
    private void RegisterSkill(Skill skill)
    {
        _skillsById[skill.SkillId] = skill;
    }
    
    public Skill? GetSkill(string skillId)
    {
        return _skillsById.GetValueOrDefault(skillId);
    }
    
    public List<Skill> GetAllSkills()
    {
        return _skillsById.Values.ToList();
    }
    
    public List<Skill> GetSkillsByFunction(SkillFunction function)
    {
        return _skillsById.Values
            .Where(s => s.Functions.Contains(function))
            .ToList();
    }
    
    public List<Skill> GetObservationSkills()
    {
        return GetSkillsByFunction(SkillFunction.Observation);
    }
    
    public List<Skill> GetThinkingSkills()
    {
        return GetSkillsByFunction(SkillFunction.Thinking);
    }
    
    public List<Skill> GetActionSkills()
    {
        return GetSkillsByFunction(SkillFunction.Action);
    }
}
