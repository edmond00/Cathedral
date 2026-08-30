using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Deep Pierce — deep stabbing thrust. Player chooses target. Causes bleeding.</summary>
public sealed class DeepPierceSkill : FightingSkill
{
    public override string SkillId                => "deep_pierce";
    public override string DisplayName            => "Deep Pierce";
    public override string Description            => "Deep stabbing thrust. Player chooses target. Causes bleeding.";
    public override string RequiredModusMentisId  => "incisiveness";
    public override string[] SecondaryModusMentisIds => new[] { "swordsmanship", "predator" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 5;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 3;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new BleedingEffect(3) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
