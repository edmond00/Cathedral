namespace Cathedral.Fight.Skills;

/// <summary>Driving Lunge — charging lunge into the trunk. Long charge distance.</summary>
public sealed class DrivingLungeSkill : FightingSkill
{
    public override string SkillId                => "driving_lunge";
    public override string DisplayName            => "Driving Lunge";
    public override string Description            => "Charging lunge into the trunk. Long charge distance.";
    public override string RequiredModusMentisId  => "battlecraft";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "swordsmanship" };
    public override FightingMedium Medium         => FightingMedium.Weapon;
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "backbone";
    public override int Range                     => 5;
}
