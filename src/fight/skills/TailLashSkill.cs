using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Tail Lash — sweep the armed tail across the legs. Knocks target down.</summary>
public sealed class TailLashSkill : FightingSkill
{
    public override string SkillId                => "tail_lash";
    public override string DisplayName            => "Tail Lash";
    public override string Description            => "Sweep the armed tail across the legs. Knocks target down.";
    public override string RequiredModusMentisId  => "tail_lashing";
    public override string[] SecondaryModusMentisIds => new[] { "counterweight", "armoured_wrath" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("thagomizer") };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new KnockdownEffect() };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Contending;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "legs,feet";
}
