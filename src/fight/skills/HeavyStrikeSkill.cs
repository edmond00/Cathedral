namespace Cathedral.Fight.Skills;

/// <summary>Heavy Strike — massive blow. Player chooses target. Knocks back.</summary>
public sealed class HeavyStrikeSkill : FightingSkill
{
    public override string SkillId                => "heavy_strike";
    public override string DisplayName            => "Heavy Strike";
    public override string Description            => "Massive blow. Player chooses target. Knocks back.";
    public override string RequiredModusMentisId  => "battlecraft";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "swordsmanship" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new PushbackEffect(1) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
