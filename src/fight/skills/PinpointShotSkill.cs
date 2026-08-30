using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Pinpoint Shot — precise aimed shot. Player chooses target.</summary>
public sealed class PinpointShotSkill : FightingSkill
{
    public override string SkillId                => "pinpoint_shot";
    public override string DisplayName            => "Pinpoint Shot";
    public override string Description            => "Precise aimed shot. Player chooses target.";
    public override string RequiredModusMentisId  => "marksman";
    public override string[] SecondaryModusMentisIds => new[] { "deadeye", "tactics" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
    public override int Range                     => 8;
    public override int MinRange                  => 2;
}
