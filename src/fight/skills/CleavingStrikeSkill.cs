namespace Cathedral.Fight.Skills;

/// <summary>Cleaving Strike — wide cutting arc through face, trunk and arms.</summary>
public sealed class CleavingStrikeSkill : FightingSkill
{
    public override string SkillId                => "cleaving_strike";
    public override string DisplayName            => "Cleaving Strike";
    public override string Description            => "Wide cutting arc through face, trunk and arms.";
    public override string RequiredModusMentisId  => "swordsmanship";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "battlecraft" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
