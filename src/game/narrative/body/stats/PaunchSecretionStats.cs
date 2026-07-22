namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Paunch (stomach) secretion-percentage derived stats
// ─────────────────────────────────────────────────────────────────────────────
// Percentages come from the shared HumoralSecretionTable (organ score 0–3).

public class PaunchBloodSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_blood_pct";
    public override string DisplayName => "Paunch Blood %";
    public override string? RelatedOrganId => "paunch";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BloodPct(sourceScore);
}

public class PaunchPhlegmSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_phlegm_pct";
    public override string DisplayName => "Paunch Phlegm %";
    public override string? RelatedOrganId => "paunch";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.PhlegmPct(sourceScore);
}

public class PaunchYellowBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_yellow_bile_pct";
    public override string DisplayName => "Paunch Yellow Bile %";
    public override string? RelatedOrganId => "paunch";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.YellowBilePct(sourceScore);
}

public class PaunchBlackBileSecretionStat : HumoralSecretionStat
{
    public override string Name        => "paunch_black_bile_pct";
    public override string DisplayName => "Paunch Black Bile %";
    public override string? RelatedOrganId => "paunch";
    protected override int CalculateValue(int sourceScore) =>
        HumoralSecretionTable.BlackBilePct(sourceScore);
}
