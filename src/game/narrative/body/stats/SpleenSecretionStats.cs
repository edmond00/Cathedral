namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Spleen secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────
// Note: Melancholia humor is NOT secreted by the Spleen during normal cycles;
// it is produced only via specific narrative event triggers (HumorQueueSet.ProduceHumor).
// No Melancholia secretion stat is defined here.
// Percentages come from the shared HumoralSecretionTable (organ score 0–3).

public class SpleenBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_blood_pct";
    public override string DisplayName => "Spleen Blood %";
    public override string? RelatedOrganId => "spleen";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BloodPct(sourceScore);
}

public class SpleenPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_phlegm_pct";
    public override string DisplayName => "Spleen Phlegm %";
    public override string? RelatedOrganId => "spleen";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.PhlegmPct(sourceScore);
}

public class SpleenYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_yellow_bile_pct";
    public override string DisplayName => "Spleen Yellow Bile %";
    public override string? RelatedOrganId => "spleen";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.YellowBilePct(sourceScore);
}

public class SpleenBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "spleen_black_bile_pct";
    public override string DisplayName => "Spleen Black Bile %";
    public override string? RelatedOrganId => "spleen";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BlackBilePct(sourceScore);
}
