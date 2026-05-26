using System;
namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Combat-related derived stats (used by the fight system)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Cinetic points — how many action-points a fighter has per turn.
/// Spending CP allows movement and skill use.
/// Source: backbone organ (trunk).
/// Formula: score × 2 (range 2–20).
/// </summary>
public class CineticPointsStat : DerivedStat
{
    public override string Name         => "cinetic_points";
    public override string DisplayName  => "Cinetic Points";
    public override string ShortDisplayName => "Cinetic Points";
    public override string? RelatedOrganId => "backbone";
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} CP";
}

/// <summary>
/// Natural defense — number of sixes an attacker must exceed to land a hit.
/// Source: lower_limbs body part (aggregate score).
/// Formula: score / 2 (range 0–5 typical).
/// </summary>
public class NaturalDefenseStat : DerivedStat
{
    public override string Name         => "natural_defense";
    public override string DisplayName  => "Natural Defense";
    public override string ShortDisplayName => "Natural Defense";
    public override string? RelatedBodyPartId => "lower_limbs";
    public override int CalculateValue(int sourceScore) => sourceScore / 2;
}

/// <summary>
/// Move speed — tiles the fighter can traverse per cinetic point during movement.
/// Source: legs organ (lower_limbs).
/// Formula: score (range 1–10).
/// </summary>
public class MoveSpeedStat : DerivedStat
{
    public override string Name         => "move_speed";
    public override string DisplayName  => "Move Speed";
    public override string ShortDisplayName => "Move Speed";
    public override string? RelatedOrganId => "legs";
    public override int CalculateValue(int sourceScore) => Math.Max(1, sourceScore);
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} tiles/CP";
}

/// <summary>
/// Runaway dice — number of dice rolled during a runaway check (need at least one six to flee).
/// Source: feet organ (lower_limbs).
/// Formula: score (1 die per foot level, range 1–10).
/// </summary>
public class RunawayDiceStat : DerivedStat
{
    public override string Name         => "runaway_dice";
    public override string DisplayName  => "Runaway Dice";
    public override string ShortDisplayName => "Runaway";
    public override string? RelatedOrganId => "feet";
    public override int CalculateValue(int sourceScore) => sourceScore;
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} dice";
}

/// <summary>
/// Initiative — base score added to a 1d6 roll at combat start to determine turn order.
/// Source: nose organ (visage).
/// Formula: score (range 1–10).
/// </summary>
public class InitiativeStat : DerivedStat
{
    public override string Name         => "initiative";
    public override string DisplayName  => "Initiative";
    public override string ShortDisplayName => "Initiative";
    public override string? RelatedOrganId => "nose";
    public override int CalculateValue(int sourceScore) => sourceScore;
    public override int MinimumValue() => 1;
}

/// <summary>
/// Damage resistance — number of dice rolled to downgrade incoming wound severity.
/// One success (4+) downgrades: High → Medium → Low.
/// Source: viscera organ (torso).
/// Formula: score / 2 (range 0–5 typical).
/// </summary>
public class DamageResistanceStat : DerivedStat
{
    public override string Name            => "damage_resistance";
    public override string DisplayName     => "Damage Resistance";
    public override string ShortDisplayName => "DR";
    public override string? RelatedOrganId => "viscera";
    public override int CalculateValue(int sourceScore) => sourceScore / 2;
    public override string FormatValue(int value) => $"{value} DR";
}

/// <summary>
/// Fight learning — dice used when attempting to learn an unknown fighting skill in combat.
/// Source: cerebellum organ (head).
/// Formula: score (range 1–10).
/// </summary>
public class FightLearningStat : DerivedStat
{
    public override string Name            => "fight_learning";
    public override string DisplayName     => "Fight Learning";
    public override string ShortDisplayName => "Learning";
    public override string? RelatedOrganId => "cerebellum";
    public override int CalculateValue(int sourceScore) => sourceScore;
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} dice";
}

/// <summary>
/// Attack range — maximum distance (in tiles) from which the fighter can use ranged skills.
/// Source: eyes organ (visage).
/// Formula: score (range 1–10).
/// </summary>
public class AttackRangeStat : DerivedStat
{
    public override string Name            => "attack_range";
    public override string DisplayName     => "Attack Range";
    public override string ShortDisplayName => "Range";
    public override string? RelatedOrganId => "eyes";
    public override int CalculateValue(int sourceScore) => sourceScore;
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} tiles";
}
