namespace Cathedral.Fight.Skills;

/// <summary>Tail Guard — swing the spiked tail across the flank: add defence dice to the next incoming attack.</summary>
public sealed class TailGuardSkill : FightingSkill
{
    public override string SkillId                => "tail_guard";
    public override string DisplayName            => "Tail Guard";
    public override string Description            => "Swing the spiked tail across the flank — add defense dice to next incoming attack.";
    public override string RequiredModusMentisId  => "spiked_guard";
    public override string[] SecondaryModusMentisIds => new[] { "counterweight", "threat_display" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("thagomizer") };
    public override int CineticPointsCost         => 2;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 2;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Defense;
    public override WoundTargetMode WoundTargetMode => WoundTargetMode.Random;
}
