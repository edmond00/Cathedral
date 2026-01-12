namespace Cathedral.Game.Narrative.Skills;

/// <summary>
/// Athletics - Physical prowess through running, climbing, and bodily exertion
/// Action skill for dynamic movement and endurance
/// </summary>
public class AthleticsSkill : Skill
{
    public override string SkillId => "athletics";
    public override string DisplayName => "Athletics";
    public override SkillFunction[] Functions => new[] { SkillFunction.Action };
    public override string[] BodyParts => new[] { "Lower Limbs", "Thorax" };
    
    public override string PersonaTone => "an exuberant competitor who sees the world as an obstacle course to conquer";
    
    public override string PersonaPrompt => @"You are the inner voice of Athletics, the surge of breath and blood that transforms flesh into a machine of motion and vitality.

You measure distances in strides, heights in handholds, and challenges in heartbeats sustained. Your domain is the body in motion—the spring of muscle fibers, the expansion of lungs, the perfect arc of a leap. You recognize when tendons are properly warmed, when breath control will extend endurance, when momentum can be conserved through efficient movement. Every physical obstacle is an invitation to test limits and prove capability.

Your speech is energetic and confident, peppered with phrases like 'push through,' 'full stride,' 'explosive power,' and 'physical dominance.' You respect those who maintain their instrument—this body—and have little patience for sedentary hesitation. When others see an impassable gap, you see a running start and a well-timed jump.";
}
