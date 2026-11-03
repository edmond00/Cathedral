using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Handiwork — the dice a body's own hands add to every thing it makes at a crafting station
/// (<see cref="Crafting.CraftRoll"/>), beside the trades it holds and the implements it carries.
/// Setting to work (USE) is not affected: that roll is the act's, settled by one trade and one tool.
///
/// <para>Source: hands organ (upper_limbs, 0–6). Formula: <c>(score + 1) / 2</c>, so 0 → 0, 1–2 → 1,
/// 3–4 → 2, 5–6 → 3 — the same bands <see cref="ToolUsageProficiencyStat"/> reads off the same organ.
/// A disabled organ (a High-handicap wound) or an anatomy with no hands gives 0.</para>
/// </summary>
public class HandiworkStat : DerivedStat
{
    public override string Name         => "handiwork";
    public override string DisplayName  => "Handiwork";
    public override string? RelatedOrganId => "hands";

    public override int? BestValue => 3;

    protected override int CalculateValue(int sourceScore) => sourceScore <= 0 ? 0 : (sourceScore + 1) / 2;

    public override string FormatValue(int value) => value == 1 ? "+1 craft die" : $"+{value} craft dice";

    /// <summary>The member's value, or 0 when the stat is missing or unusable.</summary>
    public static int Of(PartyMember member)
        => member.DerivedStats.OfType<HandiworkStat>().FirstOrDefault() is { } s && s.IsUsable(member)
            ? s.GetValue(member)
            : 0;
}
