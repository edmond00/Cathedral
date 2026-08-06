using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Deadeye Shot — legendary precision shot. Player chooses target.</summary>
public sealed class DeadeyeShotSkill : FightingSkill
{
    public override string SkillId                => "deadeye_shot";
    public override string DisplayName            => "Deadeye Shot";
    public override string Description            => "Legendary precision shot. Player chooses target.";
    public override string RequiredModusMentisId  => "deadeye";
    public override string[] SecondaryModusMentisIds => new[] { "marksman", "incisiveness" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 5;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk,upper_limbs,lower_limbs,limbs,visage";
    public override int Range                     => 15;
    public override int MinRange                  => 3;
}
