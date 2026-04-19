namespace Cathedral.Game.Narrative;

/// <summary>
/// Identifies each question slot in the Narrative CoT pipeline.
/// Used to look up per-ModusMentis question phrasing via QuestionFillerService.
/// Does NOT cover dialogue system questions or enum-selection fields (goal, how).
/// </summary>
public enum QuestionReference
{
    /// <summary>First observation sentence (full scene context, "I " prefix template).</summary>
    ObserveFirst,

    /// <summary>Per-outcome continuation sentence (no forced "I " prefix).</summary>
    ObserveContinuation,

    /// <summary>Transition sentence shifting attention to a new outcome.</summary>
    ObserveTransition,

    /// <summary>Thinking WHY call — why do you want this?</summary>
    ThinkWhy,

    /// <summary>Thinking HOW call — the reasoning sub-field ("and why?") paired with the choice field.</summary>
    ThinkHowReason,

    /// <summary>
    /// Thinking WHAT call — what will you try to do?
    /// PromptText uses {0} as a placeholder for actionModusMentis.ShortDescription.
    /// </summary>
    ThinkWhat,

    /// <summary>Outcome narration (first call) — what happened, when the action succeeded.</summary>
    OutcomeSucceededHappened,

    /// <summary>Outcome feeling follow-up (second call) — what do you feel, when the action succeeded.</summary>
    OutcomeSucceededFeel,

    /// <summary>Outcome narration (first call) — what happened, when the action failed or was impossible.</summary>
    OutcomeFailedHappened,

    /// <summary>Outcome feeling follow-up (second call) — what do you feel, when the action failed or was impossible.</summary>
    OutcomeFailedFeel,
}
