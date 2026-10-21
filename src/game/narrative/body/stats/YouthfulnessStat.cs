namespace Cathedral.Game.Narrative;

/// <summary>
/// Youthfulness — the residual vigour of youth, derived from the heart organ.
/// Range 0–50 %. Decides what fraction of the starting humor queues are seeded with
/// <see cref="JuvenescenceHumor"/>: in full for the protagonist at creation, and scaled by the part of
/// the lifetime still ahead for everyone generated in the world (<see cref="PartyMember.JuvenescencePercentForAge"/>).
/// Source: heart organ score (0–5, see <see cref="HeartOrgan.HeartPart"/>).
/// Formula: score * 10 → 0 % … 50 %.
/// </summary>
public sealed class YouthfulnessStat : DerivedStat
{
    public override string Name => "youthfulness";
    public override string DisplayName => "Youthfulness";
    public override string? RelatedOrganId => "heart";
    // Heart's single part maxes at 5; score 5 → 50 %.
    protected override int CalculateValue(int sourceScore) => sourceScore * 10;
    public override int? BestValue => 50;
    public override string FormatValue(int value) => $"{value}%";
}
