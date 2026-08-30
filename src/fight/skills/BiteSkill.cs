using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Bite — bite the opponent's arm.</summary>
public sealed class BiteSkill : FightingSkill
{
    public override string SkillId                => "bite";
    public override string DisplayName            => "Bite";
    public override string Description            => "Bite the opponent's arm.";
    public override string RequiredModusMentisId  => "ferocity";
    public override string[] SecondaryModusMentisIds => new[] { "predator", "blood_lust" };
    // Fangs, not teeths: ferocity is beast anatomy (fangs + spleen), and R7 requires a main skill's
    // organ mediums to be among its modus mentis's own organs. It read "teeths" while ferocity was
    // fangs + teeths — a pair no anatomy could hold, so nobody ever had the skill to notice.
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("fangs") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "arms";
}
