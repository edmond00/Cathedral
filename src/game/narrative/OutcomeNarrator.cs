using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        ModusMentis thinkingModusMentis,
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
            thinkingModusMentis,
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
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            string narration = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty("what_happened").GetString() ?? "");
            return !string.IsNullOrWhiteSpace(narration)
                ? narration
                : GenerateFallbackNarration(action, succeeded, outcome);
        }
        catch (JsonException)
        {
            return GenerateFallbackNarration(action, succeeded, outcome);
        }
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
You tried to {action.ActionText} but it could not happen.
{plausibilityError}

{reminderClause}what happened?";

        var schema = LLMSchemaConfig.CreateOutcomeNarrationSchema();
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf, cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return plausibilityError; // Fallback to the raw error message
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            string narration = TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty("what_happened").GetString() ?? "");
            return !string.IsNullOrWhiteSpace(narration) ? narration : plausibilityError;
        }
        catch (JsonException)
        {
            return plausibilityError;
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
        ModusMentis thinkingModusMentis,
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
You tried to {action.ActionText}.
{resultLine}

{reminderClause}what happened?";
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
