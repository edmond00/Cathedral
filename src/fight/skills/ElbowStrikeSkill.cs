namespace Cathedral.Fight.Skills;

/// <summary>Elbow Strike — vicious elbow strike to face or trunk.</summary>
public sealed class ElbowStrikeSkill : FightingSkill
{
    public override string SkillId                => "elbow_strike";
    public override string DisplayName            => "Elbow Strike";
    public override string Description            => "Vicious elbow strike to face or trunk.";
    public override string RequiredModusMentisId  => "brawling";
    public override string[] SecondaryModusMentisIds => new[] { "pugilitas", "iron_fist" };
    public override FightingMedium Medium         => FightingMedium.Organ("arm");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
