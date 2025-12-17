namespace Cathedral.Game.Narrative.Skills;

/// <summary>
/// Brute Force - Direct physical power and simple solutions.
/// Action skill only (no persona prompt, only used for skill checks).
/// </summary>
public class BruteForceSkill : Skill
{
    public override string SkillId => "brute_force";
    public override string DisplayName => "Brute Force";
    public override SkillFunction[] Functions => new[] { SkillFunction.Action };
    public override string[] BodyParts => new[] { "Upper Limbs", "Lower Limbs" };
    
    // No persona prompt - action skills don't generate narration
}
