using System;
namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Paunch (stomach) secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────

public class PaunchBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_blood_pct";
    public override string DisplayName => "Paunch Blood %";
    public override string? RelatedOrganId => "paunch";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, sourceScore * 8 - 3);
}

public class PaunchPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_phlegm_pct";
    public override string DisplayName => "Paunch Phlegm %";
    public override string? RelatedOrganId => "paunch";
    public override int CalculateValue(int sourceScore)
    {
        int blood  = Math.Max(0, sourceScore * 8 - 3);
        int yellow = Math.Max(0, 40 - sourceScore * 3);
        int black  = Math.Max(0, 50 - sourceScore * 5);
        return 100 - blood - yellow - black;
    }
}

public class PaunchYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_yellow_bile_pct";
    public override string DisplayName => "Paunch Yellow Bile %";
    public override string? RelatedOrganId => "paunch";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 40 - sourceScore * 3);
}

public class PaunchBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_black_bile_pct";
    public override string DisplayName => "Paunch Black Bile %";
    public override string? RelatedOrganId => "paunch";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 50 - sourceScore * 5);
}
