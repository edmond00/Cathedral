namespace Cathedral.Game.Narrative;

/// <summary>
/// How much an item burdens the person carrying it, as a label rather than a mass.
///
/// The unit is deliberately abstract — the question is never "how many kilograms" but "how much of
/// what I can carry does this use up", and a label forces that judgement at authoring time instead
/// of inviting false precision. The numeric values are the carrying cost, summed against the
/// <c>maximum_weight</c> derived stat.
///
/// The gaps between tiers are wide on purpose: a heavy item costs as much as fifteen trinkets, so
/// choosing to carry armour or a barrel is a real decision rather than a rounding error.
/// </summary>
public enum WeightClass
{
    /// <summary>Costs nothing to carry: a leaf, a hairpin, a feather, a coin.</summary>
    Insignificant = 0,

    /// <summary>Noticeable in quantity but not on its own: a knife, a hat, an apple.</summary>
    Light = 1,

    /// <summary>Carried deliberately: a sword, a coat, a cooking pot.</summary>
    Medium = 5,

    /// <summary>A burden in itself: a shield, an armour piece, a barrel, a log.</summary>
    Heavy = 15,
}

public static class WeightClassExtensions
{
    /// <summary>Carrying cost, in the same abstract unit as the <c>maximum_weight</c> stat.</summary>
    public static int Points(this WeightClass weight) => (int)weight;

    /// <summary>Human-readable label for the inventory panel.</summary>
    public static string Label(this WeightClass weight) => weight switch
    {
        WeightClass.Insignificant => "insignificant",
        WeightClass.Light         => "light",
        WeightClass.Medium        => "medium",
        WeightClass.Heavy         => "heavy",
        _                         => weight.ToString().ToLowerInvariant(),
    };
}
