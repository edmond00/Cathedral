using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Hand Load — the heaviest thing a hand can hold, read off the arm behind it. One stat per arm,
/// since each hand is governed by its own: the right hold by the right arm, the left by the left.
/// Source: the arm part (upper_limbs). Arm 0 → light, 1 → medium, 2 → heavy.
///
/// <para>Read through <see cref="PartyMember.HoldLimit"/>, which every placement on a hand and the
/// fight's choice of weapon ask (see <see cref="PartyMember.CanPlace"/> and
/// <see cref="PartyMember.WieldedWeapons(EquipmentAnchor)"/>). A disabled arm degrades to
/// <see cref="DerivedStat.WorstValue"/> like any stat — light — so a ruined arm can still close round
/// a knife handle.</para>
/// </summary>
public abstract class HandLoadStat : DerivedStat
{
    public override string ShortDisplayName => "Hand Load";
    public override int? BestValue => 2;

    protected override int CalculateValue(int sourceScore) => sourceScore switch
    {
        <= 0 => 0,
        1    => 1,
        _    => 2,
    };

    public override string FormatValue(int value) => ToWeight(value).Label();

    /// <summary>The weight class a value of this stat bears up to.</summary>
    public static WeightClass ToWeight(int value) => value switch
    {
        <= 0 => WeightClass.Light,
        1    => WeightClass.Medium,
        _    => WeightClass.Heavy,
    };

    /// <summary>
    /// The heaviest thing <paramref name="member"/>'s hand at <paramref name="hold"/> can bear, or null
    /// when the anatomy has no arm for it (a beast's holds are its mouth, and no organ governs them).
    /// </summary>
    public static WeightClass? Of(PartyMember member, EquipmentAnchor hold)
    {
        HandLoadStat? stat = hold switch
        {
            EquipmentAnchor.RightHold => member.DerivedStats.OfType<RightHandLoadStat>().FirstOrDefault(),
            EquipmentAnchor.LeftHold  => member.DerivedStats.OfType<LeftHandLoadStat>().FirstOrDefault(),
            _                         => null,
        };
        if (stat?.RelatedOrganPartId is not { } partId || member.GetOrganPartById(partId) == null)
            return null;
        return ToWeight(stat.GetValue(member));
    }
}

public sealed class RightHandLoadStat : HandLoadStat
{
    public override string Name               => "right_hand_load";
    public override string DisplayName        => "Right Hand Load";
    public override string? RelatedOrganPartId => "right_arm";
}

public sealed class LeftHandLoadStat : HandLoadStat
{
    public override string Name               => "left_hand_load";
    public override string DisplayName        => "Left Hand Load";
    public override string? RelatedOrganPartId => "left_arm";
}
