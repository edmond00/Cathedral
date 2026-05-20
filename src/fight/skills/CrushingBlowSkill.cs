namespace Cathedral.Fight.Skills;

/// <summary>Crushing Blow — devastating blow to face, trunk or head.</summary>
public sealed class CrushingBlowSkill : FightingSkill
{
    public override string SkillId                => "crushing_blow";
    public override string DisplayName            => "Crushing Blow";
    public override string Description            => "Devastating blow to face, trunk or head.";
    public override string RequiredModusMentisId  => "brute_force";
    public override string[] SecondaryModusMentisIds => new[] { "battlecraft", "iron_fist" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
