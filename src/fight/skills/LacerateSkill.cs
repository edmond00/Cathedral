using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Lacerate — deep raking slash across the trunk.</summary>
public sealed class LacerateSkill : FightingSkill
{
    public override string SkillId                => "lacerate";
    public override string DisplayName            => "Lacerate";
    public override string Description            => "Deep raking slash across the trunk.";
    public override string RequiredModusMentisId  => "predator";
    public override string[] SecondaryModusMentisIds => new[] { "ferocity", "incisiveness", "blood_lust" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("claws") };
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Cutting;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk";
}
