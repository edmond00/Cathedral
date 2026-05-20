namespace Cathedral.Fight.Skills;

/// <summary>Brute Kick — powerful low kick targeting the lower limbs. Pushes target back 1 step on hit.</summary>
public sealed class BruteKickSkill : FightingSkill
{
    public override string SkillId                => "brute_kick";
    public override string DisplayName            => "Brute Kick";
    public override string Description            => "Heavy boot to the legs. Targets lower limbs. Pushes target back.";
    public override string RequiredModusMentisId  => "brute_force";
    public override string[] SecondaryModusMentisIds => new[] { "acrobatics", "battlecraft" };
    public override FightingMedium Medium         => FightingMedium.Organ("feet");
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 2;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new PushbackEffect(1) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "lower_limbs";
}
