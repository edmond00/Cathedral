using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Chokehold — strangling hold targeting lungs and face. Immobilizes.</summary>
public sealed class ChokeholdSkill : FightingSkill
{
    public override string SkillId                => "chokehold";
    public override string DisplayName            => "Chokehold";
    public override string Description            => "Strangling hold targeting lungs and face. Immobilizes.";
    public override string RequiredModusMentisId  => "brawling";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "predator" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.BodyPart("upper_limbs") };
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new ImmobilizeEffect() };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "pulmones,visage";
}
