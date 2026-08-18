using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Rules;
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

    /// <summary>GBNF-forced opening for outcome narration — keeps the styled result first-person.</summary>
    private const string OutcomePrefix = "I ";

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
        INarratable outcome,
        bool succeeded,
        double difficulty,
        PartyMember protagonist,
        CancellationToken cancellationToken = default,
        IReadOnlyList<string>? outcomeVerbatims = null,
        string? neutralOverride = null,
        ILlmPreviewSink? preview = null)
    {
        // The reminescence path supplies its own neutral meaning (a plain "I tried to remember …"
        // framing that embeds the concrete recovered memory); everything else templates it here.
        string neutral = neutralOverride ?? BuildNeutralOutcome(action, succeeded, outcomeVerbatims);
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        // forcedPrefix "I " constrains the styled result to a first-person opening (every neutral
        // outcome is first-person — "I succeeded to …", "Alas, I failed to …", "I tried to remember
        // …"), the same GBNF trick the action rewrite uses. When a preview
        // sink is supplied, the outcome streams into the preview box like every other narration.
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, forcedPrefix: OutcomePrefix,
            styleInstruction: actionModusMentis.StyleInstruction, preview: preview, ct: cancellationToken);
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
        INarratable successOutcome,
        INarratable failureOutcome,
        double difficulty,
        PartyMember protagonist,
        IReadOnlyList<string>? successVerbatims,
        IReadOnlyList<string>? failureVerbatims,
        CancellationToken cancellationToken = default)
    {
        if (PlaygroundMode.IsActive)
        {
            _pendingNarratorSlot = -1;
            return (BuildNeutralOutcome(action, true, successVerbatims),
                    BuildNeutralOutcome(action, false, failureVerbatims));
        }

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        var instance = _llmManager.GetInstance(slotId);
        var baseline = instance?.SnapshotHistory();

        string success = await NarrateOutcomeAsync(
            action, actionModusMentis, successOutcome, true, difficulty, protagonist, cancellationToken,
            outcomeVerbatims: successVerbatims);
        var afterSuccess = instance?.SnapshotHistory();

        // Reset to the pre-narration baseline so the failure branch generates without seeing the
        // success turns, then snapshot the failure branch state.
        if (instance != null && baseline != null) instance.RestoreHistory(baseline);

        string failure = await NarrateOutcomeAsync(
            action, actionModusMentis, failureOutcome, false, difficulty, protagonist, cancellationToken,
            outcomeVerbatims: failureVerbatims);
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
        CancellationToken cancellationToken = default,
        ILlmPreviewSink? preview = null)
    {
        string neutral = NeutralNarration.PlausibilityFailure(ActionDisplay(action));
        if (!string.IsNullOrWhiteSpace(plausibilityError))
            neutral = $"{neutral} {plausibilityError}";

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, forcedPrefix: OutcomePrefix,
            styleInstruction: actionModusMentis.StyleInstruction, preview: preview, ct: cancellationToken);
    }

    /// <summary>
    /// Narrates a coded-rule refusal (witness present, under threat, …) in the action Modus Mentis's
    /// voice. <paramref name="reason"/> is the rule's first-person reason phrase.
    /// </summary>
    public async Task<string> NarrateRefusalAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        string reason,
        PartyMember protagonist,
        CancellationToken cancellationToken = default,
        ILlmPreviewSink? preview = null)
    {
        string neutral = NeutralNarration.ActionImpossible(ActionDisplay(action), reason);

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, forcedPrefix: OutcomePrefix,
            styleInstruction: actionModusMentis.StyleInstruction, preview: preview, ct: cancellationToken);
    }

    /// <summary>
    /// Narrates why a combined item cannot be used for the action, in the action Modus Mentis's voice.
    ///
    /// <para><paramref name="kind"/> chooses the neutral sentence, and they are genuinely different
    /// pieces of news — the implement is wrong, the act admits of no implement, the act is a blow and
    /// the thing is no weapon, the hands have no craft in them, or the idea was sound and the hands
    /// were not. Collapsing them into one "it did not work" leaves the rewrite to invent which, and
    /// it invents the flattering one.</para>
    ///
    /// <para>Only <see cref="ToolFailureKind.WrongTool"/> carries the critic's own reason, because it
    /// is the only kind an LLM was asked about.</para>
    /// </summary>
    public async Task<string> NarrateItemCombinationFailureAsync(
        ParsedNarrativeAction action,
        Item item,
        ModusMentis actionModusMentis,
        string criticReason = "",
        ToolFailureKind kind = ToolFailureKind.WrongTool,
        CancellationToken cancellationToken = default,
        ILlmPreviewSink? preview = null)
    {
        string display = ActionDisplay(action);
        string neutral = kind switch
        {
            ToolFailureKind.Senseless     => NeutralNarration.ItemCombinationSenseless(display, item.WithArticle()),
            ToolFailureKind.NotItsPurpose => NeutralNarration.ItemCombinationNotItsPurpose(display, item.WithArticle()),
            ToolFailureKind.NoProficiency => NeutralNarration.ItemCombinationNoProficiency(item.WithArticle()),
            ToolFailureKind.BeyondSkill   => NeutralNarration.ItemCombinationBeyondSkill(display, item.WithArticle()),
            ToolFailureKind.NotAWeapon    => NeutralNarration.ItemCombinationNotAWeapon(item.WithArticle()),
            _                             => NeutralNarration.ItemCombinationFailure(display, item.WithArticle()),
        };
        if (kind == ToolFailureKind.WrongTool && !string.IsNullOrWhiteSpace(criticReason))
            neutral = $"{neutral} {criticReason}";

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);
        return await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Outcome,
            actionModusMentis.PersonaReminder2, keepHistory: true, forcedPrefix: OutcomePrefix,
            styleInstruction: actionModusMentis.StyleInstruction, preview: preview, ct: cancellationToken);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildNeutralOutcome(
        ParsedNarrativeAction action, bool succeeded, IReadOnlyList<string>? outcomeVerbatims)
    {
        var d = ActionDisplay(action);
        return succeeded
            ? NeutralNarration.OutcomeSuccess(d, outcomeVerbatims)
            : NeutralNarration.OutcomeFailure(d, outcomeVerbatims);
    }

    /// <summary>
    /// Clean neutral action phrase for the neutral-meaning templates fed back to the persona rewriter.
    /// Prefers <see cref="ParsedNarrativeAction.NeutralActionText"/> ("get up and continue my journey")
    /// so the "I tried to …" framing embeds the plain phrasing rather than the already-styled label;
    /// falls back to DisplayText, then to ActionText with any leading "try to " stripped.
    /// </summary>
    private static string ActionDisplay(ParsedNarrativeAction action)
    {
        if (!string.IsNullOrWhiteSpace(action.NeutralActionText)) return action.NeutralActionText;
        if (!string.IsNullOrWhiteSpace(action.DisplayText)) return action.DisplayText;
        var text = action.ActionText ?? "";
        return text.StartsWith("try to ", StringComparison.OrdinalIgnoreCase) ? text.Substring(7) : text;
    }

    private Task<int> GetOrCreateNarratorSlotAsync(ModusMentis actionModusMentis)
        => _slotManager.GetOrCreateSlotForModusMentisAsync(actionModusMentis);
}
