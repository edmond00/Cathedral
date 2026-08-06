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
    public override string Description            => "Run flat out — cover twice the ground this turn.";
    public override string RequiredModusMentisId  => "athletics";
    public override string[] SecondaryModusMentisIds => new[] { "acrobatics", "survivalism" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Organ("legs") };

    /// <summary>
    /// Free in Cinetic Points, and it has to be — this is the one skill priced in the currency it
    /// multiplies. A movement budget is <c>CP × speed</c>, so charging a point to double the speed
    /// halves the multiplicand at the same instant it doubles the multiplier: at 2 CP the skill was
    /// exactly, measurably worth nothing, and above that it bought a third more ground rather than
    /// twice. It is still paid for, in vital heat, like every other buff.
    /// </summary>
    public override int CineticPointsCost         => 0;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 1;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Buff;

    public override FightStatusEffect CreateBuffEffect(Fighter owner) => new SprintEffect();
}
