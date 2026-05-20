namespace Cathedral.Fight.Skills;

/// <summary>Parry — deflect the next incoming melee attack.</summary>
public sealed class ParrySkill : FightingSkill
{
    public override string SkillId                => "parry";
    public override string DisplayName            => "Parry";
    public override string Description            => "Deflect the next incoming melee attack.";
    public override string RequiredModusMentisId  => "vigilance";
    public override string[] SecondaryModusMentisIds => new[] { "battlecraft", "swordsmanship" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Defense;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
