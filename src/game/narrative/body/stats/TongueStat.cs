using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Tongue — max number of player dialogue replicas available per conversation turn.
/// Derived from the tongue organ (visage).
/// Formula: 1 + floor(organScore / 2), range 1–5.
/// </summary>
public class TongueStat : DerivedStat
{
    public override string Name         => "tongue";
    public override string DisplayName  => "Tongue";
    public override string ShortDisplayName => "TNG";
    public override string? RelatedOrganId => "tongue";
    public override int CalculateValue(int sourceScore) => Math.Clamp(1 + sourceScore / 2, 1, 5);
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value} replies";
}
