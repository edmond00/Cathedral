namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Pulmones (lungs) secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────
// Percentages come from the shared HumoralSecretionTable (organ score 0–3).

public class PulmonesBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_blood_pct";
    public override string DisplayName => "Pulmones Blood %";
    public override string? RelatedOrganId => "pulmones";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BloodPct(sourceScore);
}

public class PulmonesPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_phlegm_pct";
    public override string DisplayName => "Pulmones Phlegm %";
    public override string? RelatedOrganId => "pulmones";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.PhlegmPct(sourceScore);
}

public class PulmonesYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_yellow_bile_pct";
    public override string DisplayName => "Pulmones Yellow Bile %";
    public override string? RelatedOrganId => "pulmones";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.YellowBilePct(sourceScore);
}

public class PulmonesBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "pulmones_black_bile_pct";
    public override string DisplayName => "Pulmones Black Bile %";
    public override string? RelatedOrganId => "pulmones";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BlackBilePct(sourceScore);
}
