using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Skills;

/// <summary>Spike Drive — bring the thagomizer down point-first into the body. Causes heavy bleeding.</summary>
public sealed class SpikeDriveSkill : FightingSkill
{
    public override string SkillId                => "spike_drive";
    public override string DisplayName            => "Spike Drive";
    public override string Description            => "Bring the tail's spikes down point-first into the body. Causes heavy bleeding.";
    public override string RequiredModusMentisId  => "tail_lashing";
    public override string[] SecondaryModusMentisIds => new[] { "armoured_wrath", "counterweight" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("thagomizer") };
    public override int CineticPointsCost         => 4;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 2;
    public override FightStatusEffect[] SpecialEffects => new FightStatusEffect[] { new BleedingEffect(3) };
    public override FightingSkillEffect EffectType => FightingSkillEffect.Attack;
    public override DamageType DamageTypes         => DamageType.Piercing;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.FixedBodyPart;
    public override string? TargetBodyPartId       => "trunk";
}
