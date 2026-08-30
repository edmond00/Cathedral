using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Punch — basic punch. Player chooses target.</summary>
public sealed class PunchSkill : FightingSkill
{
    public override string SkillId                => "punch";
    public override string DisplayName            => "Punch";
    public override string Description            => "Basic punch. Player chooses target.";
    public override string RequiredModusMentisId  => "pugilitas";
    public override string[] SecondaryModusMentisIds => new[] { "brawling", "brute_force" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("hands") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
