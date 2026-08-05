namespace Cathedral.Fight.Skills;

/// <summary>Iron Nerves — absolute composure, allows repeating a skill already used this turn.</summary>
public sealed class IronNervesSkill : FightingSkill
{
    public override string SkillId                => "iron_nerves_skill";
    public override string DisplayName            => "Iron Nerves";
    public override string Description            => "Absolute composure — skills already used this turn may be used again.";
    public override string RequiredModusMentisId  => "iron_nerves";
    public override string[] SecondaryModusMentisIds => new[] { "cold_blood", "vigilance" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("viscera") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new IronNervesEffect();
}
