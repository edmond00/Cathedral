namespace Cathedral.Fight.Skills;

/// <summary>Cover — shield cover, block all incoming attacks this turn.</summary>
public sealed class CoverSkill : FightingSkill
{
    public override string SkillId                => "cover";
    public override string DisplayName            => "Cover";
    public override string Description            => "Shield cover — block all incoming attacks this turn.";
    public override string RequiredModusMentisId  => "vigilance";
    public override string[] SecondaryModusMentisIds => new[] { "battlecraft", "iron_nerves" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.DefensePosture;
    public override bool GuardBreaksOnDamage      => true;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
