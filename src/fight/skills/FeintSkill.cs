namespace Cathedral.Fight.Skills;

/// <summary>Feint — deceptive strike, 6s rolled add to next attack this turn.</summary>
public sealed class FeintSkill : FightingSkill
{
    public override string SkillId                => "feint";
    public override string DisplayName            => "Feint";
    public override string Description            => "Deceptive strike — 6s rolled add to next attack this turn.";
    public override string RequiredModusMentisId  => "tactics";
    public override string[] SecondaryModusMentisIds => new[] { "incisiveness", "swordsmanship" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Other;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
