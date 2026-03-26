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
    /// New 3-call pipeline: WHY (thinking slot) → HOW (thinking slot) → WHAT (action slot).
    /// The outcome is fully predetermined by the clicked keyword.
    /// Returns a ThinkingResponse with one action, or null if any call fails.
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        ModusMentis thinkingModusMentis,
        ConcreteOutcome targetOutcome,
        string keyword,
        NarrationNode node,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken = default)
    {
        string outcomeDescription = targetOutcome.ToNaturalLanguageString();
        var skillMeans = actionModiMentis.Select(s => $"with {s.SkillMeans}").ToList();

        // Get thinking slot — reset context for a fresh batch
        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        // ── Call 1: WHY ────────────────────────────────────────────────────────────
        string whyPrompt = _promptConstructor.BuildWhyPrompt(keyword, outcomeDescription, node, thinkingModusMentis, protagonist, worldContext, targetOutcome);
        string whyGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhySchema());

        string? whyJson = await RequestFromLLMAsync(thinkingSlot, whyPrompt, whyGbnf, 350, cancellationToken);
        if (string.IsNullOrWhiteSpace(whyJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: WHY call returned empty response.");
            return null;
        }

        string whyText = ParseSingleTextField(whyJson, "what_do_i_think");
        Console.WriteLine($"ThinkingExecutor: WHY complete ({whyText.Length} chars)");

        // ── Call 2: HOW ────────────────────────────────────────────────────────────
        string howPrompt = _promptConstructor.BuildHowPrompt(outcomeDescription, actionModiMentis, thinkingModusMentis);
        string howGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateHowSchema(skillMeans));

        string? howJson = await RequestFromLLMAsync(thinkingSlot, howPrompt, howGbnf, 350, cancellationToken);
        if (string.IsNullOrWhiteSpace(howJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: HOW call returned empty response.");
            return null;
        }

        var (howText, selectedMeans) = ParseHowResponse(howJson);
        if (string.IsNullOrEmpty(selectedMeans))
        {
            Console.Error.WriteLine("ThinkingExecutor: HOW call could not parse selected_approach.");
            return null;
        }

        Console.WriteLine($"ThinkingExecutor: HOW complete — selected approach: '{selectedMeans}'");

        var selectedModusMentis = MapMeansToModusMentis(selectedMeans, actionModiMentis);
        if (selectedModusMentis == null)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Could not map approach '{selectedMeans}' to any action modusMentis.");
            return null;
        }
        string selectedSkillId = selectedModusMentis.ModusMentisId;

        // ── Call 3: WHAT (action modusMentis slot) ─────────────────────────────────
        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(selectedModusMentis);
        _llmManager.ResetInstance(actionSlot);

        string whatPrompt = _promptConstructor.BuildWhatPrompt(keyword, outcomeDescription, node, protagonist, selectedModusMentis, worldContext, targetOutcome);
        string whatGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhatSchema());

        string? whatJson = await RequestFromLLMAsync(actionSlot, whatPrompt, whatGbnf, 250, cancellationToken);
        if (string.IsNullOrWhiteSpace(whatJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: WHAT call returned empty response.");
            return null;
        }

        string actionDescription = ParseSingleTextField(whatJson, "action_description");
        string displayText = actionDescription.StartsWith("try to ", StringComparison.OrdinalIgnoreCase)
            ? actionDescription.Substring(7)
            : actionDescription;

        Console.WriteLine($"ThinkingExecutor: WHAT complete — action: '{displayText}'");

        // Combine WHY + HOW reasoning into one block
        string reasoningText = (whyText + " " + howText).Trim();

        var action = new ParsedNarrativeAction
        {
            ActionModusMentisId = selectedSkillId,
            ActionModusMentis = selectedModusMentis,
            PreselectedOutcome = targetOutcome,
            ActionText = actionDescription,
            DisplayText = displayText,
            ThinkingModusMentis = thinkingModusMentis,
            Keyword = keyword
        };

        return new ThinkingResponse
        {
            ReasoningText = reasoningText,
            Actions = new List<ParsedNarrativeAction> { action }
        };
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
        var skillMeans = actionModiMentis.Select(s => $"with {s.SkillMeans}").ToList();

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
        var chosenMeans = new List<string>();
        int actionIndex = 1;

        for (int i = 0; i < totalActions; i++)
        {
            var hardcodedOutcome = sampledOutcomes[i];
            string hardcodedOutcomeStr = hardcodedOutcome.Outcome.ToNaturalLanguageString();

            // Step 3a — approach selection
            string skillSelectionGbnf = JsonConstraintGenerator.GenerateGBNF(
                LLMSchemaConfig.CreateSkillSelectionSchema(skillMeans, hardcodedOutcomeStr));
            string skillSelectionPrompt = _promptConstructor.BuildSkillSelectionPrompt(
                hardcodedOutcomeStr, actionModiMentis, thinkingModusMentis, chosenMeans);

            string? skillSelectionJson = await RequestFromLLMAsync(
                slot, skillSelectionPrompt, skillSelectionGbnf, 300, cancellationToken);

            if (string.IsNullOrWhiteSpace(skillSelectionJson))
            {
                Console.Error.WriteLine($"ThinkingExecutor: Skill-selection {i + 1} returned empty response, skipping outcome.");
                continue;
            }

            var (skillReasoningText, selectedMeans) = ParseSkillSelectionResponse(skillSelectionJson);
            if (string.IsNullOrEmpty(selectedMeans))
            {
                Console.Error.WriteLine($"ThinkingExecutor: Skill-selection {i + 1} could not parse selected_approach, skipping outcome.");
                continue;
            }

            var selectedModusMentis = MapMeansToModusMentis(selectedMeans, actionModiMentis);
            string selectedSkillId = selectedModusMentis?.ModusMentisId ?? selectedMeans;

            skillReasoningTexts.Add(skillReasoningText);
            chosenMeans.Add(selectedMeans);
            Console.WriteLine($"ThinkingExecutor: Skill-selection {i + 1}/{totalActions}: '{selectedSkillId}' ({selectedMeans}) for '{hardcodedOutcomeStr}'");

            // Step 3b — action description
            string actionGbnf = JsonConstraintGenerator.GenerateGBNF(
                LLMSchemaConfig.CreateSingleActionSchema(hardcodedOutcomeStr, selectedMeans));
            string actionPrompt = _promptConstructor.BuildSingleActionPrompt(
                actionIndex, totalActions, thinkingModusMentis, hardcodedOutcomeStr, selectedMeans);

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
    /// Parses a single named text field from a JSON response. Returns "" on failure.
    /// </summary>
    private string ParseSingleTextField(string json, string fieldName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty(fieldName).GetString() ?? "");
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Failed to parse '{fieldName}' from JSON: {ex.Message}");
            return "";
        }
    }

    /// <summary>
    /// Parses the HOW call response. Returns (howText, selectedMeans) where selectedMeans is "with X".
    /// </summary>
    private (string HowText, string SelectedMeans) ParseHowResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string howText = root.GetProperty("how_could_i_do_it").GetString() ?? "";
            string means = root.GetProperty("selected_approach").GetString() ?? "";
            return (howText, means);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Failed to parse HOW response: {ex.Message}");
            return ("", "");
        }
    }

    /// <summary>
    /// Maps a "with X" means string back to the matching ModusMentis, or null if no match.
    /// </summary>
    private static ModusMentis? MapMeansToModusMentis(string means, List<ModusMentis> actionModiMentis)
        => actionModiMentis.FirstOrDefault(s => $"with {s.SkillMeans}" == means);

    /// <summary>
    /// Parses the reasoning-only call response.
    /// </summary>
    private string ParseReasoningResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return TextTruncationUtils.TrimToLastSentence(doc.RootElement.GetProperty("what_do_i_think").GetString() ?? "");
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
    /// <summary>
    /// Parses the skill-selection call response (step 3a).
    /// Returns (ReasoningText, SelectedMeans) where SelectedMeans is "with X"; returns ("", "") on failure.
    /// </summary>
    private (string SkillReasoningText, string SelectedMeans) ParseSkillSelectionResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string part1 = TextTruncationUtils.TrimToLastSentence(root.GetProperty("how_could_i_proceed").GetString() ?? "");
            string part2 = TextTruncationUtils.TrimToLastSentence(root.GetProperty("which_approach_and_why").GetString() ?? "");
            string skillReasoning = (part1 + " " + part2).Trim();
            string selectedMeans = root.GetProperty("selected_approach").GetString() ?? "";
            return (skillReasoning, selectedMeans);
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

            string reasoningText = root.GetProperty("what_do_i_think").GetString() ?? "";
            
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
