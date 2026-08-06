using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Piercing Blow — penetrating thrust. Player chooses target.</summary>
public sealed class PiercingBlowSkill : FightingSkill
{
    public override string SkillId                => "piercing_blow";
    public override string DisplayName            => "Piercing Blow";
    public override string Description            => "Penetrating thrust. Player chooses target.";
    public override string RequiredModusMentisId  => "incisiveness";
    public override string[] SecondaryModusMentisIds => new[] { "battlecraft", "brute_force" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
