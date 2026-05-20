namespace Cathedral.Fight.Skills;

/// <summary>High Kick — high sweeping kick targeting face or trunk.</summary>
public sealed class HighKickSkill : FightingSkill
{
    public override string SkillId                => "high_kick";
    public override string DisplayName            => "High Kick";
    public override string Description            => "High sweeping kick targeting face or trunk.";
    public override string RequiredModusMentisId  => "acrobatics";
    public override string[] SecondaryModusMentisIds => new[] { "athletics", "low_blow" };
    public override FightingMedium Medium         => FightingMedium.Organ("feet");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
