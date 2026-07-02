using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

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
    /// Expands this verb into the concrete action views it offers for <paramref name="target"/>.
    /// The default is one view when <see cref="IsPossible"/> holds — the same behaviour as the old
    /// per-verb enumeration. Verbs that turn a single target into several actions (like GRAB across
    /// items, or <c>RequestJobVerb</c> across offered jobs) override this to yield several views,
    /// carrying a per-view payload in <see cref="VerbView.Variant"/>.
    /// </summary>
    public virtual IEnumerable<VerbView> ExpandViews(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (IsPossible(scene, pov, target, actor))
            yield return new VerbView(this, Verbatim(scene, pov, target), target);
    }

    /// <summary>
    /// The base difficulty for this verb against a specific target. Defaults to
    /// <see cref="BaseDifficulty"/>; override to make difficulty depend on the target
    /// (e.g. requesting work from a master is harder than from a reeve).
    /// </summary>
    public virtual int DifficultyFor(Element? target) => BaseDifficulty;

    /// <summary>
    /// Builds the verbatim for an item-pickup verb (grab/gather/steal/cut), e.g. "gather some moss",
    /// "grab an apple", "steal a wool cloak". The item name is routed through
    /// <see cref="Item.WithArticle"/> so mass nouns ("moss", "bread") and plurals get "some" rather
    /// than an ungrammatical "a moss". Falls back to a plain a/an article for non-item targets.
    /// </summary>
    protected static string PickupVerbatim(string verb, Element target)
    {
        if (target is ItemElement itemEl)
            return $"{verb} {itemEl.Item.WithArticle()}";

        var name    = target.DisplayName.ToLowerInvariant();
        var article = name.Length > 0 && "aeiou".Contains(name[0]) ? "an" : "a";
        return $"{verb} {article} {name}";
    }

    /// <summary>
    /// A definite noun phrase for a specific scene feature (a point of interest, spot or area), e.g.
    /// "the hedge gap", "the wooden door", "the stairs". Such features are named with bare nouns by
    /// their builders, so this lower-cases the name and prepends "the" (unless it already opens with a
    /// determiner). Use it whenever a verb embeds a fixed scene target in its verbatim, so the neutral
    /// sentence reads grammatically ("follow the hedge gap", not "follow hedge gap").
    /// </summary>
    protected static string DefiniteTarget(Element target)
    {
        string lower = (target.DisplayName ?? string.Empty).Trim().ToLowerInvariant();
        if (lower.Length == 0) return "it";
        int sp = lower.IndexOf(' ');
        string first = sp < 0 ? lower : lower.Substring(0, sp);
        return System.Array.IndexOf(Determiners, first) >= 0 ? lower : $"the {lower}";
    }

    private static readonly string[] Determiners =
    {
        "the", "a", "an", "some", "this", "that", "these", "those",
        "his", "her", "its", "their", "your", "my", "our",
    };

    /// <summary>
    /// Returns the <see cref="OutcomeReport"/> objects that result from a successful execution
    /// of this verb. Each report both describes itself for the UI and applies its own
    /// game-state change via <see cref="OutcomeReport.Apply"/>.
    /// </summary>
    public virtual IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => System.Array.Empty<OutcomeReport>();

    /// <summary>
    /// View-aware success reports. Called by the execution pipeline with the exact
    /// <see cref="VerbView"/> the player chose, so verbs that expanded into several actions can
    /// read <see cref="VerbView.Variant"/> (e.g. which job was requested). Defaults to the
    /// target-only overload.
    /// </summary>
    public virtual IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target, VerbView view)
        => SuccessReports(scene, pov, actor, target);

    /// <summary>
    /// Returns the <see cref="OutcomeReport"/> objects that result from a failed execution
    /// of this verb (verb-specific failure side-effects, excluding LLM-decided wounds).
    /// </summary>
    public virtual IReadOnlyList<OutcomeReport> FailureReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => System.Array.Empty<OutcomeReport>();

    /// <summary>
    /// Applies all success reports in sequence. Kept for compatibility — prefer calling
    /// <see cref="SuccessReports"/> and iterating the results directly.
    /// </summary>
    public void Execute(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        foreach (var report in SuccessReports(scene, pov, actor, target))
            report.Apply(actor, scene, pov);
    }

    // ── Routine recording hooks ───────────────────────────────────────────────
    // A successful verb may be recorded as a step in a learned routine that is later replayed
    // without narration or skill checks. By default no verb is recordable; recordable verbs
    // override these. The contract is dynamic: a verb may inspect scene/pov/target and decline
    // to be recorded in special situations.

    /// <summary>
    /// Whether a successful execution of this verb on <paramref name="target"/> can be recorded as
    /// a routine step. Default: false (no verb is recordable until it opts in).
    /// </summary>
    public virtual bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => false;

    /// <summary>
    /// Builds the stable, rebuild-independent reference to this verb's target for routine recording.
    /// Only meaningful when <see cref="CanRecordAsRoutine"/> returns true.
    /// </summary>
    public virtual RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target) => null;

    /// <summary>
    /// The phase this verb transitions into on success, used to decide where a recorded routine
    /// stops and what happens after replay. Default: <see cref="RoutinePhaseKind.None"/>.
    /// </summary>
    public virtual RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.None;

    /// <summary>
    /// The item this verb would add to the actor's inventory on success, or null for verbs that do not
    /// pick anything up. Pickup verbs (grab/gather/steal/cut) override this so the coded
    /// inventory-capacity rule can block the action when there is no room to carry it.
    /// </summary>
    public virtual Item? AcquiredItem(Element? target) => null;
}
