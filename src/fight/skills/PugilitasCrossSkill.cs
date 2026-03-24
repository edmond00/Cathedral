namespace Cathedral.Fight.Skills;

/// <summary>Pugilitas Cross — committed straight punch targeting the upper limbs.</summary>
public sealed class PugilitasCrossSkill : FightingSkill
{
    public override string SkillId                => "pugilitas_cross";
    public override string DisplayName            => "Cross";
    public override string Description            => "Heavy straight punch. Targets arms.";
    public override string RequiredModusMentisId  => "pugilitas";
    public override FightingMedium Medium         => FightingMedium.Organ("hands");
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "upper_limbs";
}
