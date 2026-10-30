using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Impale — drive the horn through and hold the opponent on it. Immobilizes and causes heavy bleeding.</summary>
public sealed class ImpaleSkill : FightingSkill
{
    public override string SkillId                => "impale";
    public override string DisplayName            => "Impale";
    public override string Description            => "Drive the horn through and hold the opponent on it. Immobilizes and causes heavy bleeding.";
    public override string RequiredModusMentisId  => "horn_hook";
    public override string[] SecondaryModusMentisIds => new[] { "goring", "armoured_wrath" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("horns") };
    public override int CineticPointsCost         => 5;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 3;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new ImmobilizeEffect(), new BleedingEffect(2) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "viscera";
}
