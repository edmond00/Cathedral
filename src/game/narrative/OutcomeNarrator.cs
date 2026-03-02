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
/// Generates narration for action outcomes from the action skill's perspective.
/// Uses the action skill's LLM slot for outcome narration.
/// </summary>
public class OutcomeNarrator
{
    private readonly LlamaServerManager _llmManager;
    private readonly SkillSlotManager _slotManager;

    public OutcomeNarrator(LlamaServerManager llmManager, SkillSlotManager slotManager)
    {
        _llmManager = llmManager;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
    }

    /// <summary>
    /// Generates narration for an action outcome from the action skill's perspective.
    /// </summary>
    public async Task<string> NarrateOutcomeAsync(
        ParsedNarrativeAction action,
        Skill actionSkill,
        Skill thinkingSkill,
        OutcomeBase outcome,
        bool succeeded,
        double difficulty,
        Protagonist protagonist,
        CancellationToken cancellationToken = default,
        string? failureHint = null)
    {
        // Ensure narrator slot is initialized with action skill's persona
        int slotId = await GetOrCreateNarratorSlotAsync(actionSkill);

        // Build prompt
        string prompt = BuildNarrationPrompt(
            action,
            actionSkill,
            thinkingSkill,
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
            string narration = doc.RootElement.GetProperty("narration").GetString() ?? "";
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
        Skill actionSkill,
        string plausibilityError,
        Protagonist protagonist,
        CancellationToken cancellationToken = default)
    {
        // Use the action skill's slot for plausibility failure narration
        int slotId = await GetOrCreateNarratorSlotAsync(actionSkill);

        string prompt = $@"You are {actionSkill.DisplayName}, explaining why an action cannot be performed.

The attempted action was: ""{action.ActionText}""

The reason it's not possible: {plausibilityError}

As {actionSkill.DisplayName}, explain in your unique voice why this action cannot be done right now.
- Be concise but characterful
- Stay in character with {actionSkill.PersonaTone} tone
- Suggest what might be needed or what's wrong with the attempt
- Keep it 50-200 characters

Respond in JSON format:
{{
  ""narration"": ""your explanation""
}}";

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
            string narration = doc.RootElement.GetProperty("narration").GetString() ?? "";
            return !string.IsNullOrWhiteSpace(narration) ? narration : plausibilityError;
        }
        catch (JsonException)
        {
            return plausibilityError;
        }
    }

    /// <summary>
    /// Ensures the narrator slot is initialized with the action skill's persona.
    /// Returns the slot ID for this skill.
    /// </summary>
    private async Task<int> GetOrCreateNarratorSlotAsync(Skill actionSkill)
    {
        // Use SkillSlotManager to get slot for the action skill
        return await _slotManager.GetOrCreateSlotForSkillAsync(actionSkill);
    }

    /// <summary>
    /// Builds the prompt for outcome narration.
    /// </summary>
    private string BuildNarrationPrompt(
        ParsedNarrativeAction action,
        Skill actionSkill,
        Skill thinkingSkill,
        OutcomeBase outcome,
        bool succeeded,
        double difficulty,
        Protagonist protagonist,
        string? failureHint = null)
    {
        string outcomeDescription = outcome.ToNaturalLanguageString();
        string successStatus = succeeded ? "succeeded" : "failed";
        string difficultyDesc = difficulty < 0.3 ? "easy" : difficulty < 0.7 ? "moderate" : "hard";

        string failureGuidance = "";
        if (!succeeded && !string.IsNullOrEmpty(failureHint))
        {
            failureGuidance = $"\nWhat happened in the failure: {failureHint}";
        }

        return $@"You are {actionSkill.DisplayName}, narrating the outcome of an action you performed.

The action was: ""{action.ActionText}""

Difficulty: {difficultyDesc}
Result: {successStatus}

{(succeeded 
    ? $"Outcome: {outcomeDescription}" 
    : $"The attempt did not achieve the desired result.{failureGuidance}")}

Narrate what happened from your perspective as {actionSkill.DisplayName}. 
- If success: Describe how you accomplished it and what you achieved
- If failure: Describe specifically what went wrong based on the failure guidance
- Use the tone of {actionSkill.PersonaTone}
- Keep it 100-400 characters

Respond in JSON format:
{{
  ""narration"": ""your narration text""
}}";
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
