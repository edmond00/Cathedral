namespace Cathedral.Fight.Skills;

/// <summary>Chop — chopping strike to upper or lower limbs.</summary>
public sealed class ChopSkill : FightingSkill
{
    public override string SkillId                => "chop";
    public override string DisplayName            => "Chop";
    public override string Description            => "Chopping strike to upper or lower limbs.";
    public override string RequiredModusMentisId  => "battlecraft";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "swordsmanship" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
