using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The single source of truth for the humoral secretion percentages of the four sanguific
/// organs (pulmones, paunch, spleen, hepar). Keyed on the organ score, 0–3 for humans
/// (beast species may reach 4); the four percentages always sum to 100.
///
///   score : 0    1    2    3    (4)
///   Blood : 0%   12%  25%  37%  (49%)
///   Yellow: 40%  35%  30%  25%  (20%)
///   Black : 50%  42%  33%  25%  (17%)
///   Phlegm: 10%  11%  12%  13%  (14%)
///
/// High score → mostly Blood; low score → Black Bile dominant.
/// Used by the per-organ secretion stats below and by the actual secretion rolls in
/// <c>HumorQueue.CreateSecretedHumor</c>.
/// </summary>
public static class HumoralSecretionTable
{
    /// <summary>Highest organ score the table is defined for (beast species cap).</summary>
    public const int MaxScore = 4;

    public static int BloodPct(int score) =>
        (int)Math.Round(37.0 * Clamp(score) / 3, MidpointRounding.AwayFromZero);

    public static int YellowBilePct(int score) =>
        Math.Max(0, 40 - Clamp(score) * 5);

    public static int BlackBilePct(int score) =>
        Math.Max(0, 50 - (int)Math.Round(25.0 * Clamp(score) / 3, MidpointRounding.AwayFromZero));

    public static int PhlegmPct(int score) =>
        100 - BloodPct(score) - YellowBilePct(score) - BlackBilePct(score);

    private static int Clamp(int score) => Math.Clamp(score, 0, MaxScore);
}

/// <summary>
/// Base class for humoral organ secretion-percentage stats.
/// Formats values as percentages and strips the organ name prefix from the display name
/// (e.g. "Hepar Blood %" → "Blood %").
/// </summary>
public abstract class HumoralSecretionStat : DerivedStat
{
    /// <inheritdoc/>
    public override string FormatValue(int value) => $"{value}%";

    /// <inheritdoc/>
    public override string ShortDisplayName
    {
        get
        {
            string name = DisplayName;
            int spaceIdx = name.IndexOf(' ');
            return spaceIdx >= 0 ? name[(spaceIdx + 1)..] : name;
        }
    }
}
