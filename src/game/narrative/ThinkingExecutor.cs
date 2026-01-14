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
/// Manages thinking skill LLM requests using slots 10-29.
/// Handles instance creation, caching, and JSON-constrained action generation.
/// </summary>
public class ThinkingExecutor
{
    private readonly LlamaServerManager _llmManager;
    private readonly ThinkingPromptConstructor _promptConstructor;
    private readonly SkillSlotManager _slotManager;

    public ThinkingExecutor(
        LlamaServerManager llmManager,
        ThinkingPromptConstructor promptConstructor,
        SkillSlotManager slotManager)
    {
        _llmManager = llmManager;
        _promptConstructor = promptConstructor;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
    }

    /// <summary>
    /// Gets or creates a slot for the given thinking skill.
    /// Caches the persona prompt in the slot for reuse.
    /// </summary>
    private async Task<int> GetOrCreateSlotForSkillAsync(Skill skill)
    {
        return await _slotManager.GetOrCreateSlotForSkillAsync(skill);
    }

    /// <summary>
    /// Generates CoT reasoning and actions for the given keyword.
    /// Returns the parsed thinking response or null if generation failed.
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        Skill thinkingSkill,
        string keyword,
        NarrationNode node,
        List<OutcomeBase> possibleOutcomes,
        List<Skill> actionSkills,
        Avatar avatar,
        CancellationToken cancellationToken = default)
    {
        // Build the user prompt
        string userPrompt = _promptConstructor.BuildThinkingPrompt(
            keyword,
            node,
            possibleOutcomes,
            actionSkills,
            avatar,
            thinkingSkill);

        // Build JSON schema
        var schema = LLMSchemaConfig.CreateThinkingSchema(
            actionSkills.Select(s => s.SkillId).ToList(),
            possibleOutcomes.Select(o => o.ToNaturalLanguageString()).ToList());

        // Generate GBNF constraint
        string gbnfGrammar = JsonConstraintGenerator.GenerateGBNF(schema);

        // Get or create slot
        int slot = await GetOrCreateSlotForSkillAsync(thinkingSkill);

        // Request from LLM
        string? jsonResponse = await RequestFromLLMAsync(
            slot,
            userPrompt,
            gbnfGrammar,
            600, // Max tokens for reasoning + actions
            cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return null;
        }

        // Parse JSON response
        return ParseThinkingResponse(jsonResponse, possibleOutcomes, thinkingSkill, keyword);
    }

    /// <summary>
    /// Sends request to LLM and returns the complete response text.
    /// Uses event-based async pattern with TaskCompletionSource.
    /// </summary>
    private async Task<string?> RequestFromLLMAsync(
        int slot,
        string prompt,
        string grammar,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>();
        var responseText = string.Empty;

        void OnTokenStreamed(object? sender, TokenStreamedEventArgs e)
        {
            if (e.SlotId == slot)
            {
                responseText += e.Token;
            }
        }

        void OnRequestCompleted(object? sender, RequestCompletedEventArgs e)
        {
            if (e.SlotId == slot)
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

        await _llmManager.ContinueRequestAsync(
            slot,
            prompt,
            null, // onTokenStreamed - using events instead
            null, // onCompleted - using events instead
            grammar);

        var result = await tcs.Task;
        
        // Small delay to ensure LlamaServerManager's finally block completes cleanup
        await Task.Delay(100);
        
        return result;
    }



    /// <summary>
    /// Parses the LLM JSON response into a ThinkingResponse.
    /// Returns null if parsing fails.
    /// </summary>
    private ThinkingResponse? ParseThinkingResponse(string jsonResponse, List<OutcomeBase> possibleOutcomes, Skill thinkingSkill, string keyword)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            string reasoningText = root.GetProperty("reasoning_text").GetString() ?? "";
            
            var actions = new List<ParsedNarrativeAction>();
            var actionsArray = root.GetProperty("actions");

            foreach (var actionElement in actionsArray.EnumerateArray())
            {
                string actionSkill = actionElement.GetProperty("action_skill").GetString() ?? "";
                string outcomeStr = actionElement.GetProperty("outcome").GetString() ?? "";
                string actionDesc = actionElement.GetProperty("action_description").GetString() ?? "";

                // Parse outcome by matching natural language string
                var outcome = possibleOutcomes.FirstOrDefault(o => 
                    o.ToNaturalLanguageString().Equals(outcomeStr, StringComparison.OrdinalIgnoreCase));
                
                if (outcome != null)
                {
                    // Remove "try to " prefix from action description for DisplayText
                    string displayText = actionDesc;
                    if (displayText.StartsWith("try to ", StringComparison.OrdinalIgnoreCase))
                    {
                        displayText = displayText.Substring(7); // Remove "try to " (7 characters)
                    }
                    
                    actions.Add(new ParsedNarrativeAction
                    {
                        ActionSkillId = actionSkill,
                        PreselectedOutcome = outcome,
                        ActionText = actionDesc,
                        DisplayText = displayText,
                        ThinkingSkill = thinkingSkill,  // Set the thinking skill that generated this action
                        Keyword = keyword
                    });
                }
                else
                {
                    // Couldn't parse this outcome, skip it
                    Console.WriteLine($"ThinkingExecutor: Could not match outcome string '{outcomeStr}'");
                }
            }

            if (actions.Count == 0)
            {
                return null;
            }

            return new ThinkingResponse
            {
                ReasoningText = reasoningText,
                Actions = actions
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Represents the response from a thinking skill LLM request.
/// </summary>
public class ThinkingResponse
{
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
}
