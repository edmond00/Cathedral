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
    public override string? RelatedOrganId => "backbone";
    protected override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override int WorstValue => 1;
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
    public override string DisplayName  => "Natural Defence";
    public override string? RelatedBodyPartId => "lower_limbs";
    protected override int CalculateValue(int sourceScore) => sourceScore / 3;
    public override string FormatValue(int value) => $"{value} dice";
}

/// <summary>
/// Natural attack — bonus attack dice added to every offensive fighting-skill roll,
/// the raw virile vigour behind any blow.
/// Source: genitories organ (trunk).
/// Formula: score (range 0–2).
/// </summary>
public class NaturalAttackStat : DerivedStat
{
    public override string Name         => "natural_attack";
    public override string DisplayName  => "Natural Attack";
    public override string? RelatedOrganId => "genitories";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override string FormatValue(int value) => $"{value} dice";
}

/// <summary>
/// Move speed — tiles the fighter can traverse per cinetic point during movement.
/// Source: legs organ (lower_limbs), aggregate of both legs (0–4).
/// Table: 0 → 1, 1 → 2, 2 → 4, 3 → 6, 4 → 8 tiles.
/// </summary>
public class MoveSpeedStat : DerivedStat
{
    private static readonly int[] TilesByScore = { 1, 2, 4, 6, 8 };

    public override string Name         => "move_speed";
    public override string DisplayName  => "Move Speed";
    public override string? RelatedOrganId => "legs";
    protected override int CalculateValue(int sourceScore) =>
        TilesByScore[Math.Clamp(sourceScore, 0, TilesByScore.Length - 1)];
    public override int WorstValue => 1;
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
    public override string? RelatedOrganId => "feet";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} dice";
}

/// <summary>
/// Equilibrium — resistance to losing footing on treacherous/dangerous terrain during
/// fight movement. Higher value = lower chance to slip and lose the turn.
/// Source: feet organ (lower_limbs).
/// Formula: score (range 1–10).
/// </summary>
public class EquilibriumStat : DerivedStat
{
    public override string Name         => "equilibrium";
    public override string DisplayName  => "Equilibrium";
    public override string? RelatedOrganId => "feet";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} pts";
}

/// <summary>
/// Knockdown recovery — number of d6 rolled at the start of a knocked-down fighter's turn.
/// At least one six is needed to recover. Gathering yourself off the ground is an act of
/// self-possession, so the pineal gland (the seat of inward regard) governs it — this also keeps
/// the heart free to govern lifespan alone (see <see cref="LifetimeStat"/>).
/// Source: pineal gland organ (encephalon).
/// Formula: score (range 1–3).
/// </summary>
public class KnockdownRecoveryStat : DerivedStat
{
    public override string Name         => "knockdown_recovery";
    public override string DisplayName  => "Knockdown Recovery";
    public override string? RelatedOrganId => "pineal_gland";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} dice";
}

/// <summary>
/// Initiative — base score added to a 1d6 roll at combat start to determine turn order.
/// Source: ears organ (visage).
/// Formula: score (range 1–10).
/// </summary>
public class InitiativeStat : DerivedStat
{
    public override string Name         => "initiative";
    public override string DisplayName  => "Initiative";
    public override string? RelatedOrganId => "ears";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"+{value}";
}

// The viscera's combat stat used to be `damage_resistance`, a hidden roll that downgraded a wound's
// severity after the blow had already landed. The player never saw it fire and could not plan
// around it. Constitution is expressed over the long run instead — see WoundHealingStat, which took
// over this slot.

/// <summary>
/// Fight learning — dice used when attempting to learn an unknown fighting skill in combat.
/// Source: cerebellum organ (head).
/// Formula: score (range 1–10).
/// </summary>
public class FightLearningStat : DerivedStat
{
    public override string Name            => "fight_learning";
    public override string DisplayName     => "Fight Learning";
    public override string? RelatedOrganId => "cerebellum";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
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
    public override string? RelatedOrganId => "eyes";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} tiles";
}
