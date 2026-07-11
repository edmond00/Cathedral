namespace Cathedral.Game.Narrative;

/// <summary>
/// Maximum overland travel radius — how far from the protagonist's position a waypoint
/// can be set on the world sphere. Distance is Euclidean chord length on the main sphere
/// (radius 45, subdivision 6; adjacent-cell chord ≈ 0.74 units).
/// Source: feet organ (lower_limbs).
/// Formula: score × 18 + 40  (stored as tenths of sphere units, divide by 10 for float).
///   score 0 → 4.0 units ≈  5 cells,  score 6 → 14.8 units ≈ 20 cells,
///   score 10 → 22.0 units ≈ 30 cells.
/// </summary>
public class MaxTravelDistanceStat : DerivedStat
{
    public override string Name        => "max_travel_distance";
    public override string DisplayName => "Max Travel Distance";
    public override string? RelatedOrganId => "feet";

    protected override int CalculateValue(int sourceScore) => sourceScore * 18 + 40;
    public override int WorstValue => 40;

    public override string FormatValue(int value) => $"~{(int)(value / 7.4f)} cells";

    /// <summary>Returns the radius in sphere-space units (stored int is tenths).</summary>
    public float GetRadius(PartyMember member) => GetValue(member) / 10f;
}
