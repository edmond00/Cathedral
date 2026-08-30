using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Trip — sweep the legs. Knocks target down.</summary>
public sealed class TripSkill : FightingSkill
{
    public override string SkillId                => "trip";
    public override string DisplayName            => "Trip";
    public override string Description            => "Sweep the legs. Knocks target down.";
    public override string RequiredModusMentisId  => "low_blow";
    public override string[] SecondaryModusMentisIds => new[] { "brawling", "acrobatics" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("feet") };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new KnockdownEffect() };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "legs,feet";
}
