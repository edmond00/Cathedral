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
    public override string ShortDisplayName => "CP";
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
    public override string ShortDisplayName => "DEF";
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
    public override string ShortDisplayName => "SPD";
    public override string? RelatedOrganId => "legs";
    public override int CalculateValue(int sourceScore) => Math.Max(1, sourceScore);
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} tiles/CP";
}

/// <summary>
/// Runaway chance — percentage chance of successfully fleeing via the arena exit.
/// Source: feet organ (lower_limbs).
/// Formula: score × 10 (range 10–100 %).
/// </summary>
public class RunawayChanceStat : DerivedStat
{
    public override string Name         => "runaway_chance";
    public override string DisplayName  => "Runaway Chance";
    public override string ShortDisplayName => "RUN";
    public override string? RelatedOrganId => "feet";
    public override int CalculateValue(int sourceScore) => sourceScore * 10;
    public override string FormatValue(int value) => $"{value}%";
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
    public override string ShortDisplayName => "INIT";
    public override string? RelatedOrganId => "nose";
    public override int CalculateValue(int sourceScore) => sourceScore;
    public override int MinimumValue() => 1;
}
