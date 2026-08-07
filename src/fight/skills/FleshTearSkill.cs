using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Flesh Tear — savage tearing bite/scratch. Causes bleeding.</summary>
public sealed class FleshTearSkill : FightingSkill
{
    public override string SkillId                => "flesh_tear";
    public override string DisplayName            => "Flesh Tear";
    public override string Description            => "Savage tearing bite/scratch. Causes bleeding.";
    public override string RequiredModusMentisId  => "ferocity";
    public override string[] SecondaryModusMentisIds => new[] { "predator", "blood_lust" };
    // Fangs alone: ferocity is beast anatomy (fangs + spleen) and R7 keeps a main skill's organ
    // mediums inside its modus mentis's organs. A human jaw does not do this anyway — human tearing
    // is brawling's business.
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("fangs") };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new BleedingEffect(1) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Cutting;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk,upper_limbs";
}
