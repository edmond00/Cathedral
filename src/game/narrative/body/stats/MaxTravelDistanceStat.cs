namespace Cathedral.Game.Narrative;

/// <summary>
/// Maximum overland travel radius — how far from the protagonist's position a waypoint
/// can be set on the world sphere. Distance is Euclidean chord length on the main sphere
/// (radius 45, subdivision 6; adjacent-cell chord ≈ 0.74 units).
/// Source: lower_limbs body region (legs + feet aggregate score, human max 10).
/// Formula: linear interpolation from 40 at score 0 up to 148 at the region's max score
/// (stored as tenths of sphere units, divide by 10 for float).
///   score 0 → 4.0 units ≈ 5 cells,  region max → 14.8 units ≈ 20 cells.
/// </summary>
public class MaxTravelDistanceStat : DerivedStat
{
    private const int MinDistance = 40;   // tenths of sphere units at score 0
    private const int MaxDistance = 148;  // tenths of sphere units at region max score

    public override string Name        => "max_travel_distance";
    public override string DisplayName => "Max Travel Distance";
    public override string? RelatedBodyPartId => "lower_limbs";

    public override int WorstValue => MinDistance;
    public override int? BestValue => MaxDistance;

    // Member-agnostic fallback: assume the human lower-limbs max of 10. The member-aware path
    // below is what actually runs, and it reads the real max off the member's own anatomy.
    protected override int CalculateValue(int sourceScore) => Interpolate(sourceScore, 10);

    protected override int CalculateValue(PartyMember member, int sourceScore)
        => Interpolate(sourceScore, member.GetBodyPartById(RelatedBodyPartId!)?.MaxScore ?? 0);

    /// <summary>Linear map of <paramref name="score"/> over 0..<paramref name="maxScore"/> onto the distance span.</summary>
    private static int Interpolate(int score, int maxScore)
        => maxScore <= 0
            ? MinDistance
            : MinDistance + (int)System.Math.Round(
                (double)(MaxDistance - MinDistance) * score / maxScore,
                System.MidpointRounding.AwayFromZero);

    public override string FormatValue(int value) => $"~{(int)(value / 7.4f)} cells";

    /// <summary>Returns the radius in sphere-space units (stored int is tenths).</summary>
    public float GetRadius(PartyMember member) => GetValue(member) / 10f;
}
