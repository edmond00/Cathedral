namespace Cathedral.Fight.Skills;

/// <summary>Blood Lust — blood frenzy, add extra dice to all attacks this turn.</summary>
public sealed class BloodLustSkill : FightingSkill
{
    public override string SkillId                => "blood_lust_skill";
    public override string DisplayName            => "Blood Lust";
    public override string Description            => "Blood frenzy — every wound you inflict lands at its worst. Lasts the whole fight.";
    public override string RequiredModusMentisId  => "blood_lust";
    public override string[] SecondaryModusMentisIds => new[] { "rage", "ferocity" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("viscera") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new BloodLustEffect();
}
