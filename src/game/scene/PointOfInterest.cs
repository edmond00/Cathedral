using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// Default regeneration wait times (in in-game hours) for depleted item slots, by resource nature.
/// Months follow the 30-day convention used by <c>TravelInfoRenderer.FormatDuration</c>.
/// </summary>
public static class ResourceRegen
{
    /// <summary>Natural resources (wild plants, fruit, fungi, rock): 3 months.</summary>
    public const double NaturalHours = 3 * 30 * 24;   // 2160

    /// <summary>Man-made / cultivated stock (caches, crops, crafted goods): 1 year.</summary>
    public const double ManMadeHours = 12 * 30 * 24;  // 8640
}

/// <summary>
/// A large object or small space within an <see cref="Area"/> that the player can focus on.
/// For example: a tree, a boulder, a patch of flowers, a piece of furniture.
/// Contains <see cref="Narrative.Item"/>s that can be collected by interacting with this point of interest.
/// </summary>
public class PointOfInterest : Element
{
    /// <summary>
    /// Whether this PoI yields a natural/wild resource (true) versus man-made/cultivated stock (false).
    /// Drives which pickup verb applies (GATHER vs GRAB) and the regeneration wait. Set freely per
    /// builder, independent of location. Default false (man-made).
    /// </summary>
    public bool IsNatural { get; init; }

    /// <summary>Regeneration wait for this PoI's depleted item slots, derived from <see cref="IsNatural"/>.</summary>
    public double RegenHours => IsNatural ? ResourceRegen.NaturalHours : ResourceRegen.ManMadeHours;

    public override string DisplayName { get; }
    public override List<string> Descriptions { get; }

    /// <summary>
    /// Core noun used as the keyword-similarity anchor when this PoI becomes an observation.
    /// Defined explicitly at every construction site (no inference) — e.g. "tree" for an oak.
    /// </summary>
    public string ReferenceLemma { get; }

    /// <summary>Items that can be collected from this point of interest.</summary>
    public List<ItemElement> Items { get; } = new();

    /// <summary>Mood adjectives for procedural neutral descriptions.</summary>
    public string[] Moods { get; }

    public PointOfInterest(
        string displayName,
        string referenceLemma,
        List<string> descriptions,
        List<ItemElement>? items = null,
        string[]? moods = null,
        bool isNatural = false)
    {
        DisplayName    = displayName;
        ReferenceLemma = referenceLemma;
        Descriptions   = descriptions;
        Moods          = moods ?? System.Array.Empty<string>();
        IsNatural      = isNatural;
        if (items != null) Items.AddRange(items);
    }
}
