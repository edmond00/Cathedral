using System;

namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
// Modus-Mentis max-level contribution derived stats
// ─────────────────────────────────────────────────────────────────────────────
// A Modus Mentis's maximum level is 1 (floor) plus the sum of the max-level
// contributions of every organ / body region it is related to (see
// PartyMember.GetMaxLevelForModusMentis). Each organ and each body region owns one
// of these stats, so any of them can be listed in a ModusMentis's Organs array.
//
//   • An organ  contributes +0..+3  (OrganMaxLevelContributionStat,  BestValue 3)
//   • A region  contributes +0..+6  (RegionMaxLevelContributionStat, BestValue 6)
//
// For a paired organ (eyes, hands, …) the stat is owned by the organ *type*
// (RelatedOrganId, e.g. "eyes"), never an individual part (e.g. "left_eye"), so both
// sides count together via the organ's aggregate score.
//
// The contribution scales linearly with how invested the source is: +0 at score 0, and the full
// bound (+3 organ / +6 region) only when the organ/region is at its maximum possible score.
// Intermediate scores are rounded to the nearest step. Override CalculateValue on any concrete stat
// below to give that organ/region a custom curve.
//
// Wounds reach the contribution in three tiers, and the bottom two are NEGATIVE — see
// MaxLevelPenalty below, which is the only place in the game where a derived stat goes under its
// WorstValue:
//
//   • a High-handicap wound        → -2   (the source is out of use)
//   • Medium wounds below score 0  → -1
//   • score 0, wounded or never invested → +0, then the ordinary curve above

/// <summary>Marker for the stats that feed a Modus Mentis's max-level formula.</summary>
public interface IMaxLevelContributionStat { }

/// <summary>Linear map of a source score onto <c>0..cap</c> across its <c>0..maxScore</c> range.</summary>
internal static class MaxLevelCurve
{
    public static int Linear(int score, int maxScore, int cap)
        => maxScore <= 0 ? 0
         : (int)Math.Round((double)cap * score / maxScore, MidpointRounding.AwayFromZero);
}

/// <summary>
/// What a wound takes off a source's max-level contribution — the one place the two negative tiers
/// are written down.
///
/// <para><b>A ruined source subtracts rather than merely stopping.</b> Every other derived stat
/// bottoms out at <see cref="DerivedStat.WorstValue"/>, because a body that cannot see simply
/// contributes nothing to what its eyes were for. A max-level contribution is different: it is the
/// ceiling on skills the organ is <em>part of</em>, and a skill built on a ruined arm should not
/// merely stop growing, it should fall below the floor of 1 that every modus mentis starts from —
/// which is what makes it unusable. See <c>PartyMember.GetEffectiveModusMentisLevel</c>.</para>
///
/// <para><b>Absent is not wounded, and the distinction is load-bearing.</b>
/// <see cref="DerivedStat.DiscoverAll"/> gives every anatomy every stat, so a human really does
/// carry <c>FangsMaxLevelStat</c>. It reads +0 rather than a penalty because
/// <see cref="DerivedStat.GetSourceScore"/> answers 0 (not <c>int.MinValue</c>) for an organ the
/// body does not have, and <see cref="MaxLevelCurve.Linear"/> then divides into a <c>MaxScore</c>
/// of 0. Never route absence through the negative branch: a beast organ named by a human's modus
/// mentis would start costing −2, and <c>ModusMentisAnatomy.IsLearnableBy</c> already refuses that
/// pairing for a better reason.</para>
/// </summary>
internal static class MaxLevelPenalty
{
    /// <summary>A High-handicap wound: the source is out of use altogether.</summary>
    public const int Disabled = -2;

    /// <summary>Medium-handicap wounds have driven the source's effective score below zero.</summary>
    public const int Impaired = -1;

    /// <summary>
    /// The penalty owed for <paramref name="effectiveScore"/> (from
    /// <see cref="DerivedStat.GetEffectiveScore"/>), or null when the source is not wound-degraded
    /// and the ordinary curve applies. A score of exactly 0 is <em>not</em> degraded: an organ
    /// nobody ever invested in contributes nothing, which is not the same as one that was taken.
    /// </summary>
    public static int? For(int effectiveScore)
        => effectiveScore == int.MinValue ? Disabled
         : effectiveScore < 0             ? Impaired
         : null;
}

/// <summary>Base for an organ's Modus-Mentis max-level contribution (+0..+3, linear in organ score).</summary>
public abstract class OrganMaxLevelContributionStat : DerivedStat, IMaxLevelContributionStat
{
    public override bool HigherIsBetter => true;
    public override int WorstValue => 0;
    public override int? BestValue => 3;
    public override string ShortDisplayName => "Max Level +";
    public override string FormatValue(int value) => $"+{value}";

    // Member-agnostic fallback (bounds still clamp); the member-aware path below is what runs.
    protected override int CalculateValue(int sourceScore) => sourceScore;

    protected override int CalculateValue(PartyMember member, int sourceScore) =>
        MaxLevelCurve.Linear(sourceScore, member.GetOrganById(RelatedOrganId!)?.MaxScore ?? 0, BestValue!.Value);

    /// <inheritdoc cref="MaxLevelPenalty"/>
    public override int GetValue(PartyMember member)
        => MaxLevelPenalty.For(GetEffectiveScore(member))
           ?? base.GetValue(member);
}

/// <summary>Base for a body region's Modus-Mentis max-level contribution (+0..+6, linear in region score).</summary>
public abstract class RegionMaxLevelContributionStat : DerivedStat, IMaxLevelContributionStat
{
    public override bool HigherIsBetter => true;
    public override int WorstValue => 0;
    public override int? BestValue => 6;
    public override string ShortDisplayName => "Max Level +";
    public override string FormatValue(int value) => $"+{value}";

    // Member-agnostic fallback (bounds still clamp); the member-aware path below is what runs.
    protected override int CalculateValue(int sourceScore) => sourceScore;

    protected override int CalculateValue(PartyMember member, int sourceScore) =>
        MaxLevelCurve.Linear(sourceScore, member.GetBodyPartById(RelatedBodyPartId!)?.MaxScore ?? 0, BestValue!.Value);

    /// <inheritdoc cref="MaxLevelPenalty"/>
    public override int GetValue(PartyMember member)
        => MaxLevelPenalty.For(GetEffectiveScore(member))
           ?? base.GetValue(member);
}

// ── Organ contributions (+0..+3) ─────────────────────────────────────────────
// Encephalon
public sealed class AnamnesisMaxLevelStat    : OrganMaxLevelContributionStat { public override string Name => "anamnesis_max_level";    public override string DisplayName => "Anamnesis Max-Level Bonus";    public override string? RelatedOrganId => "anamnesis"; }
public sealed class CerebellumMaxLevelStat   : OrganMaxLevelContributionStat { public override string Name => "cerebellum_max_level";   public override string DisplayName => "Cerebellum Max-Level Bonus";   public override string? RelatedOrganId => "cerebellum"; }
public sealed class CerebrumMaxLevelStat     : OrganMaxLevelContributionStat { public override string Name => "cerebrum_max_level";     public override string DisplayName => "Cerebrum Max-Level Bonus";     public override string? RelatedOrganId => "cerebrum"; }
public sealed class HippocampusMaxLevelStat  : OrganMaxLevelContributionStat { public override string Name => "hippocampus_max_level";  public override string DisplayName => "Hippocampus Max-Level Bonus";  public override string? RelatedOrganId => "hippocampus"; }
public sealed class PinealGlandMaxLevelStat  : OrganMaxLevelContributionStat { public override string Name => "pineal_gland_max_level"; public override string DisplayName => "Pineal Gland Max-Level Bonus"; public override string? RelatedOrganId => "pineal_gland"; }

// Visage / muzzle
public sealed class EyesMaxLevelStat         : OrganMaxLevelContributionStat { public override string Name => "eyes_max_level";         public override string DisplayName => "Eyes Max-Level Bonus";         public override string? RelatedOrganId => "eyes"; }
public sealed class EarsMaxLevelStat         : OrganMaxLevelContributionStat { public override string Name => "ears_max_level";         public override string DisplayName => "Ears Max-Level Bonus";         public override string? RelatedOrganId => "ears"; }
public sealed class NoseMaxLevelStat         : OrganMaxLevelContributionStat { public override string Name => "nose_max_level";         public override string DisplayName => "Nose Max-Level Bonus";         public override string? RelatedOrganId => "nose"; }
public sealed class TeethsMaxLevelStat       : OrganMaxLevelContributionStat { public override string Name => "teeths_max_level";       public override string DisplayName => "Teeth Max-Level Bonus";        public override string? RelatedOrganId => "teeths"; }
public sealed class TongueMaxLevelStat       : OrganMaxLevelContributionStat { public override string Name => "tongue_max_level";       public override string DisplayName => "Tongue Max-Level Bonus";       public override string? RelatedOrganId => "tongue"; }
public sealed class SnoutMaxLevelStat        : OrganMaxLevelContributionStat { public override string Name => "snout_max_level";        public override string DisplayName => "Snout Max-Level Bonus";        public override string? RelatedOrganId => "snout"; }
public sealed class FangsMaxLevelStat        : OrganMaxLevelContributionStat { public override string Name => "fangs_max_level";        public override string DisplayName => "Fangs Max-Level Bonus";        public override string? RelatedOrganId => "fangs"; }

// Trunk
public sealed class BackboneMaxLevelStat     : OrganMaxLevelContributionStat { public override string Name => "backbone_max_level";     public override string DisplayName => "Backbone Max-Level Bonus";     public override string? RelatedOrganId => "backbone"; }
public sealed class HeartMaxLevelStat        : OrganMaxLevelContributionStat { public override string Name => "heart_max_level";        public override string DisplayName => "Heart Max-Level Bonus";        public override string? RelatedOrganId => "heart"; }
public sealed class VisceraMaxLevelStat      : OrganMaxLevelContributionStat { public override string Name => "viscera_max_level";      public override string DisplayName => "Viscera Max-Level Bonus";      public override string? RelatedOrganId => "viscera"; }
public sealed class HeparMaxLevelStat        : OrganMaxLevelContributionStat { public override string Name => "hepar_max_level";        public override string DisplayName => "Hepar Max-Level Bonus";        public override string? RelatedOrganId => "hepar"; }
public sealed class SpleenMaxLevelStat       : OrganMaxLevelContributionStat { public override string Name => "spleen_max_level";       public override string DisplayName => "Spleen Max-Level Bonus";       public override string? RelatedOrganId => "spleen"; }
public sealed class PaunchMaxLevelStat       : OrganMaxLevelContributionStat { public override string Name => "paunch_max_level";       public override string DisplayName => "Paunch Max-Level Bonus";       public override string? RelatedOrganId => "paunch"; }
public sealed class PulmonesMaxLevelStat     : OrganMaxLevelContributionStat { public override string Name => "pulmones_max_level";     public override string DisplayName => "Pulmones Max-Level Bonus";     public override string? RelatedOrganId => "pulmones"; }
public sealed class GenitoriesMaxLevelStat   : OrganMaxLevelContributionStat { public override string Name => "genitories_max_level";   public override string DisplayName => "Genitories Max-Level Bonus";   public override string? RelatedOrganId => "genitories"; }

// Limbs
public sealed class ArmsMaxLevelStat         : OrganMaxLevelContributionStat { public override string Name => "arms_max_level";         public override string DisplayName => "Arms Max-Level Bonus";         public override string? RelatedOrganId => "arms"; }
public sealed class HandsMaxLevelStat        : OrganMaxLevelContributionStat { public override string Name => "hands_max_level";        public override string DisplayName => "Hands Max-Level Bonus";        public override string? RelatedOrganId => "hands"; }
public sealed class LegsMaxLevelStat         : OrganMaxLevelContributionStat { public override string Name => "legs_max_level";         public override string DisplayName => "Legs Max-Level Bonus";         public override string? RelatedOrganId => "legs"; }
public sealed class FeetMaxLevelStat         : OrganMaxLevelContributionStat { public override string Name => "feet_max_level";         public override string DisplayName => "Feet Max-Level Bonus";         public override string? RelatedOrganId => "feet"; }
public sealed class ClawsMaxLevelStat        : OrganMaxLevelContributionStat { public override string Name => "claws_max_level";        public override string DisplayName => "Claws Max-Level Bonus";        public override string? RelatedOrganId => "claws"; }

// ── Body region contributions (+0..+6) ───────────────────────────────────────
public sealed class EncephalonMaxLevelStat   : RegionMaxLevelContributionStat { public override string Name => "encephalon_region_max_level"; public override string DisplayName => "Encephalon Max-Level Bonus"; public override string? RelatedBodyPartId => "encephalon"; }
public sealed class VisageMaxLevelStat       : RegionMaxLevelContributionStat { public override string Name => "visage_region_max_level";     public override string DisplayName => "Visage Max-Level Bonus";     public override string? RelatedBodyPartId => "visage"; }
public sealed class TrunkMaxLevelStat        : RegionMaxLevelContributionStat { public override string Name => "trunk_region_max_level";      public override string DisplayName => "Trunk Max-Level Bonus";      public override string? RelatedBodyPartId => "trunk"; }
public sealed class UpperLimbsMaxLevelStat   : RegionMaxLevelContributionStat { public override string Name => "upper_limbs_region_max_level"; public override string DisplayName => "Upper Limbs Max-Level Bonus"; public override string? RelatedBodyPartId => "upper_limbs"; }
public sealed class LowerLimbsMaxLevelStat   : RegionMaxLevelContributionStat { public override string Name => "lower_limbs_region_max_level"; public override string DisplayName => "Lower Limbs Max-Level Bonus"; public override string? RelatedBodyPartId => "lower_limbs"; }
public sealed class MuzzleMaxLevelStat       : RegionMaxLevelContributionStat { public override string Name => "muzzle_region_max_level";     public override string DisplayName => "Muzzle Max-Level Bonus";     public override string? RelatedBodyPartId => "muzzle"; }
public sealed class LimbsMaxLevelStat        : RegionMaxLevelContributionStat { public override string Name => "limbs_region_max_level";      public override string DisplayName => "Limbs Max-Level Bonus";      public override string? RelatedBodyPartId => "limbs"; }
