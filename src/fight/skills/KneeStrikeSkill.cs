namespace Cathedral.Fight.Skills;

/// <summary>Knee Strike — knee to the gut or groin.</summary>
public sealed class KneeStrikeSkill : FightingSkill
{
    public override string SkillId                => "knee_strike";
    public override string DisplayName            => "Knee Strike";
    public override string Description            => "Knee to the gut or groin.";
    public override string RequiredModusMentisId  => "pugilitas";
    public override string[] SecondaryModusMentisIds => new[] { "brawling", "low_blow" };
    public override FightingMedium Medium         => FightingMedium.Organ("legs");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
