using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Speech Fluency — max number of player dialogue replicas available per conversation turn.
/// Derived from the tongue organ (visage).
/// Formula: 1 + floor(organScore / 2), range 1–5.
/// </summary>
public class SpeechFluencyStat : DerivedStat
{
    public override string Name         => "speech fluency";
    public override string DisplayName  => "Speech Fluency";
    public override string ShortDisplayName => "Speech Fluency";
    public override string? RelatedOrganId => "tongue";
    public override int CalculateValue(int sourceScore) => Math.Clamp(1 + sourceScore / 2, 1, 5);
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} replies";
}
