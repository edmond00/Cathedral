namespace Cathedral.Fight.Skills;

/// <summary>Cold Blood — glacial calm in combat, suppresses fear and pain.</summary>
public sealed class ColdBloodSkill : FightingSkill
{
    public override string SkillId                => "cold_blood_skill";
    public override string DisplayName            => "Cold Blood";
    public override string Description            => "Glacial calm — any attack you turn aside ends the attacker's turn. Lasts until your next turn.";
    public override string RequiredModusMentisId  => "cold_blood";
    public override string[] SecondaryModusMentisIds => new[] { "tactics", "iron_nerves" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("viscera") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new ColdBloodEffect();
}
