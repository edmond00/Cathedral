namespace Cathedral.Fight.Skills;

/// <summary>Run — sprint, double move speed this turn.</summary>
public sealed class RunSkill : FightingSkill
{
    public override string SkillId                => "run";
    public override string DisplayName            => "Run";
    public override string Description            => "Sprint — double move speed this turn.";
    public override string RequiredModusMentisId  => "athletics";
    public override string[] SecondaryModusMentisIds => new[] { "acrobatics", "survivalism" };
    public override FightingMedium Medium         => FightingMedium.Organ("leg");
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Other;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
