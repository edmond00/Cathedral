namespace Cathedral.Fight.Skills;

/// <summary>Uppercut — explosive rising fist. Attacker picks the target body part.</summary>
public sealed class UppercutStrikeSkill : FightingSkill
{
    public override string SkillId                => "uppercut_strike";
    public override string DisplayName            => "Uppercut";
    public override string Description            => "Rising strike. You choose the target body part.";
    public override string RequiredModusMentisId  => "uppercut";
    public override string[] SecondaryModusMentisIds => new[] { "pugilitas", "iron_fist" };
    public override FightingMedium Medium         => FightingMedium.Organ("hands");
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 4;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
