namespace Cathedral.Fight.Skills;

/// <summary>Smash — direct blow to face, trunk or head.</summary>
public sealed class SmashSkill : FightingSkill
{
    public override string SkillId                => "smash";
    public override string DisplayName            => "Smash";
    public override string Description            => "Direct blow to face, trunk or head.";
    public override string RequiredModusMentisId  => "brute_force";
    public override string[] SecondaryModusMentisIds => new[] { "battlecraft", "brawling" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
