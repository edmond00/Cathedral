namespace Cathedral.Fight.Skills;

/// <summary>Longshot — extreme range shot. Double range. Targets trunk, limbs or face.</summary>
public sealed class LongshotSkill : FightingSkill
{
    public override string SkillId                => "longshot";
    public override string DisplayName            => "Longshot";
    public override string Description            => "Extreme range shot. Double range. Targets trunk, limbs or face.";
    public override string RequiredModusMentisId  => "deadeye";
    public override string[] SecondaryModusMentisIds => new[] { "marksman", "tactics" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
    public override int Range                     => 20;
}
