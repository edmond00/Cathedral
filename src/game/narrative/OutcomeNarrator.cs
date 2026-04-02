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

    public OutcomeNarrator(LlamaServerManager llmManager, ModusMentisSlotManager slotManager)
    {
        _llmManager = llmManager;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
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
        Protagonist protagonist,
        CancellationToken cancellationToken = default,
        string? failureHint = null)
    {
        // Ensure narrator slot is initialized with action modusMentis's persona
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);

        // Build prompt
        string prompt = BuildNarrationPrompt(
            action,
            actionModusMentis,
            outcome,
            succeeded,
            difficulty,
            protagonist,
            failureHint);

        // Build JSON schema for narration
        var schema = LLMSchemaConfig.CreateOutcomeNarrationSchema();

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
            narrationText = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty("what_happened").GetString() ?? "");
            if (string.IsNullOrWhiteSpace(narrationText))
                return GenerateFallbackNarration(action, succeeded, outcome);
        }
        catch (JsonException)
        {
            return GenerateFallbackNarration(action, succeeded, outcome);
        }

        // Follow-up: what do you feel?
        string feeling = await RequestFeelingAsync(slotId, actionModusMentis.PersonaReminder2, cancellationToken);
        return string.IsNullOrWhiteSpace(feeling)
            ? narrationText
            : $"{narrationText} {feeling}";
    }
    
    /// <summary>
    /// Generates narration explaining why an action failed plausibility checks.
    /// </summary>
    public async Task<string> NarratePlausibilityFailureAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        string plausibilityError,
        Protagonist protagonist,
        CancellationToken cancellationToken = default)
    {
        // Use the action modusMentis's slot for plausibility failure narration
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);

        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}."
            : $"You are {actionModusMentis.DisplayName}.";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        string prompt = $@"{personaToneLine}
{WorldContext.EpochContext}
You tried to {action.ActionText} but it could not happen.
{plausibilityError}

{reminderClause}what happened?
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";

        var schema = LLMSchemaConfig.CreateOutcomeNarrationSchema();
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
            narrationText = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty("what_happened").GetString() ?? "");
            if (string.IsNullOrWhiteSpace(narrationText))
                return plausibilityError;
        }
        catch (JsonException)
        {
            return plausibilityError;
        }

        // Follow-up: what do you feel?
        string feeling = await RequestFeelingAsync(slotId, actionModusMentis.PersonaReminder2, cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        int slotId = await GetOrCreateNarratorSlotAsync(actionModusMentis);

        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}."
            : $"You are {actionModusMentis.DisplayName}.";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        string prompt = $@"{personaToneLine}
{WorldContext.EpochContext}
You want to: {action.ActionText}.
You are holding: {item.DisplayName} ({item.Description}).

{reminderClause}explain in one sentence why using {item.DisplayName} here simply does not work or makes no sense.
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
        Protagonist protagonist,
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

{reminderClause}what happened?
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Follow-up call in the same slot: asks "what do you feel?" after outcome narration.
    /// The slot still holds the narration context, so no prompt reset is needed.
    /// Returns the parsed feeling sentence, or empty string on failure.
    /// </summary>
    private async Task<string> RequestFeelingAsync(int slotId, string? personaReminder2, CancellationToken cancellationToken)
    {
        string prompt = "What do you feel about this outcome?\n" + Config.Narrative.AnswerInstructionFor(personaReminder2);
        var schema = LLMSchemaConfig.CreateFeelingSchema();
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf, cancellationToken);
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            return TextTruncationUtils.TrimToLastSentence(
                doc.RootElement.GetProperty("what_i_feel").GetString() ?? "");
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
