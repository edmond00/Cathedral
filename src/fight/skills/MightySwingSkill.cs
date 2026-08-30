using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Mighty Swing — bone-shattering swing. Player chooses target. Knocks down.</summary>
public sealed class MightySwingSkill : FightingSkill
{
    public override string SkillId                => "mighty_swing";
    public override string DisplayName            => "Mighty Swing";
    public override string Description            => "Bone-shattering swing. Player chooses target. Knocks down.";
    public override string RequiredModusMentisId  => "battlecraft";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "ferocity" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 5;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 3;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new KnockdownEffect() };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending | DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
}
