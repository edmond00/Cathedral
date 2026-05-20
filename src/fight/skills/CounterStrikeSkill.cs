namespace Cathedral.Fight.Skills;

/// <summary>Counter Strike — riposte, only executes after successfully defending a melee attack.</summary>
public sealed class CounterStrikeSkill : FightingSkill
{
    public override string SkillId                => "counter_strike";
    public override string DisplayName            => "Counter Strike";
    public override string Description            => "Riposte — only executes after successfully defending a melee attack.";
    public override string RequiredModusMentisId  => "tactics";
    public override string[] SecondaryModusMentisIds => new[] { "swordsmanship", "incisiveness" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "backbone";
}
