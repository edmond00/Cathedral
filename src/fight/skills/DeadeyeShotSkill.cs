namespace Cathedral.Fight.Skills;

/// <summary>Deadeye Shot — legendary precision shot. Player chooses target.</summary>
public sealed class DeadeyeShotSkill : FightingSkill
{
    public override string SkillId                => "deadeye_shot";
    public override string DisplayName            => "Deadeye Shot";
    public override string Description            => "Legendary precision shot. Player chooses target.";
    public override string RequiredModusMentisId  => "deadeye";
    public override string[] SecondaryModusMentisIds => new[] { "marksman", "incisiveness" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 5;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.PlayerChooses;
    public override int Range                     => 15;
    public override int MinRange                  => 3;
}
