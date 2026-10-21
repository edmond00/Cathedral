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
    /// Where this stat reads its score on a body of <paramref name="anatomy"/>. Defaults to the three
    /// <c>Related…</c> properties, which therefore describe the stat on any anatomy that does not
    /// say otherwise; override when an anatomy carries the same faculty in a different organ —
    /// a beast's natural defence is in its legs, where a person's is the whole lower limbs, which a
    /// beast does not have. Everything that resolves a source against a member goes through
    /// <see cref="SourceOf"/>, so an override is the whole of the change.
    /// </summary>
    protected virtual StatSource SourceFor(AnatomyType anatomy)
        => new(RelatedOrganPartId, RelatedOrganId, RelatedBodyPartId);

    /// <summary>The source this stat reads on <paramref name="member"/>'s body.</summary>
    public StatSource SourceOf(PartyMember member) => SourceFor(member.AnatomyType);

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
        var src = SourceOf(member);
        if (src.OrganPartId != null) return member.GetOrganPartById(src.OrganPartId)?.Score ?? 0;
        if (src.OrganId     != null) return member.GetOrganById(src.OrganId)?.Score ?? 0;
        if (src.BodyPartId  != null) return member.GetBodyPartById(src.BodyPartId)?.Score ?? 0;
        return 0;
    }

    /// <summary>
    /// Get effective score, taking wounds into account.
    /// Returns int.MinValue if the source is disabled by a high-handicap wound.
    /// </summary>
    public int GetEffectiveScore(PartyMember member)
    {
        // Determine the organ part / organ / body part this stat relates to
        var src = SourceOf(member);
        string? organPartId = src.OrganPartId;
        string? organId     = src.OrganId;
        string? bodyPartId  = src.BodyPartId;

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
    /// Member-aware variant of <see cref="CalculateValue(int)"/>, used by <see cref="GetValue"/> and
    /// <see cref="GetRawValue"/>. Defaults to the member-agnostic formula, so existing stats are
    /// unaffected; override when the value depends on more than the source score (e.g. a linear
    /// scale relative to the source's <c>MaxScore</c>).
    /// </summary>
    protected virtual int CalculateValue(PartyMember member, int sourceScore) => CalculateValue(sourceScore);

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
        var src = SourceOf(member);
        if (src.OrganPartId != null)
        {
            if (member.GetOrganPartById(src.OrganPartId) == null) return false;
        }
        else if (src.OrganId != null)
        {
            if (member.GetOrganById(src.OrganId) == null) return false;
        }
        else if (src.BodyPartId != null)
        {
            if (member.GetBodyPartById(src.BodyPartId) == null) return false;
        }

        // Check wound-disabled state
        return GetEffectiveScore(member) != int.MinValue;
    }

    /// <summary>
    /// Get the final computed value of this derived stat for the given party member,
    /// taking wounds into account.
    ///
    /// <para><b>Virtual for one family of stats only.</b> Every stat here degrades <em>towards</em>
    /// <see cref="WorstValue"/> and stops, which is right for a quantity a body either has or lacks
    /// — carry weight, beauty, health. The modus-mentis max-level contributions are the exception:
    /// a ruined organ does not merely stop contributing to what its skills may reach, it takes some
    /// of that reach away, so those two override this to return a negative. Do not widen that
    /// reading to the rest — see <c>MaxLevelContributionStats.cs</c>.</para>
    /// </summary>
    public virtual int GetValue(PartyMember member)
    {
        int score = GetEffectiveScore(member);
        // A negative score means the source is absent (int.MinValue) or wound-disabled: the
        // stat degrades all the way to its worst value.
        if (score < 0) return WorstValue;
        return Clamp(CalculateValue(member, score));
    }

    /// <summary>
    /// Value computed from the raw source score, ignoring wounds, with bounds applied.
    /// Used where wounds are accounted for separately (e.g. <see cref="PartyMember.MaxHp"/>).
    /// </summary>
    public int GetRawValue(PartyMember member) => Clamp(CalculateValue(member, GetSourceScore(member)));

    /// <summary>
    /// The value this stat reaches when its source is scored as high as this member's anatomy
    /// allows — the far end of the stat's range, not its current state. Wounds are ignored, since
    /// the point is the size of the bar rather than how much of it is filled. UI that draws a stat
    /// as filled/empty pips needs this to know how many pips to draw; a species with a smaller
    /// maximum therefore gets a shorter bar rather than one it can never fill.
    /// Returns <see cref="WorstValue"/> when the source is absent from the anatomy.
    /// </summary>
    public int GetValueAtMaxScore(PartyMember member)
    {
        var src = SourceOf(member);
        int? maxScore =
              src.OrganPartId != null ? member.GetOrganPartById(src.OrganPartId)?.MaxScore
            : src.OrganId     != null ? member.GetOrganById(src.OrganId)?.MaxScore
            : src.BodyPartId  != null ? member.GetBodyPartById(src.BodyPartId)?.MaxScore
            : null;

        return maxScore is { } max ? Clamp(CalculateValue(member, max)) : WorstValue;
    }

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

/// <summary>
/// Where a <see cref="DerivedStat"/> reads its score on one anatomy: exactly one of an organ part, an
/// organ or a body part (region) id, as in organs.csv. See <see cref="DerivedStat.SourceOf"/>.
/// </summary>
public readonly record struct StatSource(string? OrganPartId, string? OrganId, string? BodyPartId)
{
    /// <summary>Whether this source is the given organ part, organ or region id.</summary>
    public bool Is(string? organPartId = null, string? organId = null, string? bodyPartId = null)
        => (organPartId != null && OrganPartId == organPartId)
        || (organId     != null && OrganId     == organId)
        || (bodyPartId  != null && BodyPartId  == bodyPartId);
}
