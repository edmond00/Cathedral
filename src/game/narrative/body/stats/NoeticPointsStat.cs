namespace Cathedral.Game.Narrative;

/// <summary>
/// Noetic Points — the pool of thinking attempts a member has per narration node (each
/// observation/thinking/action that can fail spends one).
/// Source: encephalon body part (aggregate score).
/// Formula: <c>encephalon / 2 + 3</c> (integer division) — one point per two encephalon levels,
/// plus three. An encephalon with every organ at 1 (score 5) gives 5; the human maximum of 23
/// gives 14; an encephalon of nothing, or one a wound has disabled, still gives 3.
///
/// <para>The <c>+ 3</c> is one whole chain: a look, a thought and the attempt each cost a point,
/// so three is the least a body can act on at all. It was <c>+ 1</c> while carrying an act out was
/// free; once the attempt cost its point too, a pool under 3 — any encephalon of 3 or less, a
/// starting character with one head wound — could look and think and never act, in any place.</para>
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
/// 14 points the counter is 23 columns wide and still clears the centred trespass label.</para>
/// </summary>
public class NoeticPointsStat : DerivedStat
{
    public override string Name => "noetic_points";
    public override string DisplayName => "Noetic Points";
    public override string? RelatedBodyPartId => "encephalon";

    protected override int CalculateValue(int sourceScore) => Math.Max(3, sourceScore / 2 + 3);
    public override int WorstValue => 3;
    public override string FormatValue(int value) => $"{value} pts";
}
