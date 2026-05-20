namespace Cathedral.Fight.Skills;

/// <summary>Back Kick — powerful back kick targeting trunk. Pushes target back.</summary>
public sealed class BackKickSkill : FightingSkill
{
    public override string SkillId                => "back_kick";
    public override string DisplayName            => "Back Kick";
    public override string Description            => "Powerful back kick targeting trunk. Pushes target back.";
    public override string RequiredModusMentisId  => "athletics";
    public override string[] SecondaryModusMentisIds => new[] { "acrobatics", "battlecraft" };
    public override FightingMedium Medium         => FightingMedium.Organ("feet");
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new PushbackEffect(1) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
