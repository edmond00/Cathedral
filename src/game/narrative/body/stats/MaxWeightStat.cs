namespace Cathedral.Game.Narrative;

/// <summary>
/// Maximum weight this character can carry — the total <see cref="WeightClass"/> cost of
/// everything held, worn, and stowed inside anything worn.
///
/// Sourced from the <b>backbone</b>: what limits a load is the spine that bears it, which is the
/// same organ <see cref="CineticPointsStat"/> draws on. The tiers are deliberately coarse and
/// widely spaced — a weak back carries a knife and a loaf, a strong one carries armour and a
/// barrel — so that the difference between them is felt rather than calculated.
///
/// Registered automatically: <c>DerivedStat.DiscoverAll()</c> finds every subclass by reflection,
/// so there is no list to add this to. Beasts have a backbone too and get it for free.
/// </summary>
public class MaxWeightStat : DerivedStat
{
    public override string  Name           => "maximum_weight";
    public override string  DisplayName    => "Maximum Weight";
    public override string  ShortDisplayName => "Max Weight";
    public override string? RelatedOrganId => "backbone";

    /// <summary>The capacity ladder, from a ruined back to the strongest one a body can have.</summary>
    private static readonly int[] Tiers = { 10, 50, 100, 150, 200 };

    /// <summary>
    /// Spreads <see cref="Tiers"/> across the backbone's <em>actual</em> range rather than an
    /// assumed one. The backbone is a single-part organ scoring 0–4, so a hardcoded ladder keyed to
    /// a 0–10 score topped out at 50 and the strongest possible back could never reach 200. Scaling
    /// to the organ's own MaxScore means the top tier is always reachable, and stays reachable if
    /// that maximum is ever retuned.
    /// </summary>
    protected override int CalculateValue(PartyMember member, int sourceScore)
    {
        int max = member.GetOrganById("backbone")?.MaxScore ?? 4;
        if (max <= 0) return Tiers[0];

        int clamped = Math.Clamp(sourceScore, 0, max);
        int index   = (int)Math.Round((clamped / (double)max) * (Tiers.Length - 1));
        return Tiers[Math.Clamp(index, 0, Tiers.Length - 1)];
    }

    /// <summary>
    /// Score-only fallback, used when no member is in hand. Assumes the current 0–4 backbone.
    /// </summary>
    protected override int CalculateValue(int sourceScore) =>
        Tiers[Math.Clamp((int)Math.Round(Math.Clamp(sourceScore, 0, 4) / 4.0 * (Tiers.Length - 1)),
                         0, Tiers.Length - 1)];

    /// <summary>
    /// A ruined backbone still carries something: dropping to zero capacity would strand the
    /// character with an inventory they cannot legally hold.
    /// </summary>
    public override int WorstValue => 10;

    public override int? BestValue => 200;

    public override string FormatValue(int value) => $"{value} wt";

    /// <summary>
    /// The capacity a given backbone score yields, without needing a character to ask. Used by
    /// <c>--item-audit</c> to show what the tiers mean in practice.
    /// </summary>
    public int PreviewValue(int backboneScore) => CalculateValue(backboneScore);
}
