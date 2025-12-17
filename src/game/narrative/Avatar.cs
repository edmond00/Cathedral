using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents the player's avatar with body parts, skills, and humors.
/// Tracks learned skills, current state, inventory, and companions.
/// </summary>
public class Avatar
{
    public Dictionary<string, int> BodyPartLevels { get; init; }  // 17 body parts, level 1-10
    public List<Skill> LearnedSkills { get; set; }                // ~50 skills max
    public Dictionary<string, int> Humors { get; set; }           // 10 humors, 0-100 range
    public List<string> Inventory { get; set; }                   // Items (placeholder)
    public List<string> Companions { get; set; }                  // Animal companions (placeholder)
    
    public Avatar()
    {
        BodyPartLevels = InitializeBodyParts();
        LearnedSkills = new List<Skill>();
        Humors = InitializeHumors();
        Inventory = new List<string>();
        Companions = new List<string>();
    }
    
    private Dictionary<string, int> InitializeBodyParts()
    {
        // 17 body parts from design doc
        var bodyParts = new[]
        {
            "Lower Limbs", "Upper Limbs", "Thorax", "Viscera", "Heart",
            "Fingers", "Feet", "Backbone", "Ears", "Eyes",
            "Tongue", "Nose", "Cerebrum", "Cerebellum", "Anamnesis",
            "Hippocampus", "Pineal Gland"
        };
        
        var random = new Random();
        return bodyParts.ToDictionary(bp => bp, bp => random.Next(1, 11)); // Random 1-10
    }
    
    private Dictionary<string, int> InitializeHumors()
    {
        // 10 humors from design doc, start at 50 (neutral)
        var humors = new[]
        {
            "Black Bile", "Yellow Bile", "Appetitus", "Melancholia", "Ether",
            "Phlegm", "Blood", "Voluptas", "Laetitia", "Euphoria"
        };
        
        return humors.ToDictionary(h => h, h => 50); // Start at midpoint
    }
    
    /// <summary>
    /// Initialize avatar with random selection of skills from registry.
    /// </summary>
    public void InitializeSkills(SkillRegistry registry, int skillCount = 50)
    {
        var random = new Random();
        var allSkills = registry.GetAllSkills();
        
        // Ensure we have at least some of each type
        var observationSkills = registry.GetObservationSkills().OrderBy(_ => random.Next()).Take(10).ToList();
        var thinkingSkills = registry.GetThinkingSkills().OrderBy(_ => random.Next()).Take(20).ToList();
        var actionSkills = registry.GetActionSkills().OrderBy(_ => random.Next()).Take(20).ToList();
        
        LearnedSkills = observationSkills.Concat(thinkingSkills).Concat(actionSkills)
            .Distinct()
            .Take(skillCount)
            .ToList();
        
        // Randomize skill levels (1-10)
        foreach (var skill in LearnedSkills)
        {
            skill.Level = random.Next(1, 11);
        }
    }
    
    // Helper queries
    public List<Skill> GetObservationSkills() => 
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Observation)).ToList();
    
    public List<Skill> GetThinkingSkills() => 
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Thinking)).ToList();
    
    public List<Skill> GetActionSkills() => 
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Action)).ToList();
    
    public Skill? GetSkillById(string skillId) =>
        LearnedSkills.FirstOrDefault(s => s.SkillId == skillId);
}
