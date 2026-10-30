using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Gore — head-down charge, the horn driven into the body. Causes bleeding.</summary>
public sealed class GoreSkill : FightingSkill
{
    public override string SkillId                => "gore";
    public override string DisplayName            => "Gore";
    public override string Description            => "Head-down horn thrust into the body. Causes bleeding.";
    public override string RequiredModusMentisId  => "goring";
    public override string[] SecondaryModusMentisIds => new[] { "rival_reading", "armoured_wrath" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("horns") };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new BleedingEffect(1) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk";
}
