using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Forward Lunge — charging thrust. Can close distance before striking.</summary>
public sealed class ForwardLungeSkill : FightingSkill
{
    public override string SkillId                => "forward_lunge";
    public override string DisplayName            => "Forward Lunge";
    public override string Description            => "Charging thrust. Can close distance before striking.";
    public override string RequiredModusMentisId  => "swordsmanship";
    public override string[] SecondaryModusMentisIds => new[] { "athletics", "battlecraft" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk";
    public override int Range                     => 3;
    public override int ChargeDistance            => 3;
}
