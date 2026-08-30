using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Snap Thrust — fast stabbing thrust.</summary>
public sealed class SnapThrustSkill : FightingSkill
{
    public override string SkillId                => "snap_thrust";
    public override string DisplayName            => "Snap Thrust";
    public override string Description            => "Fast stabbing thrust.";
    public override string RequiredModusMentisId  => "swordsmanship";
    public override string[] SecondaryModusMentisIds => new[] { "incisiveness", "battlecraft" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk,upper_limbs,visage";
}
