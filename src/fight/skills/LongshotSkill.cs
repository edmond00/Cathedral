using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Longshot — extreme range shot. Double range. Targets trunk, limbs or face.</summary>
public sealed class LongshotSkill : FightingSkill
{
    public override string SkillId                => "longshot";
    public override string DisplayName            => "Longshot";
    public override string Description            => "Extreme range shot. Double range. Targets trunk, limbs or face.";
    public override string RequiredModusMentisId  => "deadeye";
    public override string[] SecondaryModusMentisIds => new[] { "marksman", "tactics" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk,upper_limbs,lower_limbs,limbs,visage";
    /// <summary>A drawn bow's ordinary reach; Longshot doubles it, which is the whole skill.</summary>
    private const int OrdinaryBowRange = 10;
    private const int OrdinaryBowMinRange = 2;
    public override int Range                     => OrdinaryBowRange * 2;
    public override int MinRange                  => OrdinaryBowMinRange * 2;
}
