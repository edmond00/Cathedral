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
/// Generates narration for action outcomes from the thinking skill's perspective.
/// Uses LLM slot 51 for outcome narration.
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
    /// Generates narration for an action outcome from the thinking skill's perspective.
    /// </summary>
    public async Task<string> NarrateOutcomeAsync(
        ParsedNarrativeAction action,
        Skill actionSkill,
        Skill thinkingSkill,
        OutcomeBase outcome,
        bool succeeded,
        double difficulty,
        Avatar avatar,
        CancellationToken cancellationToken = default)
    {
        // Ensure narrator slot is initialized and get slot ID
        int slotId = await GetOrCreateNarratorSlotAsync(thinkingSkill);

        // Build prompt
        string prompt = BuildNarrationPrompt(
            action,
            actionSkill,
            thinkingSkill,
            outcome,
            succeeded,
            difficulty,
            avatar);

        // Build JSON schema for narration
        var schema = new CompositeField("OutcomeNarration",
            new StringField("narration", MinLength: 50, MaxLength: 800, Hint: "A short narration text describing the outcome of the action")
        );

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
    /// Ensures the narrator slot is initialized with the thinking skill's persona.
    /// Returns the slot ID for this skill.
    /// </summary>
    private async Task<int> GetOrCreateNarratorSlotAsync(Skill thinkingSkill)
    {
        // Use SkillSlotManager to reuse the same slot as thinking phase
        return await _slotManager.GetOrCreateSlotForSkillAsync(thinkingSkill);
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
        Avatar avatar)
    {
        string outcomeDescription = outcome.ToNaturalLanguageString();
        string successStatus = succeeded ? "succeeded" : "failed";
        string difficultyDesc = difficulty < 0.3 ? "easy" : difficulty < 0.7 ? "moderate" : "hard";

        return $@"You ({thinkingSkill.DisplayName}) suggested the following action:
""{action.ActionText}""

The action used the {actionSkill.DisplayName} skill (difficulty: {difficultyDesc}).

The action {successStatus}.

{(succeeded 
    ? $"Outcome: {outcomeDescription}" 
    : "The attempt did not achieve the desired result.")}

Narrate what happened from your perspective as {thinkingSkill.DisplayName}. 
- If success: Describe how {actionSkill.DisplayName} accomplished it
- If failure: Reflect on why it didn't work
- Use the tone of {thinkingSkill.PersonaTone}
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
