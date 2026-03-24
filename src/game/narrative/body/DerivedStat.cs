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
    /// Convert a negative source score (due to low-handicap wounds) into the stat value.
    /// Default: clamps to 0. Override for stats that can meaningfully go negative.
    /// </summary>
    public virtual int CalculateValueNegative(int sourceScore) => 0;

    /// <summary>
    /// Stat value when the source organ/body part is fully disabled by a high-handicap wound.
    /// Default: 0.
    /// </summary>
    public virtual int CalculateValueDisabled() => 0;

    /// <summary>
    /// Get the raw (unmodified by wounds) source score.
    /// </summary>
    public int GetSourceScore(PartyMember member)
    {
        if (RelatedOrganPartId != null)
        {
            var part = member.GetOrganPartById(RelatedOrganPartId);
            return part?.Score ?? 0;
        }
        if (RelatedOrganId != null)
        {
            var organ = member.GetOrganById(RelatedOrganId);
            return organ?.Score ?? 0;
        }
        if (RelatedBodyPartId != null)
        {
            var bodyPart = member.GetBodyPartById(RelatedBodyPartId);
            return bodyPart?.Score ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// Get effective score, taking wounds into account.
    /// Returns int.MinValue if the source is disabled by a high-handicap wound.
    /// </summary>
    public int GetEffectiveScore(PartyMember member)
    {
        // Determine the organ part / organ / body part this stat relates to
        string? organPartId = RelatedOrganPartId;
        string? organId     = RelatedOrganId;
        string? bodyPartId  = RelatedBodyPartId;

        // Resolve all three IDs from whichever relation key is set
        if (organPartId != null)
        {
            foreach (var bp in member.BodyParts)
                foreach (var organ in bp.Organs)
                    foreach (var part in organ.Parts)
                        if (part.Id == organPartId)
                        {
                            organId    ??= organ.Id;
                            bodyPartId ??= bp.Id;
                        }
        }
        else if (organId != null)
        {
            foreach (var bp in member.BodyParts)
                foreach (var organ in bp.Organs)
                    if (organ.Id == organId)
                        bodyPartId ??= bp.Id;
        }

        // Check for disabling wounds
        bool disabled = member.Wounds.Any(w =>
            (organPartId != null && w.AffectsOrganPart(organPartId, organId ?? "", bodyPartId ?? "")
                                 && w.Handicap == WoundHandicap.High)
         || (organId    != null && organPartId == null
                                 && w.AffectsOrgan(organId, bodyPartId ?? "")
                                 && w.Handicap == WoundHandicap.High)
         || (bodyPartId != null && organId == null && organPartId == null
                                 && w.AffectsBodyPart(bodyPartId)
                                 && w.Handicap == WoundHandicap.High));

        if (disabled) return int.MinValue;

        int rawScore = GetSourceScore(member);

        // Penalty from medium-handicap wounds (Low/wildcard wounds have no organ effect)
        int penalty = member.Wounds.Count(w =>
            (organPartId != null && w.AffectsOrganPart(organPartId, organId ?? "", bodyPartId ?? "")
                                 && w.Handicap == WoundHandicap.Medium)
         || (organId    != null && organPartId == null
                                 && w.AffectsOrgan(organId, bodyPartId ?? "")
                                 && w.Handicap == WoundHandicap.Medium)
         || (bodyPartId != null && organId == null && organPartId == null
                                 && w.AffectsBodyPart(bodyPartId)
                                 && w.Handicap == WoundHandicap.Medium));

        return rawScore - penalty;
    }

    /// <summary>
    /// Convert the source score into the derived stat value (score >= 0 path).
    /// Each subclass implements its own formula.
    /// </summary>
    public abstract int CalculateValue(int sourceScore);

    /// <summary>
    /// The lowest meaningful value for this stat regardless of wounds or anatomy.
    /// Used as a safe fallback when <see cref="IsUsable"/> returns false and the caller
    /// needs a numeric value to stay compatible with running code.
    /// Default: 0. Override in stats where a minimum of 1 is required (e.g. memory slots).
    /// </summary>
    public virtual int MinimumValue() => 0;

    /// <summary>
    /// Returns false when this stat cannot be computed for the given party member because
    /// (a) the related organ / organ part / body part is absent from their anatomy, or
    /// (b) the related source is fully disabled by a High-handicap wound.
    /// Callers can use <see cref="MinimumValue"/> as the fallback score, or invoke
    /// anatomy-specific fallback logic.
    /// </summary>
    public bool IsUsable(PartyMember member)
    {
        // Check presence in anatomy
        if (RelatedOrganPartId != null)
        {
            if (member.GetOrganPartById(RelatedOrganPartId) == null) return false;
        }
        else if (RelatedOrganId != null)
        {
            if (member.GetOrganById(RelatedOrganId) == null) return false;
        }
        else if (RelatedBodyPartId != null)
        {
            if (member.GetBodyPartById(RelatedBodyPartId) == null) return false;
        }

        // Check wound-disabled state
        return GetEffectiveScore(member) != int.MinValue;
    }

    /// <summary>
    /// Get the final computed value of this derived stat for the given party member,
    /// taking wounds into account.
    /// </summary>
    public int GetValue(PartyMember member)
    {
        int effective = GetEffectiveScore(member);
        if (effective == int.MinValue) return CalculateValueDisabled();
        if (effective < 0)             return CalculateValueNegative(effective);
        return CalculateValue(effective);
    }
}

