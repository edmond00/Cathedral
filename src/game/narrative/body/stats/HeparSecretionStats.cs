using System;
namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Hepar secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────
// These four stats describe what fraction of humors the Hepar organ secretes of
// each type. They always sum to 100 % for any given organ score (1-10).
//
// Formulae (score 1-10):
//   Blood    % = max(0, score * 8 - 3)
//   Yellow   % = max(0, 40 - score * 3)
//   Black    % = max(0, 50 - score * 5)
//   Phlegm   % = 100 - other three         (always 13 %)

public class HeparBloodSecretionStat : DerivedStat
{
    public override string Name        => "hepar_blood_pct";
    public override string DisplayName => "Hepar Blood %";
    public override string? RelatedOrganId => "hepar";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, sourceScore * 8 - 3);
}

public class HeparPhlegmSecretionStat : DerivedStat
{
    public override string Name        => "hepar_phlegm_pct";
    public override string DisplayName => "Hepar Phlegm %";
    public override string? RelatedOrganId => "hepar";
    public override int CalculateValue(int sourceScore)
    {
        int blood  = Math.Max(0, sourceScore * 8 - 3);
        int yellow = Math.Max(0, 40 - sourceScore * 3);
        int black  = Math.Max(0, 50 - sourceScore * 5);
        return 100 - blood - yellow - black;
    }
}

public class HeparYellowBileSecretionStat : DerivedStat
{
    public override string Name        => "hepar_yellow_bile_pct";
    public override string DisplayName => "Hepar Yellow Bile %";
    public override string? RelatedOrganId => "hepar";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 40 - sourceScore * 3);
}

public class HeparBlackBileSecretionStat : DerivedStat
{
    public override string Name        => "hepar_black_bile_pct";
    public override string DisplayName => "Hepar Black Bile %";
    public override string? RelatedOrganId => "hepar";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 50 - sourceScore * 5);
}
