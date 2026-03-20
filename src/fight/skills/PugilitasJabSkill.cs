namespace Cathedral.Fight.Skills;

/// <summary>Pugilitas Jab — fast jabbing strike with the hand. Random wound.</summary>
public sealed class PugilitasJabSkill : FightingSkill
{
    public override string SkillId                => "pugilitas_jab";
    public override string DisplayName            => "Jab";
    public override string Description            => "A fast probing strike. Random wound.";
    public override string RequiredModusMentisId  => "pugilitas";
    public override FightingMedium Medium         => FightingMedium.Organ("hands");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
