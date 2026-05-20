namespace Cathedral.Fight.Skills;

/// <summary>Scratch — quick claw strike.</summary>
public sealed class ScratchSkill : FightingSkill
{
    public override string SkillId                => "scratch";
    public override string DisplayName            => "Scratch";
    public override string Description            => "Quick claw strike.";
    public override string RequiredModusMentisId  => "ferocity";
    public override string[] SecondaryModusMentisIds => new[] { "predator", "brawling" };
    public override FightingMedium Medium         => FightingMedium.Organ("claws");
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
