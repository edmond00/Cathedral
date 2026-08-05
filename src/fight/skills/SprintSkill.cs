namespace Cathedral.Fight.Skills;

/// <summary>
/// Sprint — double move speed this turn.
/// <para>
/// Named "sprint", not "run": the fight UI already has a RUN AWAY button for fleeing combat, and
/// two unrelated actions called Run is how this one ended up looking broken rather than merely
/// unimplemented.
/// </para>
/// </summary>
public sealed class SprintSkill : FightingSkill
{
    public override string SkillId                => "sprint";
    public override string DisplayName            => "Sprint";
    public override string Description            => "Run flat out — cover twice the ground per Cinetic Point this turn.";
    public override string RequiredModusMentisId  => "athletics";
    public override string[] SecondaryModusMentisIds => new[] { "acrobatics", "survivalism" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("legs") };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new SprintEffect();
}
