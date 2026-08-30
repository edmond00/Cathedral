namespace Cathedral.Fight.Skills;

/// <summary>Survival Instinct — focus survival instinct, boosts runaway dice this turn.</summary>
public sealed class SurvivalInstinctSkill : FightingSkill
{
    public override string SkillId                => "survival_instinct";
    public override string DisplayName            => "Survival Instinct";
    public override string Description            => "Fix on the way out — a failed runaway check may be retried as often as you like this turn.";
    public override string RequiredModusMentisId  => "survivalism";
    public override string[] SecondaryModusMentisIds => new[] { "iron_nerves", "vigilance" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("viscera") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new SurvivalInstinctEffect();
}
