namespace Cathedral.Game.Narrative;

/// <summary>
/// Noetic Points — the pool of thinking attempts a member has per narration node (each
/// observation/thinking/action that can fail spends one).
/// Source: encephalon body part (aggregate score).
/// Formula: <c>encephalon / 2 + 1</c> (integer division), floored at 1 — one point per two
/// encephalon levels, plus one. An encephalon with every organ at 1 (score 5) gives 3; the human
/// maximum of 23 gives 12.
///
/// <para>The <c>+ 1</c> is there because a bare <c>encephalon / 2</c> would leave the weakest minds
/// on a single point, and a single point buys the one thought and nothing else — no looking closer
/// at anything, no Speak-About hand-off (which spends the speaker's own point), no second idea about
/// the same room. Those are mechanics, not luxuries.</para>
///
/// <para><b>Smaller than it once was.</b> One point per encephalon level made the pool large enough
/// that a segment was never really spent: the player could work through every object in a room and
/// still have attempts left, so nothing had to be chosen over anything else. One point per three
/// levels then proved too tight, and the divisor settled at two. Two changes carry the cost of a
/// smaller pool: a failed action no longer refills it (see
/// <c>NarrativeController.CloseNarrationSegment</c>), and the thinking phase no longer re-proposes an
/// action it has already offered — so the same handful of points reaches further into the scene than
/// the full pool used to.</para>
///
/// <para>The header draws one marker per point (<c>NarrativeUI.RenderHeader</c>), right-aligned. At
/// 12 points the counter is 21 columns wide and still clears the centred trespass label.</para>
/// </summary>
public class NoeticPointsStat : DerivedStat
{
    public override string Name => "noetic_points";
    public override string DisplayName => "Noetic Points";
    public override string? RelatedBodyPartId => "encephalon";

    protected override int CalculateValue(int sourceScore) => Math.Max(1, sourceScore / 2 + 1);
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} pts";
}
