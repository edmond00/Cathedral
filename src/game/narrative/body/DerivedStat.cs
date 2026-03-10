namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for derived stats. A derived stat is computed from a specific organ part,
/// an organ aggregate, or an entire body part. Each subclass implements its own formula.
/// </summary>
public abstract class DerivedStat
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; }

    /// <summary>
    /// The organ part id this stat derives from (maps to <c>organ_part</c> in organs.csv,
    /// e.g. "left_hand"). Use this when the stat is specific to one side/instance.
    /// Null if derived from an organ or body part instead.
    /// </summary>
    public virtual string? RelatedOrganPartId => null;

    /// <summary>
    /// The organ id this stat derives from (maps to <c>organ</c> in organs.csv,
    /// e.g. "hands"). Use this when the stat applies to all parts of an organ.
    /// Null if derived from an organ part or body part instead.
    /// </summary>
    public virtual string? RelatedOrganId => null;

    /// <summary>
    /// The body part id this stat derives from (maps to <c>body_part</c> in organs.csv,
    /// e.g. "encephalon"). Use this when the stat applies to an entire body region.
    /// Null if derived from an organ or organ part instead.
    /// </summary>
    public virtual string? RelatedBodyPartId => null;

    /// <summary>
    /// Short display name used in stat panels. Defaults to <see cref="DisplayName"/>.
    /// Override to strip a prefix (e.g. secretion stats strip the organ name).
    /// </summary>
    public virtual string ShortDisplayName => DisplayName;

    /// <summary>
    /// Format the computed value for display. Defaults to a plain integer string.
    /// Override to add a suffix such as "%" for percentage stats.
    /// </summary>
    public virtual string FormatValue(int value) => value.ToString();

    /// <summary>
    /// Get the source score from a party member's data.
    /// Checks organ part → organ → body part in order of specificity.
    /// </summary>
    public int GetSourceScore(PartyMember protagonist)
    {
        if (RelatedOrganPartId != null)
        {
            var part = protagonist.GetOrganPartById(RelatedOrganPartId);
            return part?.Score ?? 0;
        }
        if (RelatedOrganId != null)
        {
            var organ = protagonist.GetOrganById(RelatedOrganId);
            return organ?.Score ?? 0;
        }
        if (RelatedBodyPartId != null)
        {
            var bodyPart = protagonist.GetBodyPartById(RelatedBodyPartId);
            return bodyPart?.Score ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// Convert the source score into the derived stat value.
    /// Each subclass implements its own formula.
    /// </summary>
    public abstract int CalculateValue(int sourceScore);

    /// <summary>
    /// Get the final computed value of this derived stat for the given party member.
    /// </summary>
    public int GetValue(PartyMember member) => CalculateValue(GetSourceScore(member));
}
