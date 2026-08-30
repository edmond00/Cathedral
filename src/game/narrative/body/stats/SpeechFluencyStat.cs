namespace Cathedral.Game.Narrative;

/// <summary>
/// Speech Fluency — max number of player dialogue replicas available per conversation turn.
/// Derived from the tongue organ (visage).
/// Formula: 1 reply per tongue level, bounded to 0–5. A tongue at 0 (or wound-disabled) means
/// 0 replies — the character cannot hold a conversation at all (see ZeroRepliesDialogueRule).
/// </summary>
public class SpeechFluencyStat : DerivedStat
{
    public override string Name         => "speech fluency";
    public override string DisplayName  => "Speech Fluency";
    public override string? RelatedOrganId => "tongue";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 0;
    public override int? BestValue => 5;
    public override string FormatValue(int value) => $"{value} replies";
}
