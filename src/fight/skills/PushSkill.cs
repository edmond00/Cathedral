namespace Cathedral.Fight.Skills;

/// <summary>Push — shove the opponent back one step.</summary>
public sealed class PushSkill : FightingSkill
{
    public override string SkillId                => "push";
    public override string DisplayName            => "Push";
    public override string Description            => "Shove the opponent back one step.";
    public override string RequiredModusMentisId  => "brute_force";
    public override string[] SecondaryModusMentisIds => new[] { "brawling", "athletics" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("arms") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new PushbackEffect(1) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "backbone";
}
