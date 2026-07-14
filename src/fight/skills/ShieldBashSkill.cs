namespace Cathedral.Fight.Skills;

/// <summary>Shield Bash — bash with the shield targeting face or trunk.</summary>
public sealed class ShieldBashSkill : FightingSkill
{
    public override string SkillId                => "shield_bash";
    public override string DisplayName            => "Shield Bash";
    public override string Description            => "Bash with the shield targeting face or trunk.";
    public override string RequiredModusMentisId  => "battlecraft";
    public override string[] SecondaryModusMentisIds => new[] { "brute_force", "brawling" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
