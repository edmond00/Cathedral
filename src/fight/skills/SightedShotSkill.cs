using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Sighted Shot — aimed shot targeting the heart/viscera.</summary>
public sealed class SightedShotSkill : FightingSkill
{
    public override string SkillId                => "sighted_shot";
    public override string DisplayName            => "Sighted Shot";
    public override string Description            => "Aimed shot targeting the heart/viscera.";
    public override string RequiredModusMentisId  => "marksman";
    public override string[] SecondaryModusMentisIds => new[] { "deadeye", "tactics" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "heart";
    public override int Range                     => 8;
    public override int MinRange                  => 2;
}
