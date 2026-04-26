namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// Static description of a single childhood-reminescence fragment.
/// A fragment is the reminescence equivalent of a point of interest:
/// its <see cref="ObservationText"/> is a vague, impressionistic description the player sees
/// during the observation phase, without revealing what the fragment is.
/// The <see cref="OutcomeText"/> is the concrete memory recovered only after REMEMBER fires —
/// it is shown in the outcome narration block and saved to <see cref="ChildhoodHistory"/>.
/// </summary>
public sealed class FragmentData
{
    /// <summary>The vague name of the fragment ("a laugh", "a small hairpin", ...).</summary>
    public string Name { get; }

    /// <summary>
    /// Vague, impressionistic one-liner used as the POI description during the observation phase.
    /// Should not reveal who, what or where — only a raw sensory impression.
    /// Example: "a warm, distant laugh dissolving into darkness".
    /// </summary>
    public string ObservationText { get; }

    /// <summary>
    /// Concrete one-liner revealed only after REMEMBER executes.
    /// Used in the outcome narration block and saved to <see cref="ChildhoodHistory"/>.
    /// Example: "you remember the laugh of your father at the stable where you spent your childhood".
    /// </summary>
    public string OutcomeText { get; }

    /// <summary>What REMEMBERing this fragment grants and which reminescence to enter next.</summary>
    public FragmentOutcome Outcome { get; }

    /// <summary>Alias for <see cref="OutcomeText"/> — used when saving to childhood history.</summary>
    public string Summary => OutcomeText;

    public FragmentData(string name, string observationText, string outcomeText, FragmentOutcome outcome)
    {
        Name            = name;
        ObservationText = observationText;
        OutcomeText     = outcomeText;
        Outcome         = outcome;
    }
}
