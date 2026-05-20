namespace Cathedral.Fight.Skills;

/// <summary>Needle Thrust — precise piercing stab. Player chooses target.</summary>
public sealed class NeedleThrustSkill : FightingSkill
{
    public override string SkillId                => "needle_thrust";
    public override string DisplayName            => "Needle Thrust";
    public override string Description            => "Precise piercing stab. Player chooses target.";
    public override string RequiredModusMentisId  => "incisiveness";
    public override string[] SecondaryModusMentisIds => new[] { "swordsmanship", "tactics" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
