namespace Cathedral.Fight.Skills;

/// <summary>Jump — leap over obstacles, move ignoring intermediate terrain.</summary>
public sealed class JumpSkill : FightingSkill
{
    public override string SkillId                => "jump";
    public override string DisplayName            => "Jump";
    public override string Description            => "Leap over obstacles — move ignoring intermediate terrain.";
    public override string RequiredModusMentisId  => "athletics";
    public override string[] SecondaryModusMentisIds => new[] { "acrobatics", "brute_force" };
    public override FightingMedium Medium         => FightingMedium.Organ("leg");
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Other;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
