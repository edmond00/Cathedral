namespace Cathedral.Fight.Skills;

/// <summary>Dodge — evasive step, add defense dice to next incoming attack.</summary>
public sealed class DodgeSkill : FightingSkill
{
    public override string SkillId                => "dodge";
    public override string DisplayName            => "Dodge";
    public override string Description            => "Evasive step — add defense dice to next incoming attack.";
    public override string RequiredModusMentisId  => "acrobatics";
    public override string[] SecondaryModusMentisIds => new[] { "athletics", "vigilance" };
    public override FightingMedium Medium         => FightingMedium.Organ("legs");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Defense;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
