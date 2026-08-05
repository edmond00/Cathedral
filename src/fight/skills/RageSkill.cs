namespace Cathedral.Fight.Skills;

/// <summary>Rage — enter battle rage, refill cinetic points.</summary>
public sealed class RageSkill : FightingSkill
{
    public override string SkillId                => "rage_skill";
    public override string DisplayName            => "Rage";
    public override string Description            => "Give in to rage — the first blow that lands this turn restores your Cinetic Points.";
    public override string RequiredModusMentisId  => "rage";
    public override string[] SecondaryModusMentisIds => new[] { "ferocity", "blood_lust" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("viscera") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new RageEffect();
}
