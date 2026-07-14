namespace Cathedral.Fight.Skills;

/// <summary>Quickshot — fast ranged shot at the trunk.</summary>
public sealed class QuickshotSkill : FightingSkill
{
    public override string SkillId                => "quickshot";
    public override string DisplayName            => "Quickshot";
    public override string Description            => "Fast ranged shot at the trunk.";
    public override string RequiredModusMentisId  => "marksman";
    public override string[] SecondaryModusMentisIds => new[] { "athletics", "battlecraft" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId      => "backbone";
    public override int Range                     => 10;
    public override int MinRange                  => 2;
}
