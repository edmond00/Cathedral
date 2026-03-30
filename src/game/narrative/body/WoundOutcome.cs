namespace Cathedral.Game.Narrative;

/// <summary>
/// An outcome that inflicts a specific wound on the protagonist.
/// Produced by the failure outcome tree when the action failure causes physical injury.
/// </summary>
public class WoundOutcome : OutcomeBase
{
    /// <summary>The wound to apply, or null if no wound was determined.</summary>
    public Wound? Wound { get; }

    public WoundOutcome(Wound? wound)
    {
        Wound = wound;
    }

    public override string DisplayName =>
        Wound != null ? $"Wound: {Wound.WoundName}" : "No wound";

    public override string ToNaturalLanguageString() =>
        Wound != null
            ? $"suffered a {Wound.WoundName} to {(Wound.TargetId.Length > 0 ? Wound.TargetId : Wound.WildcardZoneHint ?? "body").Replace('_', ' ')}"
            : "escaped without injury";
}
