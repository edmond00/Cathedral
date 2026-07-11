namespace Cathedral.Game.Narrative;

/// <summary>
/// Speech Fluency — max number of player dialogue replicas available per conversation turn.
/// Derived from the tongue organ (visage).
/// Formula: 1 + floor(organScore / 2), bounded to 1–5.
/// </summary>
public class SpeechFluencyStat : DerivedStat
{
    public override string Name         => "speech fluency";
    public override string DisplayName  => "Speech Fluency";
    public override string? RelatedOrganId => "tongue";
    protected override int CalculateValue(int sourceScore) => 1 + sourceScore / 2;
    public override int WorstValue => 1;
    public override int? BestValue => 5;
    public override string FormatValue(int value) => $"{value} replies";
}
