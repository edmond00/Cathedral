using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// How well a body handles an implement at all. Four bands rather than a number, because what this
/// governs is not an amount but a <b>threshold</b>: which of the item-use critic's verdicts count as
/// good enough to proceed.
/// </summary>
public enum ToolProficiency
{
    /// <summary>Hands at nought — a disabled or absent organ. No implement can be used for anything.</summary>
    None = 0,
    /// <summary>Only an implement that plainly is the right one can be made to serve.</summary>
    Low = 1,
    /// <summary>A different thing that would do the work properly is also within reach.</summary>
    Medium = 2,
    /// <summary>Even a clumsy improvisation can be made to work.</summary>
    High = 3,
}

/// <summary>
/// Tool Usage Proficiency — the band of implement-handling a body is capable of.
/// Source: hands organ (upper_limbs). Bands: 0 → None, 1–2 → Low, 3–4 → Medium, 5–6 → High.
///
/// <para>This replaced a Tool Usage <i>Cap</i>, which clamped the bonus dice a combined implement
/// lent. The two are opposite ends of the same organ and keeping both taxed hands twice for one act:
/// the cap said a clumsy body draws less from a fine tool, and the band now says it cannot reach for
/// a doubtful one at all. The dice a combination lends are the implement's own level, whole.</para>
///
/// <para>Read through <see cref="Of"/> at exactly two moments, both in
/// <c>NarrativeController.ExecuteItemCombinationAsync</c>: <see cref="ToolProficiency.None"/> fails
/// the combination outright with no critic call — there is no verdict a bodiless hand could pass —
/// and otherwise the band is compared against the verdict the critic returned.</para>
/// </summary>
public class ToolUsageProficiencyStat : DerivedStat
{
    public override string Name         => "tool_usage_proficiency";
    public override string DisplayName  => "Tool Usage Proficiency";
    public override string? RelatedOrganId => "hands";

    public override int? BestValue => (int)ToolProficiency.High;

    protected override int CalculateValue(int sourceScore) => sourceScore switch
    {
        <= 0    => (int)ToolProficiency.None,
        <= 2    => (int)ToolProficiency.Low,
        <= 4    => (int)ToolProficiency.Medium,
        _       => (int)ToolProficiency.High,
    };

    public override string FormatValue(int value) => ((ToolProficiency)value).ToString();

    /// <summary>
    /// The band this body handles implements at. Falls to <see cref="ToolProficiency.None"/> for an
    /// anatomy carrying no such stat at all, which is the right answer for a body with no hands.
    /// </summary>
    public static ToolProficiency Of(PartyMember member)
    {
        var stat = member.DerivedStats.FirstOrDefault(s => s.Name == "tool_usage_proficiency");
        return stat == null ? ToolProficiency.None : (ToolProficiency)stat.GetValue(member);
    }
}
