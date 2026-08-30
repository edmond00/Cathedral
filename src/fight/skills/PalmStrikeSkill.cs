using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Palm Strike — devastating open-hand strike.</summary>
public sealed class PalmStrikeSkill : FightingSkill
{
    public override string SkillId                => "palm_strike";
    public override string DisplayName            => "Palm Strike";
    public override string Description            => "Devastating open-hand strike.";
    public override string RequiredModusMentisId  => "iron_fist";
    public override string[] SecondaryModusMentisIds => new[] { "pugilitas", "battlecraft" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("hands") };
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "visage,trunk";
}
