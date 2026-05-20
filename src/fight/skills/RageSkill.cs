namespace Cathedral.Fight.Skills;

/// <summary>Rage — enter battle rage, refill cinetic points.</summary>
public sealed class RageSkill : FightingSkill
{
    public override string SkillId                => "rage_skill";
    public override string DisplayName            => "Rage";
    public override string Description            => "Enter battle rage — refill cinetic points.";
    public override string RequiredModusMentisId  => "rage";
    public override string[] SecondaryModusMentisIds => new[] { "ferocity", "blood_lust" };
    public override FightingMedium Medium         => FightingMedium.Organ("viscera");
    public override int CineticPointsCost         => 1;
    public override int VitalHeatCost             => 6;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Other;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
