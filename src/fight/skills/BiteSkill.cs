namespace Cathedral.Fight.Skills;

/// <summary>Bite — bite the opponent's arm.</summary>
public sealed class BiteSkill : FightingSkill
{
    public override string SkillId                => "bite";
    public override string DisplayName            => "Bite";
    public override string Description            => "Bite the opponent's arm.";
    public override string RequiredModusMentisId  => "ferocity";
    public override string[] SecondaryModusMentisIds => new[] { "predator", "blood_lust" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("teeths") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "upper_limbs";
}
