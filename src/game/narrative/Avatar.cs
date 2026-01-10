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
    private List<BodyPart> _bodyParts;                            // 17 body parts, level 1-10 (for skill checks)
    private List<BodyHumor> _humors;                              // 10 humors, 0-100 range
    
    public List<BodyPart> BodyParts => _bodyParts;
    public Dictionary<string, int> BodyPartLevels =>              // Backward compatibility: name -> level
        _bodyParts.ToDictionary(bp => bp.Name, bp => bp.Level);
    
    public List<Skill> Skills { get; set; }                       // Current skills (for execution)
    public List<Skill> LearnedSkills { get; set; }                // Alias for Skills
    
    public List<BodyHumor> Humors => _humors;
    public Dictionary<string, int> HumorValues =>                 // Backward compatibility: name -> value
        _humors.ToDictionary(h => h.Name, h => h.Value);
    
    public List<string> Inventory { get; set; }                   // Items
    public List<string> Companions { get; set; }                  // Animal companions
    public int CurrentLocationId { get; set; }                    // Current location ID (used as RNG seed)
    
    public Avatar()
    {
        _bodyParts = InitializeBodyParts();
        Skills = new List<Skill>();
        LearnedSkills = Skills;      // Same reference
        _humors = InitializeHumors();
        Inventory = new List<string>();
        Companions = new List<string>();
    }
    
    private List<BodyPart> InitializeBodyParts()
    {
        var random = new Random();
        
        // Create 17 body parts from design doc with random levels 1-10
        return new List<BodyPart>
        {
            new LowerLimbs(random.Next(1, 11)),
            new UpperLimbs(random.Next(1, 11)),
            new Thorax(random.Next(1, 11)),
            new Viscera(random.Next(1, 11)),
            new Heart(random.Next(1, 11)),
            new Fingers(random.Next(1, 11)),
            new Feet(random.Next(1, 11)),
            new Backbone(random.Next(1, 11)),
            new Ears(random.Next(1, 11)),
            new Eyes(random.Next(1, 11)),
            new Tongue(random.Next(1, 11)),
            new Nose(random.Next(1, 11)),
            new Cerebrum(random.Next(1, 11)),
            new Cerebellum(random.Next(1, 11)),
            new Anamnesis(random.Next(1, 11)),
            new Hippocampus(random.Next(1, 11)),
            new PinealGland(random.Next(1, 11))
        };
    }
    
    private List<BodyHumor> InitializeHumors()
    {
        // 10 humors from design doc, start at 50 (neutral)
        return new List<BodyHumor>
        {
            new BlackBile(),
            new YellowBile(),
            new Appetitus(),
            new Melancholia(),
            new Ether(),
            new Phlegm(),
            new Blood(),
            new Voluptas(),
            new Laetitia(),
            new Euphoria()
        };
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
        
        var selectedSkills = observationSkills.Concat(thinkingSkills).Concat(actionSkills)
            .Distinct()
            .Take(skillCount)
            .ToList();
        
        // Clear and repopulate the existing list to maintain the reference
        Skills.Clear();
        Skills.AddRange(selectedSkills);
        
        // Randomize skill levels (1-10)
        foreach (var skill in Skills)
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
