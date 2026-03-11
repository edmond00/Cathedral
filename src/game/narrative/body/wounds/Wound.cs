namespace Cathedral.Game.Narrative;

/// <summary>
/// How severely a wound impairs the affected body location.
/// </summary>
public enum WoundHandicap
{
    /// <summary>Applies −1 to the organ/body part score (negative values are possible).</summary>
    Low,
    /// <summary>Completely disables the organ/body part (score treated as 0, stat uses disabled formula).</summary>
    High
}

/// <summary>
/// What kind of location a wound targets.
/// </summary>
public enum WoundTargetKind
{
    OrganPart,
    Organ,
    BodyPart
}

/// <summary>
/// Abstract base class for a wound. Each concrete wound type is a subclass.
/// Wounds affect organ parts, organs, or body parts and modify derived stat calculations
/// via <see cref="DerivedStat.CalculateValueNegative"/> and <see cref="DerivedStat.CalculateValueDisabled"/>.
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
    /// </summary>
    public abstract string TargetId { get; }

    /// <summary>Returns true if the given organ part is directly or transitively affected by this wound.</summary>
    public bool AffectsOrganPart(string organPartId, string organId, string bodyPartId) =>
        TargetKind switch
        {
            WoundTargetKind.OrganPart => TargetId == organPartId,
            WoundTargetKind.Organ     => TargetId == organId,
            WoundTargetKind.BodyPart  => TargetId == bodyPartId,
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
        Handicap == WoundHandicap.High
            ? $"{WoundName} — disables {TargetId.Replace('_', ' ')}"
            : $"{WoundName} — −1 to {TargetId.Replace('_', ' ')}";
}
