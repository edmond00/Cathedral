using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Abstract action that can modify the active party member (inventory),
/// the <see cref="PoV"/> (changing area/focus), or the <see cref="Scene"/> state (unlocking, etc.).
/// Verbs are registered in the global <see cref="VerbRegistry"/> and filtered per scene.
/// </summary>
public abstract class Verb
{
    /// <summary>Unique identifier for this verb type (e.g. "move", "grab").</summary>
    public abstract string VerbId { get; }

    /// <summary>Human-readable display name (e.g. "Move", "Grab").</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Base difficulty of this verb (1–10). Combined with a situational modifier
    /// from the LLM critic to produce the final difficulty level.
    /// </summary>
    public abstract int BaseDifficulty { get; }

    /// <summary>
    /// Optional override for the action menu's difficulty glyph. When non-null, takes
    /// precedence over the difficulty-level derived glyph in <c>NarrativeUI</c>.
    /// </summary>
    public virtual char? DifficultyGlyphOverride => null;

    /// <summary>
    /// Whether executing this verb is a legal action.
    /// Override to false for verbs that constitute crimes (stealing, trespassing, attacking innocents).
    /// </summary>
    public virtual bool IsLegal => true;

    /// <summary>
    /// Whether this verb is valid to use when an enemy is nearby (same area).
    /// When false, the LLM critic asks whether the enemy gets an opportunity attack.
    /// Override to true for combat verbs (attack, slay, reconcile, appease).
    /// </summary>
    public virtual bool CanBeUsedUnderThreat => false;

    /// <summary>Returns whether this verb can be executed given the current scene state.</summary>
    public abstract bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null);

    /// <summary>Natural-language string describing the intended action, sent to the LLM.</summary>
    public abstract string Verbatim(Scene scene, PoV pov, Element target);

    /// <summary>
    /// Returns the <see cref="OutcomeReport"/> objects that result from a successful execution
    /// of this verb. Each report both describes itself for the UI and applies its own
    /// game-state change via <see cref="OutcomeReport.Apply"/>.
    /// </summary>
    public virtual IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
        => System.Array.Empty<OutcomeReport>();

    /// <summary>
    /// Returns the <see cref="OutcomeReport"/> objects that result from a failed execution
    /// of this verb (verb-specific failure side-effects, excluding LLM-decided wounds).
    /// </summary>
    public virtual IReadOnlyList<OutcomeReport> FailureReports(Scene scene, PoV pov, Protagonist actor, Element target)
        => System.Array.Empty<OutcomeReport>();

    /// <summary>
    /// Applies all success reports in sequence. Kept for compatibility — prefer calling
    /// <see cref="SuccessReports"/> and iterating the results directly.
    /// </summary>
    public void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        foreach (var report in SuccessReports(scene, pov, actor, target))
            report.Apply(actor, scene, pov);
    }
}
