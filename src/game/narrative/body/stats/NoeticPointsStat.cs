namespace Cathedral.Game.Narrative;

/// <summary>
/// Noetic Points — the pool of thinking attempts a member has per narration node (each
/// observation/thinking/action that can fail spends one).
/// Source: encephalon body part (aggregate score).
/// Formula: one point per three encephalon levels, <b>rounded up</b> and floored at 1.
///
/// <para>Rounded up rather than down because flooring lands a starting character on exactly 1, and a
/// single point buys the one thought and nothing else — no looking closer at anything, no Speak-About
/// hand-off (which spends the speaker's own point), no second idea about the same room. Those are
/// mechanics, not luxuries. Ceiling keeps two.</para>
///
/// <para><b>A third of what it was.</b> One point per encephalon level made the pool large enough
/// that a segment was never really spent: the player could work through every object in a room and
/// still have attempts left, so nothing had to be chosen over anything else. Divided by three, a
/// point is a decision. Two changes carry the cost: a failed action no longer refills the pool
/// (see <c>NarrativeController.CloseNarrationSegment</c>), and the thinking phase no longer
/// re-proposes an action it has already offered — so the same handful of points reaches further
/// into the scene than the full pool used to.</para>
/// </summary>
public class NoeticPointsStat : DerivedStat
{
    public override string Name => "noetic_points";
    public override string DisplayName => "Noetic Points";
    public override string? RelatedBodyPartId => "encephalon";

    protected override int CalculateValue(int sourceScore) => Math.Max(1, (sourceScore + 3) / 3);
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} pts";
}
