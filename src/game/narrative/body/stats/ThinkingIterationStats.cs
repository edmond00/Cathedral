using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// How many times a thinking modus mentis tries again within one thinking, when what it proposed comes
/// to nothing: the action modus mentis refused it, a coded rule would refuse it, or the player doubted
/// it (<see cref="DoubtStat"/>). Each retry is a fresh choice of goal and means, with every pair already
/// tried crossed off; when nothing is left the thinking stops. Spends no noetic point — the one the
/// thinking cost covers the whole chain. See <c>ThinkingExecutor.GenerateThinkingAsync</c>.
///
/// Source: pineal gland organ score (0–3), the seat of inward regard. Formula: the score itself, so a
/// score of 0 (or a pineal gland disabled by a wound) thinks once and stops, as every mind used to.
/// </summary>
public class ThinkingIterationsStat : DerivedStat
{
    public override string Name        => "thinking_iterations";
    public override string DisplayName => "Iterative Thinking";
    public override string? RelatedOrganId => "pineal_gland";

    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override string FormatValue(int value) => value == 1 ? "1 retry" : $"{value} retries";

    /// <summary>The acting member's value, or 0 when the stat is missing or unusable.</summary>
    public static int Of(PartyMember member)
        => member.DerivedStats.OfType<ThinkingIterationsStat>().FirstOrDefault() is { } s && s.IsUsable(member)
            ? s.GetValue(member)
            : 0;
}

/// <summary>
/// Whether the player may doubt what a thinking modus mentis proposes. When it can, and a retry
/// remains (<see cref="ThinkingIterationsStat"/>), the reasoning ends on a question and the preview box
/// asks YES or NO before the action modus mentis is asked or any rule is checked. NO counts as a
/// refusal: the pair is crossed off and the thinking tries again.
///
/// Source: pineal gland organ score (0–3). Formula: 1 (yes) at a score of 2 or more, else 0.
/// </summary>
public class DoubtStat : DerivedStat
{
    public override string Name        => "can_doubt";
    public override string DisplayName => "Doubt";
    public override string? RelatedOrganId => "pineal_gland";

    protected override int CalculateValue(int sourceScore) => sourceScore >= 2 ? 1 : 0;
    public override int? BestValue => 1;
    public override string FormatValue(int value) => value > 0 ? "yes" : "no";

    /// <summary>The acting member's value as a flag; false when the stat is missing or unusable.</summary>
    public static bool Of(PartyMember member)
        => member.DerivedStats.OfType<DoubtStat>().FirstOrDefault() is { } s && s.IsUsable(member)
           && s.GetValue(member) > 0;
}
