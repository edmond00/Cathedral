namespace Cathedral.Fight.Skills;

/// <summary>Defensive Posture — guard posture, add defense to all incoming attacks this turn.</summary>
public sealed class DefensivePostureSkill : FightingSkill
{
    public override string SkillId                => "defensive_posture";
    public override string DisplayName            => "Defensive Posture";
    public override string Description            => "Guard posture — add defense to all incoming attacks this turn.";
    public override string RequiredModusMentisId  => "vigilance";
    public override string[] SecondaryModusMentisIds => new[] { "battlecraft", "iron_nerves" };
    public override FightingMedium Medium         => FightingMedium.Organ("leg");
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.DefensePosture;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
