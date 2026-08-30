using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Throat Grip — crushing grip on the throat. Immobilizes and causes heavy bleeding.</summary>
public sealed class ThroatGripSkill : FightingSkill
{
    public override string SkillId                => "throat_grip";
    public override string DisplayName            => "Throat Grip";
    public override string Description            => "Crushing grip on the throat. Immobilizes and causes heavy bleeding.";
    public override string RequiredModusMentisId  => "predator";
    public override string[] SecondaryModusMentisIds => new[] { "ferocity", "incisiveness" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("fangs") };
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new ImmobilizeEffect(), new BleedingEffect(3) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "pulmones";
}
