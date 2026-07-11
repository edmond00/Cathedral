using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
    /// Whether a higher computed value is better for the player. Default: true.
    /// Override to false for stats where a lower value is preferable (e.g. an XP cost or
    /// threshold). This drives which side of the value range is the "worst" and how the
    /// value is clamped — see <see cref="WorstValue"/> and <see cref="BestValue"/>.
    /// </summary>
    public virtual bool HigherIsBetter => true;

    /// <summary>
    /// The maximally-degraded value for this stat — what <see cref="GetValue"/> returns when
    /// the source is absent or disabled, and the bound the result can never be worse than.
    /// For a <see cref="HigherIsBetter"/> stat this is a floor (a low number); for a
    /// lower-is-better stat it is a ceiling (a high number). Default: 0.
    /// </summary>
    public virtual int WorstValue => 0;

    /// <summary>
    /// Optional cap on the best side of the range, or null for no cap. For a
    /// <see cref="HigherIsBetter"/> stat this is the maximum the value may reach; for a
    /// lower-is-better stat it is the minimum. Lets each stat carry its own two-sided bounds
    /// instead of relying on clamps at the call site. Default: null (no cap).
    /// </summary>
    public virtual int? BestValue => null;

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
    /// Convert the source score into the raw derived stat value (score >= 0 path), before
    /// any bounds are applied. Each subclass implements its own formula. Protected so callers
    /// go through <see cref="GetValue"/> / <see cref="GetRawValue"/>, which apply the bounds.
    /// </summary>
    protected abstract int CalculateValue(int sourceScore);

    /// <summary>
    /// Returns false when this stat cannot be computed for the given party member because
    /// (a) the related organ / organ part / body part is absent from their anatomy, or
    /// (b) the related source is fully disabled by a High-handicap wound.
    /// Callers can use <see cref="WorstValue"/> as the fallback score, or invoke
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
        int score = GetEffectiveScore(member);
        // A negative score means the source is absent (int.MinValue) or wound-disabled: the
        // stat degrades all the way to its worst value.
        if (score < 0) return WorstValue;
        return Clamp(CalculateValue(score));
    }

    /// <summary>
    /// Value computed from the raw source score, ignoring wounds, with bounds applied.
    /// Used where wounds are accounted for separately (e.g. <see cref="PartyMember.MaxHp"/>).
    /// </summary>
    public int GetRawValue(PartyMember member) => Clamp(CalculateValue(GetSourceScore(member)));

    /// <summary>
    /// Clamp a raw computed value so it is never worse than <see cref="WorstValue"/> nor,
    /// when set, better than <see cref="BestValue"/>, respecting <see cref="HigherIsBetter"/>.
    /// </summary>
    private int Clamp(int value)
    {
        if (HigherIsBetter)
        {
            value = Math.Max(WorstValue, value);
            if (BestValue.HasValue) value = Math.Min(BestValue.Value, value);
        }
        else
        {
            value = Math.Min(WorstValue, value);
            if (BestValue.HasValue) value = Math.Max(BestValue.Value, value);
        }
        return value;
    }

    /// <summary>
    /// Discovers and instantiates every concrete <see cref="DerivedStat"/> subclass
    /// in this assembly. New stats are picked up automatically — no manual registration
    /// needed in anatomy factories.
    /// </summary>
    public static List<DerivedStat> DiscoverAll()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(DerivedStat).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .Select(t => (DerivedStat)Activator.CreateInstance(t)!)
            .ToList();
    }
}

