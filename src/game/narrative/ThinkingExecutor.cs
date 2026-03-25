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
/// Manages thinking modusMentis LLM requests using slots 10-29.
/// Handles instance creation, caching, and JSON-constrained action generation.
/// </summary>
public class ThinkingExecutor
{
    private readonly LlamaServerManager _llmManager;
    private readonly ThinkingPromptConstructor _promptConstructor;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly Random _random = new();

    public ThinkingExecutor(
        LlamaServerManager llmManager,
        ThinkingPromptConstructor promptConstructor,
        ModusMentisSlotManager slotManager)
    {
        _llmManager = llmManager;
        _promptConstructor = promptConstructor;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
    }

    /// <summary>
    /// Gets or creates a slot for the given thinking modusMentis.
    /// Caches the persona prompt in the slot for reuse.
    /// </summary>
    private async Task<int> GetOrCreateSlotForModusMentisAsync(ModusMentis modusMentis)
    {
        return await _slotManager.GetOrCreateSlotForModusMentisAsync(modusMentis);
    }

    /// <summary>
    /// Generates CoT reasoning and actions for the given keyword.
    /// Returns the parsed thinking response or null if generation failed.
    /// </summary>
    /// <param name="thinkingModusMentis">The thinking modusMentis to use</param>
    /// <param name="keyword">The keyword that was clicked</param>
    /// <param name="keywordSourceOutcome">The outcome/element that the keyword relates to (e.g., "berry bush"), or null</param>
    /// <param name="node">The current narration node</param>
    /// <param name="outcomesWithMetadata">Possible outcomes with circuitous metadata</param>
    /// <param name="actionModiMentis">Available action modiMentis</param>
    /// <param name="protagonist">The player protagonist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        ModusMentis thinkingModusMentis,
        string keyword,
        string? keywordSourceOutcome,
        NarrationNode node,
        List<OutcomeWithMetadata> outcomesWithMetadata,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        CancellationToken cancellationToken = default)
    {
        var modusMentisIds = actionModiMentis.Select(s => s.ModusMentisId).ToList();

        // Get or create slot, then reset history so this thinking batch starts clean
        int slot = await GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(slot);

        // Sample K outcomes before reasoning so the LLM can consider skill-outcome pairing
        int totalActions = Math.Min(_random.Next(2, 4), outcomesWithMetadata.Count);
        totalActions = Math.Max(totalActions, 2); // at least 2
        var sampledOutcomes = SampleOutcomes(outcomesWithMetadata, totalActions);

        // ── Call 1: Reasoning ──────────────────────────────────────────────────────
        string reasoningPrompt = _promptConstructor.BuildReasoningPrompt(
            keyword, keywordSourceOutcome, node, sampledOutcomes,
            actionModiMentis, protagonist, thinkingModusMentis);

        string reasoningGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateReasoningSchema());

        string? reasoningJson = await RequestFromLLMAsync(slot, reasoningPrompt, reasoningGbnf, 400, cancellationToken);
        if (string.IsNullOrWhiteSpace(reasoningJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: Reasoning call returned empty response.");
            return null;
        }

        string reasoningText = ParseReasoningResponse(reasoningJson);
        Console.WriteLine($"ThinkingExecutor: Reasoning complete ({reasoningText.Length} chars)");

        // ── Per-outcome: step 3a (skill selection) + step 3b (action description) ──────────────
        var actions = new List<ParsedNarrativeAction>();
        var skillReasoningTexts = new List<string>();
        var chosenSkillIds = new List<string>();
        int actionIndex = 1;

        for (int i = 0; i < totalActions; i++)
        {
            var hardcodedOutcome = sampledOutcomes[i];
            string hardcodedOutcomeStr = hardcodedOutcome.Outcome.ToNaturalLanguageString();

            // Step 3a — skill selection
            string skillSelectionGbnf = JsonConstraintGenerator.GenerateGBNF(
                LLMSchemaConfig.CreateSkillSelectionSchema(modusMentisIds, hardcodedOutcomeStr));
            string skillSelectionPrompt = _promptConstructor.BuildSkillSelectionPrompt(
                hardcodedOutcomeStr, actionModiMentis, thinkingModusMentis, chosenSkillIds);

            string? skillSelectionJson = await RequestFromLLMAsync(
                slot, skillSelectionPrompt, skillSelectionGbnf, 300, cancellationToken);

            if (string.IsNullOrWhiteSpace(skillSelectionJson))
            {
                Console.Error.WriteLine($"ThinkingExecutor: Skill-selection {i + 1} returned empty response, skipping outcome.");
                continue;
            }

            var (skillReasoningText, selectedSkillId) = ParseSkillSelectionResponse(skillSelectionJson);
            if (string.IsNullOrEmpty(selectedSkillId))
            {
                Console.Error.WriteLine($"ThinkingExecutor: Skill-selection {i + 1} could not parse selected_skill, skipping outcome.");
                continue;
            }

            skillReasoningTexts.Add(skillReasoningText);
            chosenSkillIds.Add(selectedSkillId);
            Console.WriteLine($"ThinkingExecutor: Skill-selection {i + 1}/{totalActions}: '{selectedSkillId}' for '{hardcodedOutcomeStr}'");

            // Step 3b — action description
            string actionGbnf = JsonConstraintGenerator.GenerateGBNF(
                LLMSchemaConfig.CreateSingleActionSchema(hardcodedOutcomeStr, selectedSkillId));
            string actionPrompt = _promptConstructor.BuildSingleActionPrompt(
                actionIndex, totalActions, thinkingModusMentis, hardcodedOutcomeStr, selectedSkillId);

            string? actionJson = await RequestFromLLMAsync(slot, actionPrompt, actionGbnf, 200, cancellationToken);

            if (string.IsNullOrWhiteSpace(actionJson))
            {
                Console.Error.WriteLine($"ThinkingExecutor: Action description {i + 1} returned empty response.");
                continue;
            }

            var action = ParseSingleActionResponse(
                actionJson, hardcodedOutcome, selectedSkillId, actionModiMentis, thinkingModusMentis, keyword);
            if (action != null)
            {
                actions.Add(action);
                Console.WriteLine($"ThinkingExecutor: Action {actionIndex}/{totalActions} parsed: '{action.DisplayText}'");
                actionIndex++;
            }
            else
            {
                Console.Error.WriteLine($"ThinkingExecutor: Action description {i + 1} failed to parse.");
            }
        }

        if (actions.Count == 0)
        {
            Console.Error.WriteLine("ThinkingExecutor: All action calls failed.");
            return null;
        }

        // Concatenate overall reasoning with all per-outcome skill reasoning texts into one block
        string fullReasoningText = skillReasoningTexts.Count > 0
            ? reasoningText + " " + string.Join(" ", skillReasoningTexts)
            : reasoningText;

        return new ThinkingResponse
        {
            ReasoningText = fullReasoningText,
            Actions = actions
        };
    }
    
    /// <summary>
    /// Generates CoT reasoning and actions for the given keyword.
    /// Legacy overload that accepts plain outcomes (treats all as straightforward).
    /// Does not include outcome context for the keyword.
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        ModusMentis thinkingModusMentis,
        string keyword,
        NarrationNode node,
        List<OutcomeBase> possibleOutcomes,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        CancellationToken cancellationToken = default)
    {
        // Wrap all outcomes as straightforward
        var outcomesWithMetadata = possibleOutcomes
            .Select(o => OutcomeWithMetadata.Straightforward(o))
            .ToList();
            
        return await GenerateThinkingAsync(
            thinkingModusMentis, keyword, null, node, outcomesWithMetadata, actionModiMentis, protagonist, cancellationToken);
    }
    
    /// <summary>
    /// Generates CoT reasoning and actions for the given keyword with outcome context.
    /// Overload that accepts plain outcomes (treats all as straightforward) but includes keyword source.
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        ModusMentis thinkingModusMentis,
        string keyword,
        string? keywordSourceOutcome,
        NarrationNode node,
        List<OutcomeBase> possibleOutcomes,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        CancellationToken cancellationToken = default)
    {
        // Wrap all outcomes as straightforward
        var outcomesWithMetadata = possibleOutcomes
            .Select(o => OutcomeWithMetadata.Straightforward(o))
            .ToList();
            
        return await GenerateThinkingAsync(
            thinkingModusMentis, keyword, keywordSourceOutcome, node, outcomesWithMetadata, actionModiMentis, protagonist, cancellationToken);
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
    /// Parses the reasoning-only call response.
    /// </summary>
    private string ParseReasoningResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("reasoning_text").GetString() ?? "";
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Failed to parse reasoning JSON: {ex.Message}");
            return "";
        }
    }

    /// <summary>
    /// Parses the skill-selection call response (step 3a).
    /// Returns (SkillReasoningText, SelectedSkillId); returns ("", "") on failure.
    /// </summary>
    private (string SkillReasoningText, string SelectedSkillId) ParseSkillSelectionResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string skillReasoning = root.GetProperty("skill_reasoning_text").GetString() ?? "";
            string selectedSkill = root.GetProperty("selected_skill").GetString() ?? "";
            return (skillReasoning, selectedSkill);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Failed to parse skill-selection JSON: {ex.Message}");
            return ("", "");
        }
    }

    /// <summary>
    /// Parses a single-action call response (step 3b) into a <see cref="ParsedNarrativeAction"/>.
    /// Both outcome and skill are passed as hardcoded parameters — only action_description is read from JSON.
    /// </summary>
    private ParsedNarrativeAction? ParseSingleActionResponse(
        string json,
        OutcomeWithMetadata hardcodedOutcome,
        string hardcodedSkillId,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis,
        string keyword)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string actionDesc = root.GetProperty("action_description").GetString() ?? "";

            string displayText = actionDesc.StartsWith("try to ", StringComparison.OrdinalIgnoreCase)
                ? actionDesc.Substring(7)
                : actionDesc;

            var resolvedModusMentis = actionModiMentis.FirstOrDefault(s => s.ModusMentisId == hardcodedSkillId);

            return new ParsedNarrativeAction
            {
                ActionModusMentisId = hardcodedSkillId,
                ActionModusMentis = resolvedModusMentis,
                PreselectedOutcome = hardcodedOutcome.Outcome,
                ActionText = actionDesc,
                DisplayText = displayText,
                ThinkingModusMentis = thinkingModusMentis,
                Keyword = keyword
            };
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Failed to parse single action JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Randomly samples <paramref name="k"/> outcomes from the pool (without replacement).
    /// Used to pre-assign outcomes to each action before the reasoning call.
    /// </summary>
    private List<OutcomeWithMetadata> SampleOutcomes(List<OutcomeWithMetadata> outcomes, int k)
    {
        return outcomes.OrderBy(_ => _random.Next()).Take(k).ToList();
    }

    /// <summary>
    /// Parses the LLM JSON response into a ThinkingResponse.
    /// Returns null if parsing fails.
    /// </summary>
    private ThinkingResponse? ParseThinkingResponse(
        string jsonResponse, 
        List<OutcomeWithMetadata> outcomesWithMetadata, 
        List<ModusMentis> actionModiMentis, 
        ModusMentis thinkingModusMentis, 
        string keyword)
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
                string actionModusMentis = actionElement.GetProperty("action_modusMentis").GetString() ?? "";
                string outcomeStr = actionElement.GetProperty("outcome").GetString() ?? "";
                string actionDesc = actionElement.GetProperty("action_description").GetString() ?? "";

                // Find outcome with metadata by matching natural language string
                var outcomeWithMeta = outcomesWithMetadata.FirstOrDefault(o => 
                    o.Outcome.ToNaturalLanguageString().Equals(outcomeStr, StringComparison.OrdinalIgnoreCase));
                
                if (outcomeWithMeta != null)
                {
                    // Remove "try to " prefix from action description for DisplayText
                    string displayText = actionDesc;
                    if (displayText.StartsWith("try to ", StringComparison.OrdinalIgnoreCase))
                    {
                        displayText = displayText.Substring(7); // Remove "try to " (7 characters)
                    }
                    
                    // Resolve the actual modusMentis instance from actionModiMentis list
                    var resolvedActionModusMentis = actionModiMentis.FirstOrDefault(s => s.ModusMentisId == actionModusMentis);
                    
                    actions.Add(new ParsedNarrativeAction
                    {
                        ActionModusMentisId = actionModusMentis,
                        ActionModusMentis = resolvedActionModusMentis, // Set the resolved modusMentis instance with proper level
                        PreselectedOutcome = outcomeWithMeta.Outcome,
                        ActionText = actionDesc,
                        DisplayText = displayText,
                        ThinkingModusMentis = thinkingModusMentis,
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
/// Represents the response from a thinking modusMentis LLM request.
/// </summary>
public class ThinkingResponse
{
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
}
