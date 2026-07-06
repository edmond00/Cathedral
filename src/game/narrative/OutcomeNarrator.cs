using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Generates narration for action outcomes from the action Modus Mentis's perspective by building
/// neutral meaning text (<see cref="NeutralNarration"/>) and re-expressing it in persona voice via
/// <see cref="PersonaRewriter"/>. In playground mode the neutral text is returned unchanged.
/// </summary>
public class OutcomeNarrator
{
    private readonly LlamaServerManager _llmManager;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly PersonaRewriter _rewriter;

    public OutcomeNarrator(LlamaServerManager llmManager, ModusMentisSlotManager slotManager)
    {
        _llmManager  = llmManager;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _rewriter    = new PersonaRewriter(llmManager);
    }

    /// <summary>
    /// Narrates an action outcome (success or failure) in the action Modus Mentis's voice.
    /// </summary>
    public async Task<string> NarrateOutcomeAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        OutcomeBase outcome,
        bool succeeded,
        double difficulty,
        PartyMember protagonist,
        CancellationToken cancellationToken = default,
        string? failureHint = null,
        string? neutralOverride = null)
    {
        // The reminescence path supplies its own neutral meaning (a plain "I try to remember …"
        // framing that embeds the concrete recovered memory); everything else templates it here.
        string neutral = neutralOverride ?? BuildNeutralOutcome(action, succeeded, failureHint);
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        // keepHistory so the dual-outcome snapshot/restore (humor flips) sees the generated turns.
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, styleInstruction: actionModusMentis.StyleInstruction, ct: cancellationToken);
    }

    // ── Dual outcome pre-generation (for humor dice modifiers) ─────────────────
    // Both success and failure narration are generated up-front during the dice animation so the
    // player can flip the result via humor modifiers with no further loading. Each branch is
    // generated on a clean copy of the narrator slot history (snapshot/restore) so they don't
    // pollute each other; CommitNarrationHistory keeps only the chosen branch's turns.
    private int _pendingNarratorSlot = -1;
    private List<object>? _pendingSuccessHistory;
    private List<object>? _pendingFailureHistory;

    /// <summary>
    /// Generate BOTH the success and failure narration for an action in one pass. Neither result
    /// is committed to the slot's permanent history yet — call <see cref="CommitNarrationHistory"/>
    /// once the final (possibly humor-modified) outcome is known.
    /// </summary>
    public async Task<(string success, string failure)> NarrateBothOutcomesAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        OutcomeBase successOutcome,
        OutcomeBase failureOutcome,
        double difficulty,
        PartyMember protagonist,
        string? failureHint,
        CancellationToken cancellationToken = default)
    {
        if (PlaygroundMode.IsActive)
        {
            _pendingNarratorSlot = -1;
            return (BuildNeutralOutcome(action, true, null), BuildNeutralOutcome(action, false, failureHint));
        }

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        var instance = _llmManager.GetInstance(slotId);
        var baseline = instance?.SnapshotHistory();

        string success = await NarrateOutcomeAsync(
            action, actionModusMentis, successOutcome, true, difficulty, protagonist, cancellationToken);
        var afterSuccess = instance?.SnapshotHistory();

        // Reset to the pre-narration baseline so the failure branch generates without seeing the
        // success turns, then snapshot the failure branch state.
        if (instance != null && baseline != null) instance.RestoreHistory(baseline);

        string failure = await NarrateOutcomeAsync(
            action, actionModusMentis, failureOutcome, false, difficulty, protagonist, cancellationToken, failureHint);
        var afterFailure = instance?.SnapshotHistory();

        _pendingNarratorSlot   = (instance != null) ? slotId : -1;
        _pendingSuccessHistory = afterSuccess;
        _pendingFailureHistory = afterFailure;

        return (success, failure);
    }

    /// <summary>
    /// After the final outcome is chosen, keep only that branch's narration turns in the slot's
    /// conversation history (discarding the speculative other branch). No-op if no dual generation
    /// is pending.
    /// </summary>
    public void CommitNarrationHistory(bool success)
    {
        if (_pendingNarratorSlot < 0) return;
        var instance = _llmManager.GetInstance(_pendingNarratorSlot);
        var chosen = success ? _pendingSuccessHistory : _pendingFailureHistory;
        if (instance != null && chosen != null) instance.RestoreHistory(chosen);
        _pendingNarratorSlot = -1;
        _pendingSuccessHistory = null;
        _pendingFailureHistory = null;
    }

    /// <summary>
    /// Narrates why an action failed plausibility checks, in the action Modus Mentis's voice.
    /// </summary>
    public async Task<string> NarratePlausibilityFailureAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        string plausibilityError,
        PartyMember protagonist,
        CancellationToken cancellationToken = default)
    {
        string neutral = NeutralNarration.PlausibilityFailure(ActionDisplay(action));
        if (!string.IsNullOrWhiteSpace(plausibilityError))
            neutral = $"{neutral} {plausibilityError}";

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, styleInstruction: actionModusMentis.StyleInstruction, ct: cancellationToken);
    }

    /// <summary>
    /// Narrates why a combined item cannot be used for the action, in the action Modus Mentis's voice.
    /// </summary>
    public async Task<string> NarrateItemCombinationFailureAsync(
        ParsedNarrativeAction action,
        Item item,
        ModusMentis actionModusMentis,
        string criticReason = "",
        CancellationToken cancellationToken = default)
    {
        string neutral = NeutralNarration.ItemCombinationFailure(ActionDisplay(action), item.WithArticle());
        if (!string.IsNullOrWhiteSpace(criticReason))
            neutral = $"{neutral} {criticReason}";

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, styleInstruction: actionModusMentis.StyleInstruction, ct: cancellationToken);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildNeutralOutcome(ParsedNarrativeAction action, bool succeeded, string? failureHint)
    {
        var d = ActionDisplay(action);
        if (succeeded) return NeutralNarration.OutcomeSuccess(d);
        var neutral = NeutralNarration.OutcomeFailure(d);
        return string.IsNullOrWhiteSpace(failureHint) ? neutral : $"{neutral} {failureHint}";
    }

    /// <summary>
    /// Clean action phrase for templates: prefers DisplayText ("climb the tree"), falling back to
    /// ActionText with any leading "try to " stripped.
    /// </summary>
    private static string ActionDisplay(ParsedNarrativeAction action)
    {
        if (!string.IsNullOrWhiteSpace(action.DisplayText)) return action.DisplayText;
        var text = action.ActionText ?? "";
        return text.StartsWith("try to ", StringComparison.OrdinalIgnoreCase) ? text.Substring(7) : text;
    }

    private Task<int> GetOrCreateNarratorSlotAsync(ModusMentis actionModusMentis)
        => _slotManager.GetOrCreateSlotForModusMentisAsync(actionModusMentis);
}
