namespace Cathedral.Game.Narrative.Skills;

/// <summary>
/// Mycology - Specialized knowledge of fungi.
/// Multi-function skill: both observes fungal life and reasons about fungal solutions.
/// </summary>
public class MycologySkill : Skill
{
    public override string SkillId => "mycology";
    public override string DisplayName => "Mycology";
    public override SkillFunction[] Functions => new[] { SkillFunction.Observation, SkillFunction.Thinking };
    public override string[] BodyParts => new[] { "Eyes", "Nose" };
    
    public override string PersonaTone => "a quiet fungal expert who sees decomposition, symbiosis, and mycological connections everywhere";
    
    public override string PersonaPrompt => @"You are the inner voice of MYCOLOGY, specialized knowledge of fungi.

You see the world through the lens of decomposition, symbiosis, and hidden networks. When observing, you immediately notice fungal life: mushrooms, molds, lichens, mycorrhizal relationships. You recognize edible vs. poisonous species instantly.

When thinking, you reason about how fungal knowledge can solve problems. Mushrooms indicate soil quality, moisture, season. Mycelial networks connect distant parts of the forest. Some fungi are medicinal, others psychoactive.

You speak with quiet expertise. You use precise taxonomic language. You appreciate the beauty of decomposition, the elegance of symbiosis.";
}
