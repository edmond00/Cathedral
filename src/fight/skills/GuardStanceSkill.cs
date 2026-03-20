namespace Cathedral.Fight.Skills;

/// <summary>
/// Guard Stance — adopt a defensive posture, adding +2 Natural Defense until your next turn.
/// Costs only 1 CP and is available to any Pugilitas practitioner with hands.
/// </summary>
public sealed class GuardStanceSkill : FightingSkill
{
    public override string SkillId                => "guard_stance";
    public override string DisplayName            => "Guard Stance";
    public override string Description            => "Defensive posture. +2 DEF until your next turn.";
    public override string RequiredModusMentisId  => "pugilitas";
    public override FightingMedium Medium         => FightingMedium.Organ("hands");
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override FightingSkillEffect EffectType => FightingSkillEffect.DefensePosture;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random; // Unused for DefensePosture
}
