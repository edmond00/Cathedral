using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents the player's avatar with body parts, organs, skills, humors, and derived stats.
/// Tracks learned skills, current state, inventory, and companions.
/// </summary>
public class Avatar
{
    private List<BodyPart> _bodyParts;                            // 5 body part regions
    private List<BodyHumor> _humors;                              // 10 humors, 0-100 range
    
    public List<BodyPart> BodyParts => _bodyParts;
    public List<DerivedStat> DerivedStats { get; private set; }   // Derived stats computed from organ/body part scores
    
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
        DerivedStats = InitializeDerivedStats();
        Inventory = new List<string>();
        Companions = new List<string>();
    }
    
    private List<BodyPart> InitializeBodyParts()
    {
        var random = new Random();
        
        // Create 5 body part regions, each containing organs with organ parts
        var bodyParts = new List<BodyPart>
        {
            new EncephalonBodyPart(),
            new VisageBodyPart(),
            new TrunkBodyPart(),
            new UpperLimbsBodyPart(),
            new LowerLimbsBodyPart()
        };
        
        // Randomize organ part scores (1-10) — player will allocate these later
        foreach (var bp in bodyParts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    part.Score = random.Next(1, 11);
        
        return bodyParts;
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
    
    // Body hierarchy queries
    
    /// <summary>
    /// Find a body part by its id (e.g. "encephalon", "trunk").
    /// </summary>
    public BodyPart? GetBodyPartById(string id) =>
        _bodyParts.FirstOrDefault(bp => bp.Id == id);
    
    /// <summary>
    /// Find an organ by its id (e.g. "eyes", "heart") across all body parts.
    /// </summary>
    public Organ? GetOrganById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).FirstOrDefault(o => o.Id == id);
    
    /// <summary>
    /// Find an organ part by its id (e.g. "left_eye", "heart") across all organs.
    /// </summary>
    public OrganPart? GetOrganPartById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts).FirstOrDefault(p => p.Id == id);
    
    /// <summary>
    /// Get the organ score for a skill's primary organ. Used for skill checks.
    /// </summary>
    public int GetOrganScoreForSkill(Skill skill)
    {
        if (skill.Organs.Length == 0) return 0;
        var organ = GetOrganById(skill.Organs[0]);
        return organ?.Score ?? 0;
    }
    
    private List<DerivedStat> InitializeDerivedStats()
    {
        return new List<DerivedStat>
        {
            new PerceptionStat(),
            new EnduranceStat(),
            new IntellectStat(),
            new DexterityStat(),
            new WillpowerStat()
        };
    }
}
