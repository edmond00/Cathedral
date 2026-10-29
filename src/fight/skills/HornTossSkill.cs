using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Horn Toss — hook under the opponent and fling them back. Pushes back two steps.</summary>
public sealed class HornTossSkill : FightingSkill
{
    public override string SkillId                => "horn_toss";
    public override string DisplayName            => "Horn Toss";
    public override string Description            => "Hook a horn under the opponent and fling them back.";
    public override string RequiredModusMentisId  => "horn_hook";
    public override string[] SecondaryModusMentisIds => new[] { "goring", "threat_display" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("horns") };
    public override int CineticPointsCost         => 3;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new PushbackEffect(2) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk";
}
