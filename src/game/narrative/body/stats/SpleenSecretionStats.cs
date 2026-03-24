using System;
namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Spleen secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────
// Note: Melancholia humor is NOT secreted by the Spleen during normal cycles;
// it is produced only via specific narrative event triggers (HumorQueueSet.ProduceHumor).
// No Melancholia secretion stat is defined here.

public class SpleenBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_blood_pct";
    public override string DisplayName => "Spleen Blood %";
    public override string? RelatedOrganId => "spleen";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, sourceScore * 8 - 3);
}

public class SpleenPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_phlegm_pct";
    public override string DisplayName => "Spleen Phlegm %";
    public override string? RelatedOrganId => "spleen";
    public override int CalculateValue(int sourceScore)
    {
        int blood  = Math.Max(0, sourceScore * 8 - 3);
        int yellow = Math.Max(0, 40 - sourceScore * 3);
        int black  = Math.Max(0, 50 - sourceScore * 5);
        return 100 - blood - yellow - black;
    }
}

public class SpleenYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_yellow_bile_pct";
    public override string DisplayName => "Spleen Yellow Bile %";
    public override string? RelatedOrganId => "spleen";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 40 - sourceScore * 3);
}

public class SpleenBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_black_bile_pct";
    public override string DisplayName => "Spleen Black Bile %";
    public override string? RelatedOrganId => "spleen";
    public override int CalculateValue(int sourceScore) =>
        Math.Max(0, 50 - sourceScore * 5);
}
