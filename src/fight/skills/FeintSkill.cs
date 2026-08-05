namespace Cathedral.Fight.Skills;

/// <summary>
/// Feint — a deceptive strike whose sixes become dice on the next attack.
/// <para>
/// The one self-targeting skill that still rolls, and deliberately so: a feint has to convince
/// someone, so unlike a <see cref="FightingSkillEffect.Buff"/> there IS something to roll against.
/// It resolves through the ordinary attack path but causes no wound — see
/// <c>FightModeAdapter.FinishAttackResolution</c>, which converts the sixes into a
/// <see cref="FeintEffect"/> instead of applying a wound.
/// </para>
/// </summary>
public sealed class FeintSkill : FightingSkill
{
    public override string SkillId                => "feint";
    public override string DisplayName            => "Feint";
    public override string Description            => "Deceptive strike — every six rolled adds a die to your next attack this turn.";
    public override string RequiredModusMentisId  => "tactics";
    public override string[] SecondaryModusMentisIds => new[] { "incisiveness", "swordsmanship" };
    public override FightingMedium[] Mediums => new[] { FightingMedium.Weapon };
    public override int CineticPointsCost         => 1;
    public override int BaseDice                  => 0;
    public override int MediumLevelMultiplicator  => 1;
    public override int SkillLevelMultiplicator   => 3;
    public override FightingSkillEffect EffectType => FightingSkillEffect.Other;

    public override FightStatusEffect? CreateRolledEffect(Fighter owner, int sixes)
        => sixes > 0 ? new FeintEffect(sixes) : null;
}
