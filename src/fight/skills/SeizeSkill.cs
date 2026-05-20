namespace Cathedral.Fight.Skills;

/// <summary>Seize — grab and restrain. Immobilizes the target.</summary>
public sealed class SeizeSkill : FightingSkill
{
    public override string SkillId                => "seize";
    public override string DisplayName            => "Seize";
    public override string Description            => "Grab and restrain. Immobilizes the target.";
    public override string RequiredModusMentisId  => "brawling";
    public override string[] SecondaryModusMentisIds => new[] { "pugilitas", "brute_force" };
    public override FightingMedium Medium         => FightingMedium.Organ("hands");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new ImmobilizeEffect() };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
