using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Uppercut — rising strike targeting head.</summary>
public sealed class UppercutSkill : FightingSkill
{
    public override string SkillId                => "uppercut";
    public override string DisplayName            => "Uppercut";
    public override string Description            => "Rising strike targeting head.";
    public override string RequiredModusMentisId  => "uppercut";
    public override string[] SecondaryModusMentisIds => new[] { "pugilitas", "iron_fist" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("hands") };
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "visage,encephalon";
}
