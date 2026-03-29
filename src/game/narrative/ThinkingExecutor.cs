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
        KeywordInContext? keywordInContext,
        NarrationNode node,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken = default)
    {
        // ── Call 0: REFLECT + GOAL (only when targeting a multi-outcome ObservationObject) ──
        ConcreteOutcome resolvedOutcome = targetOutcome;
        string reflectText = "";
        if (targetOutcome is ObservationObject obs)
        {
            if (obs.SubOutcomes.Count == 1)
            {
                resolvedOutcome = obs.SubOutcomes[0];
            }
            else if (obs.SubOutcomes.Count > 1)
            {
                var (goalOutcome, reflect) = await GenerateGoalAsync(
                    obs, node, thinkingModusMentis, protagonist, worldContext, cancellationToken);
                resolvedOutcome = goalOutcome ?? obs.SubOutcomes[0];
                reflectText = reflect;
            }
        }

        string outcomeDescription = resolvedOutcome.ToNaturalLanguageString();
        var skillMeans = actionModiMentis.Select(s => $"with {s.SkillMeans}").ToList();

        // Get thinking slot — reset context for a fresh batch
        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        // ── Call 1: WHY ────────────────────────────────────────────────────────────
        string whyPrompt = _promptConstructor.BuildWhyPrompt(outcomeDescription, node, thinkingModusMentis, protagonist, worldContext, resolvedOutcome, keywordInContext);
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
            Console.Error.WriteLine("ThinkingExecutor: HOW call could not parse 'how' field.");
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

        string whatPrompt = _promptConstructor.BuildWhatPrompt(keyword, keywordInContext, outcomeDescription, node, protagonist, selectedModusMentis, worldContext, resolvedOutcome);
        string whatGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhatSchema());

        string? whatJson = await RequestFromLLMAsync(actionSlot, whatPrompt, whatGbnf, 250, cancellationToken);
        if (string.IsNullOrWhiteSpace(whatJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: WHAT call returned empty response.");
            return null;
        }

        string actionDescription = ParseSingleTextField(whatJson, "what_should_i_do");
        string displayText = actionDescription.StartsWith("try to ", StringComparison.OrdinalIgnoreCase)
            ? actionDescription.Substring(7)
            : actionDescription;

        Console.WriteLine($"ThinkingExecutor: WHAT complete — action: '{displayText}'");

        // Combine REFLECT (if any) + WHY + HOW reasoning into one block
        string reasoningText = string.Join(" ", new[] { reflectText, whyText, howText }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var action = new ParsedNarrativeAction
        {
            ActionModusMentisId = selectedSkillId,
            ActionModusMentis = selectedModusMentis,
            PreselectedOutcome = resolvedOutcome,
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
    /// REFLECT + GOAL batch: two calls in the same slot.
    /// Call 0a (REFLECT): full context, asks what the thinker makes of the observation → reasoning text.
    /// Call 0b (GOAL): short continuation, picks which sub-outcome to pursue.
    /// Returns (chosen sub-outcome, reflect reasoning text). Outcome is null on failure.
    /// </summary>
    private async Task<(ConcreteOutcome? Outcome, string ReflectText)> GenerateGoalAsync(
        ObservationObject obs,
        NarrationNode node,
        ModusMentis thinkingModusMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken)
    {
        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        // ── Call 0a: REFLECT ───────────────────────────────────────────────────────
        string reflectPrompt = ThinkingPromptConstructor.BuildReflectPrompt(obs, node, thinkingModusMentis, protagonist, worldContext);
        string reflectGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhySchema());

        string? reflectJson = await RequestFromLLMAsync(thinkingSlot, reflectPrompt, reflectGbnf, 350, cancellationToken);
        string reflectText = "";
        if (!string.IsNullOrWhiteSpace(reflectJson))
        {
            reflectText = ParseSingleTextField(reflectJson, "what_do_i_think");
            Console.WriteLine($"ThinkingExecutor: REFLECT complete ({reflectText.Length} chars)");
        }
        else
        {
            Console.Error.WriteLine("ThinkingExecutor: REFLECT call returned empty response — continuing to GOAL.");
        }

        // ── Call 0b: GOAL ──────────────────────────────────────────────────────────
        var goalOptions = obs.SubOutcomes.Select(o => o.ToNaturalLanguageString()).ToList();
        string goalPrompt = ThinkingPromptConstructor.BuildGoalPrompt(obs.SubOutcomes, thinkingModusMentis);
        string goalGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateGoalSchema(goalOptions));

        string? goalJson = await RequestFromLLMAsync(thinkingSlot, goalPrompt, goalGbnf, 100, cancellationToken);
        if (string.IsNullOrWhiteSpace(goalJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: GOAL call returned empty response.");
            return (null, reflectText);
        }

        try
        {
            using var doc = JsonDocument.Parse(goalJson);
            string chosen = doc.RootElement.GetProperty("goal").GetString() ?? "";
            var match = obs.SubOutcomes.FirstOrDefault(o =>
                o.ToNaturalLanguageString().Equals(chosen, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"ThinkingExecutor: GOAL selected '{chosen}'");
            return (match, reflectText);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ThinkingExecutor: Failed to parse GOAL response: {ex.Message}");
            return (null, reflectText);
        }
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
            string means = root.GetProperty("how").GetString() ?? "";
            string howText = root.GetProperty("why").GetString() ?? "";
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

}

/// <summary>
/// Represents the response from a thinking modusMentis LLM request.
/// </summary>
public class ThinkingResponse
{
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
}
