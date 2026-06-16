using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Generates narration for action outcomes from the action modusMentis's perspective.
/// Uses the action modusMentis's LLM slot for outcome narration.
/// </summary>
public class OutcomeNarrator
{
    private readonly LlamaServerManager _llmManager;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly QuestionFillerService _questionFillerService;

    public OutcomeNarrator(LlamaServerManager llmManager, ModusMentisSlotManager slotManager, QuestionFillerService? questionFillerService = null)
    {
        _llmManager = llmManager;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _questionFillerService = questionFillerService ?? QuestionFillerService.Instance;
    }

    /// <summary>
    /// Generates narration for an action outcome from the action modusMentis's perspective.
    /// </summary>
    public async Task<string> NarrateOutcomeAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        OutcomeBase outcome,
        bool succeeded,
        double difficulty,
        PartyMember protagonist,
        CancellationToken cancellationToken = default,
        string? failureHint = null)
    {
        if (PlaygroundMode.IsActive)
        {
            var d = PlaygroundActionDisplay(action);
            return succeeded ? PlaygroundNarration.OutcomeSuccess(d) : PlaygroundNarration.OutcomeFailure(d);
        }

        // Ensure narrator slot is initialized with action modusMentis's persona
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);

        // Resolve questions from the filler service
        var happenedRef = succeeded ? QuestionReference.OutcomeSucceededHappened : QuestionReference.OutcomeFailedHappened;
        var happenedQ = _questionFillerService.GetNext(actionModusMentis, happenedRef);

        // Build prompt
        string prompt = BuildNarrationPrompt(
            action,
            actionModusMentis,
            outcome,
            succeeded,
            difficulty,
            protagonist,
            happenedQ.PromptText,
            failureHint);

        // Build JSON schema for narration
        var schema = LLMSchemaConfig.CreateOutcomeNarrationSchema(happenedQ.JsonFieldName);

        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        // Request from LLM
        string? jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf, cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return GenerateFallbackNarration(action, succeeded, outcome);
        }

        // Parse response
        string narrationText;
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            narrationText = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty(happenedQ.JsonFieldName).GetString() ?? "");
            if (string.IsNullOrWhiteSpace(narrationText))
                return GenerateFallbackNarration(action, succeeded, outcome);
        }
        catch (JsonException)
        {
            return GenerateFallbackNarration(action, succeeded, outcome);
        }

        // Follow-up: what do you feel?
        string feeling = await RequestFeelingAsync(slotId, actionModusMentis, action.ActionText, succeeded, cancellationToken);
        return string.IsNullOrWhiteSpace(feeling)
            ? narrationText
            : $"{narrationText} {feeling}";
    }

    // ── Dual outcome pre-generation (for humor dice modifiers) ─────────────────
    // Both success and failure narration are generated up-front during the dice animation so
    // the player can flip the result via humor modifiers with no further loading. Each branch is
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
            var d = PlaygroundActionDisplay(action);
            return (PlaygroundNarration.OutcomeSuccess(d), PlaygroundNarration.OutcomeFailure(d));
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
    /// Generates narration explaining why an action failed plausibility checks.
    /// </summary>
    public async Task<string> NarratePlausibilityFailureAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        string plausibilityError,
        PartyMember protagonist,
        CancellationToken cancellationToken = default)
    {
        if (PlaygroundMode.IsActive)
            return PlaygroundNarration.PlausibilityFailure(PlaygroundActionDisplay(action));

        // Use the action modusMentis's slot for plausibility failure narration
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);

        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}."
            : $"You are {actionModusMentis.DisplayName}.";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        var happenedQ = _questionFillerService.GetNext(actionModusMentis, QuestionReference.OutcomeFailedHappened);

        string prompt = $@"{personaToneLine}
{WorldContext.EpochContext}
You tried to {action.ActionText} but it could not happen.
{plausibilityError}

{reminderClause}{happenedQ.PromptText}
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";

        var schema = LLMSchemaConfig.CreateOutcomeNarrationSchema(happenedQ.JsonFieldName);
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf, cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return plausibilityError; // Fallback to the raw error message
        }

        string narrationText;
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            narrationText = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty(happenedQ.JsonFieldName).GetString() ?? "");
            if (string.IsNullOrWhiteSpace(narrationText))
                return plausibilityError;
        }
        catch (JsonException)
        {
            return plausibilityError;
        }

        // Follow-up: what do you feel?
        string feeling = await RequestFeelingAsync(slotId, actionModusMentis, action.ActionText, false, cancellationToken);
        return string.IsNullOrWhiteSpace(feeling)
            ? narrationText
            : $"{narrationText} {feeling}";
    }

    /// <summary>
    /// Generates a short narration explaining why a combined item cannot be used for the action
    /// (i.e. the item appropriateness critic rejected the combination).
    /// </summary>
    public async Task<string> NarrateItemCombinationFailureAsync(
        ParsedNarrativeAction action,
        Item item,
        ModusMentis actionModusMentis,
        string criticReason = "",
        CancellationToken cancellationToken = default)
    {
        if (PlaygroundMode.IsActive)
            return PlaygroundNarration.ItemCombinationFailure(PlaygroundActionDisplay(action), item.WithArticle());

        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);

        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}."
            : $"You are {actionModusMentis.DisplayName}.";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";
        string criticLine = !string.IsNullOrWhiteSpace(criticReason)
            ? $"{criticReason}\n"
            : "";

        string prompt = $@"{personaToneLine}
{WorldContext.EpochContext}
You are about to: {action.ActionText}.
You are in possession of {item.WithArticle()} ({item.DescriptionLower()}).
You want to use this item to realize this action but it is not suitable.
{criticLine}
{reminderClause}explain in one sentence why using {item.WithArticle()} here simply does not work or makes no sense.
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";

        var schema = LLMSchemaConfig.CreateOutcomeNarrationSchema();
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf, cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonResponse))
            return $"Using {item.DisplayName} here does not help.";

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            string narration = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty("what_happened").GetString() ?? "");
            return string.IsNullOrWhiteSpace(narration)
                ? $"Using {item.DisplayName} here does not help."
                : narration;
        }
        catch (JsonException)
        {
            return $"Using {item.DisplayName} here does not help.";
        }
    }

    /// <summary>
    /// Resolves the clean action phrase for playground templates: prefers DisplayText
    /// ("climb the tree"), falling back to ActionText with any leading "try to " stripped.
    /// </summary>
    private static string PlaygroundActionDisplay(ParsedNarrativeAction action)
    {
        if (!string.IsNullOrWhiteSpace(action.DisplayText)) return action.DisplayText;
        var text = action.ActionText ?? "";
        return text.StartsWith("try to ", StringComparison.OrdinalIgnoreCase) ? text.Substring(7) : text;
    }

    /// <summary>
    /// Ensures the narrator slot is initialized with the action modusMentis's persona.
    /// Returns the slot ID for this modusMentis.
    /// </summary>
    private async Task<int> GetOrCreateNarratorSlotAsync(ModusMentis actionModusMentis)
    {
        // Use ModusMentisSlotManager to get slot for the action modusMentis
        return await _slotManager.GetOrCreateSlotForModusMentisAsync(actionModusMentis);
    }

    /// <summary>
    /// Builds the prompt for outcome narration.
    /// </summary>
    private string BuildNarrationPrompt(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        OutcomeBase outcome,
        bool succeeded,
        double difficulty,
        PartyMember protagonist,
        string questionText,
        string? failureHint = null)
    {
        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}."
            : $"You are {actionModusMentis.DisplayName}.";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        string resultLine;
        if (succeeded)
        {
            string difficultyNote = difficulty < 0.3
                ? "without much effort"
                : difficulty < 0.7
                    ? "after some difficulty"
                    : "against the odds";
            string outcomeDescription = outcome.ToNaturalLanguageString();
            resultLine = $"You succeeded {difficultyNote}. {outcomeDescription}.";
        }
        else
        {
            string failureLine = string.IsNullOrEmpty(failureHint)
                ? "You failed."
                : $"You failed. {failureHint}";
            resultLine = failureLine;
        }

        return $@"{personaToneLine}
{WorldContext.EpochContext}
You tried to {action.ActionText}.
{resultLine}

{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Follow-up call in the same slot: asks "what do you feel?" after outcome narration.
    /// The slot still holds the narration context, so no prompt reset is needed.
    /// Returns the parsed feeling sentence, or empty string on failure.
    /// </summary>
    private async Task<string> RequestFeelingAsync(int slotId, ModusMentis actionModusMentis, string actionText, bool succeeded, CancellationToken cancellationToken)
    {
        var feelRef = succeeded ? QuestionReference.OutcomeSucceededFeel : QuestionReference.OutcomeFailedFeel;
        var feelQ = _questionFillerService.GetNext(actionModusMentis, feelRef);
        string outcomeReminder = succeeded
            ? $"You tried to {actionText} and you succeeded."
            : $"You tried to {actionText} and you failed.";
        string prompt = $"{outcomeReminder}\n{feelQ.PromptText}\n{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
        var schema = LLMSchemaConfig.CreateFeelingSchema(feelQ.JsonFieldName);
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf, cancellationToken);
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            return TextTruncationUtils.TrimToLastSentence(
                doc.RootElement.GetProperty(feelQ.JsonFieldName).GetString() ?? "");
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Sends request to LLM and returns the complete response text.
    /// </summary>
    private async Task<string?> RequestFromLLMAsync(
        int slotId,
        string prompt,
        string grammar,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>();
        var responseText = string.Empty;

        void OnTokenStreamed(object? sender, TokenStreamedEventArgs e)
        {
            if (e.SlotId == slotId)
            {
                responseText += e.Token;
            }
        }

        void OnRequestCompleted(object? sender, RequestCompletedEventArgs e)
        {
            if (e.SlotId == slotId)
            {
                _llmManager.TokenStreamed -= OnTokenStreamed;
                _llmManager.RequestCompleted -= OnRequestCompleted;

                if (!e.WasCancelled)
                {
                    tcs.SetResult(responseText);
                }
                else
                {
                    tcs.SetResult(string.Empty);
                }
            }
        }

        _llmManager.TokenStreamed += OnTokenStreamed;
        _llmManager.RequestCompleted += OnRequestCompleted;

        try
        {
            await _llmManager.ContinueRequestAsync(
                slotId,
                prompt,
                null,
                null,
                grammar);

            var result = await tcs.Task;
            
            // Small delay to ensure LlamaServerManager's finally block completes cleanup
            await Task.Delay(100);
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OutcomeNarrator: LLM request failed: {ex.Message}");
            _llmManager.TokenStreamed -= OnTokenStreamed;
            _llmManager.RequestCompleted -= OnRequestCompleted;
            return null;
        }
    }

    /// <summary>
    /// Generates a simple fallback narration when LLM fails.
    /// </summary>
    private string GenerateFallbackNarration(ParsedNarrativeAction action, bool succeeded, OutcomeBase outcome)
    {
        if (succeeded)
        {
            return $"The action succeeded. {outcome.ToNaturalLanguageString()}.";
        }
        else
        {
            return "The attempt failed. Perhaps another approach would work better.";
        }
    }
}
