namespace Cathedral.Fight.Skills;

/// <summary>Flesh Clamp — crushing bite that immobilizes.</summary>
public sealed class FleshClampSkill : FightingSkill
{
    public override string SkillId                => "flesh_clamp";
    public override string DisplayName            => "Flesh Clamp";
    public override string Description            => "Crushing bite that immobilizes.";
    public override string RequiredModusMentisId  => "predator";
    public override string[] SecondaryModusMentisIds => new[] { "ferocity", "brawling" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("fangs") };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new ImmobilizeEffect() };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
