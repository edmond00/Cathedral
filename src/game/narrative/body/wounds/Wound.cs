namespace Cathedral.Game.Narrative;

/// <summary>
/// How severely a wound impairs the affected body location.
/// </summary>
public enum WoundHandicap
{
    /// <summary>No organ/stat penalty — only costs 1 HP. Used for generic wounds (contusion, cut, puncture).</summary>
    Low,
    /// <summary>Applies −1 to the organ/body part score (negative values are possible).</summary>
    Medium,
    /// <summary>Completely disables the organ/body part (score treated as 0, stat uses disabled formula).</summary>
    High
}

/// <summary>
/// What kind of location a wound targets.
/// </summary>
public enum WoundTargetKind
{
    /// <summary>Targets a specific organ part, organ, or body part.</summary>
    OrganPart,
    Organ,
    BodyPart,
    /// <summary>Generic / wildcard wound — no fixed target, no organ penalty, -1 HP only.</summary>
    Wildcard
}

/// <summary>
/// A KIND of wound — the catalogue entry, not an injury on anybody.
/// Wounds affect organ parts, organs, or body parts and modify derived stat calculations
/// via <see cref="DerivedStat.GetEffectiveScore"/>; when a source is disabled the stat degrades
/// to its <see cref="DerivedStat.WorstValue"/>.
///
/// <para>
/// <b>Immutable, deliberately.</b> <see cref="WoundRegistry.All"/> hands out one shared instance
/// per type, so a settable property here is a property shared by every character in the process —
/// which is exactly what went wrong when glyph positions lived on this class. Anything true of one
/// person's injury rather than of the injury itself belongs on <see cref="WoundInstance"/>.
/// </para>
/// </summary>
public abstract class Wound
{
    /// <summary>Single-char id from wounds.csv. Used to locate the wound glyph on wounds.txt.</summary>
    public abstract char WoundId { get; }

    /// <summary>Human-readable wound name (e.g. "Black Eye").</summary>
    public abstract string WoundName { get; }

    /// <summary>Severity of the wound.</summary>
    public abstract WoundHandicap Handicap { get; }

    /// <summary>What kind of target this wound affects.</summary>
    public abstract WoundTargetKind TargetKind { get; }

    /// <summary>
    /// ID of the affected target. Matches organ part id, organ id, or body part id
    /// as used in the body hierarchy (e.g. "left_eye", "eyes", "visage").
    /// Empty string for Wildcard wounds.
    /// </summary>
    public abstract string TargetId { get; }

    // Art position (ArtX/ArtY) and the wildcard zone hint used to live here as settable properties.
    // They describe one person's injury, not the kind of injury, and on a shared registry instance
    // that meant every character shared them. They now live on WoundInstance.

    /// <summary>Returns true if the given organ part is directly or transitively affected by this wound.
    /// Note: BodyPart-targeted wounds do NOT cascade down to organs/organ-parts.
    /// Wildcard (Low handicap) wounds never affect organs.</summary>
    public bool AffectsOrganPart(string organPartId, string organId, string bodyPartId) =>
        TargetKind switch
        {
            WoundTargetKind.OrganPart => TargetId == organPartId,
            WoundTargetKind.Organ     => TargetId == organId,
            WoundTargetKind.BodyPart  => false,   // body-part wounds don't cascade to organs
            WoundTargetKind.Wildcard  => false,   // low wounds have no organ effect
            _                         => false
        };

    /// <summary>Returns true if the given organ (and all its parts) is affected.</summary>
    public bool AffectsOrgan(string organId, string bodyPartId) =>
        TargetKind switch
        {
            WoundTargetKind.Organ    => TargetId == organId,
            WoundTargetKind.BodyPart => TargetId == bodyPartId,
            _                        => false
        };

    /// <summary>Returns true if the given body part (and all its organs) is affected.</summary>
    public bool AffectsBodyPart(string bodyPartId) =>
        TargetKind == WoundTargetKind.BodyPart && TargetId == bodyPartId;

    /// <summary>Short description shown in the hover detail panel.</summary>
    public virtual string Description =>
        Handicap switch
        {
            WoundHandicap.High    => $"{WoundName} — disables {TargetId.Replace('_', ' ')}",
            WoundHandicap.Medium  => $"{WoundName} — −1 to {TargetId.Replace('_', ' ')}",
            _                     => $"{WoundName} — −1 HP"
        };
}
