namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Hepar secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────
// These four stats describe what fraction of humors the Hepar organ secretes of
// each type. They always sum to 100 % for any given organ score.
// Percentages come from the shared HumoralSecretionTable (organ score 0–3).

public class HeparBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "hepar_blood_pct";
    public override string DisplayName => "Hepar Blood %";
    public override string? RelatedOrganId => "hepar";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BloodPct(sourceScore);
}

public class HeparPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "hepar_phlegm_pct";
    public override string DisplayName => "Hepar Phlegm %";
    public override string? RelatedOrganId => "hepar";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.PhlegmPct(sourceScore);
}

public class HeparYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "hepar_yellow_bile_pct";
    public override string DisplayName => "Hepar Yellow Bile %";
    public override string? RelatedOrganId => "hepar";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.YellowBilePct(sourceScore);
}

public class HeparBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "hepar_black_bile_pct";
    public override string DisplayName => "Hepar Black Bile %";
    public override string? RelatedOrganId => "hepar";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BlackBilePct(sourceScore);
}
