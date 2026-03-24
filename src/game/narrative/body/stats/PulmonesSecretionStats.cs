using System;
namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Pulmones (lungs) secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────

public class PulmonesBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_blood_pct";
    public override string DisplayName => "Pulmones Blood %";
    public override string? RelatedOrganId => "pulmones";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, sourceScore * 8 - 3);
}

public class PulmonesPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_phlegm_pct";
    public override string DisplayName => "Pulmones Phlegm %";
    public override string? RelatedOrganId => "pulmones";
    public override int CalculateValue(int sourceScore)
    {
        int blood  = Math.Max(0, sourceScore * 8 - 3);
        int yellow = Math.Max(0, 40 - sourceScore * 3);
        int black  = Math.Max(0, 50 - sourceScore * 5);
        return 100 - blood - yellow - black;
    }
}

public class PulmonesYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_yellow_bile_pct";
    public override string DisplayName => "Pulmones Yellow Bile %";
    public override string? RelatedOrganId => "pulmones";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 40 - sourceScore * 3);
}

public class PulmonesBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_black_bile_pct";
    public override string DisplayName => "Pulmones Black Bile %";
    public override string? RelatedOrganId => "pulmones";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 50 - sourceScore * 5);
}
