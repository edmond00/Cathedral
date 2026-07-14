namespace Cathedral.Fight.Skills;

/// <summary>Gut Ripper — disemboweling claw strike. Causes severe bleeding.</summary>
public sealed class GutRipperSkill : FightingSkill
{
    public override string SkillId                => "gut_ripper";
    public override string DisplayName            => "Gut Ripper";
    public override string Description            => "Disemboweling claw strike. Causes severe bleeding.";
    public override string RequiredModusMentisId  => "predator";
    public override string[] SecondaryModusMentisIds => new[] { "ferocity", "incisiveness" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("claws") };
    public override int CineticPointsCost         => 5;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 3;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new BleedingEffect(3) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "viscera";
}
