using System;
using System.Collections.Generic;
using System.Linq;
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
        // Observation skills
        RegisterSkill(new ObservationSkill());
        RegisterSkill(new MycologySkill()); // Multi-function
        
        // Thinking skills
        RegisterSkill(new AlgebraicAnalysisSkill());
        // TODO: Add more thinking skills (Visual Analysis, Logic, Intuition, etc.)
        
        // Action skills
        RegisterSkill(new BruteForceSkill());
        // TODO: Add more action skills (Finesse, Athletics, Stealth, etc.)
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
