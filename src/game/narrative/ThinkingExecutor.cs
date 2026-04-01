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
    /// CoT pipeline: REFLECT+GOAL → WHY → (HOW → WHAT, or early exit if IGNORE).
    /// For ObservationObject targets the thinking modusMentis first reflects and picks a
    /// goal (including the "ignore and move on" option). If it chooses to ignore, only the
    /// WHY reasoning is returned and no action is generated. Otherwise the full
    /// HOW → WHAT pipeline runs and one action is returned.
    /// Returns null if any required LLM call fails.
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
        // Acquire and reset the thinking slot once at the start of the procedure.
        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        // ── Call 0: REFLECT + GOAL (always) ─────────────────────────────────────────
        // For ObservationObjects use their sub-outcomes; for plain outcomes wrap in a list.
        var (reflectTarget, subOutcomes) = targetOutcome is ObservationObject obs
            ? (obs as ConcreteOutcome, obs.SubOutcomes)
            : (targetOutcome, new List<ConcreteOutcome> { targetOutcome });

        var (goalOutcome, reflect) = await GenerateGoalAsync(
            thinkingSlot, reflectTarget, subOutcomes, node, thinkingModusMentis, protagonist, worldContext, cancellationToken);

        ConcreteOutcome resolvedOutcome = goalOutcome ?? subOutcomes[0];
        string reflectText = reflect;
        ObservationObject? sourceObs = targetOutcome as ObservationObject;

        string outcomeDescription = resolvedOutcome.ToNaturalLanguageString();
        var skillMeans = actionModiMentis.Select(s => $"with {s.SkillMeans}").ToList();

        // ── Call 1: WHY ────────────────────────────────────────────────────────────
        // For the ignore outcome use the source observation as the attention label so
        // the prompt reads naturally ("drawn to [observation]… want to ignore and move on").
        ConcreteOutcome whyTargetOutcome = (resolvedOutcome is IgnoreOutcome && sourceObs != null)
            ? sourceObs
            : resolvedOutcome;
        string whyPrompt = _promptConstructor.BuildWhyPrompt(outcomeDescription, node, thinkingModusMentis, protagonist, worldContext, whyTargetOutcome, keywordInContext);
        string whyGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhySchema());

        string? whyJson = await RequestFromLLMAsync(thinkingSlot, whyPrompt, whyGbnf, cancellationToken);
        if (string.IsNullOrWhiteSpace(whyJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: WHY call returned empty response.");
            return null;
        }

        string whyText = ParseSingleTextField(whyJson, "what_do_i_think");
        Console.WriteLine($"ThinkingExecutor: WHY complete ({whyText.Length} chars)");

        // ── Early exit: IGNORE ─────────────────────────────────────────────────────
        if (resolvedOutcome is IgnoreOutcome)
        {
            Console.WriteLine("ThinkingExecutor: IGNORE selected — skipping HOW/WHAT, returning reasoning only.");
            string ignoreReasoning = string.Join(" ", new[] { reflectText, whyText }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return new ThinkingResponse
            {
                ReasoningText = ignoreReasoning,
                Actions = new List<ParsedNarrativeAction>()
            };
        }

        // ── Call 2: HOW ────────────────────────────────────────────────────────────
        string howPrompt = _promptConstructor.BuildHowPrompt(outcomeDescription, actionModiMentis, thinkingModusMentis);
        string howGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateHowSchema(skillMeans));

        string? howJson = await RequestFromLLMAsync(thinkingSlot, howPrompt, howGbnf, cancellationToken);
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

        string? whatJson = await RequestFromLLMAsync(actionSlot, whatPrompt, whatGbnf, cancellationToken);
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
    /// Call 0a (REFLECT): full context, asks what the thinker makes of <paramref name="reflectTarget"/> → reasoning text.
    /// Call 0b (GOAL): short continuation, picks from <paramref name="subOutcomes"/> or "ignore and move on".
    /// Returns (chosen sub-outcome or IgnoreOutcome, reflect text). Outcome is null on LLM failure.
    /// </summary>
    private async Task<(ConcreteOutcome? Outcome, string ReflectText)> GenerateGoalAsync(
        int thinkingSlot,
        ConcreteOutcome reflectTarget,
        List<ConcreteOutcome> subOutcomes,
        NarrationNode node,
        ModusMentis thinkingModusMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken)
    {
        // ── Call 0a: REFLECT ───────────────────────────────────────────────────────
        string reflectPrompt = reflectTarget is ObservationObject obsTarget
            ? ThinkingPromptConstructor.BuildReflectPrompt(obsTarget, node, thinkingModusMentis, protagonist, worldContext)
            : ThinkingPromptConstructor.BuildReflectPrompt(reflectTarget, node, thinkingModusMentis, protagonist, worldContext);
        string reflectGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhySchema());

        string? reflectJson = await RequestFromLLMAsync(thinkingSlot, reflectPrompt, reflectGbnf, cancellationToken);
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
        // Always append "ignore and move on" so there are at least 2 choices.
        var goalOptions = subOutcomes
            .Select(o => o.ToNaturalLanguageString())
            .Append(IgnoreOutcome.GoalString)
            .ToList();
        string goalPrompt = ThinkingPromptConstructor.BuildGoalPrompt(goalOptions, thinkingModusMentis);
        string goalGbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateGoalSchema(goalOptions));

        string? goalJson = await RequestFromLLMAsync(thinkingSlot, goalPrompt, goalGbnf, cancellationToken);
        if (string.IsNullOrWhiteSpace(goalJson))
        {
            Console.Error.WriteLine("ThinkingExecutor: GOAL call returned empty response.");
            return (null, reflectText);
        }

        try
        {
            using var doc = JsonDocument.Parse(goalJson);
            string chosen = doc.RootElement.GetProperty("goal").GetString() ?? "";
            Console.WriteLine($"ThinkingExecutor: GOAL selected '{chosen}'");
            if (chosen.Equals(IgnoreOutcome.GoalString, StringComparison.OrdinalIgnoreCase))
                return (IgnoreOutcome.Instance, reflectText);
            var match = subOutcomes.FirstOrDefault(o =>
                o.ToNaturalLanguageString().Equals(chosen, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Asks the action modusMentis to reformulate an existing action text to incorporate a combined item.
    /// Uses the WHAT-style prompt. Returns the reformulated display text, or null on LLM failure.
    /// </summary>
    public async Task<string?> ExecuteItemReformulationAsync(
        ParsedNarrativeAction originalAction,
        Item item,
        NarrationNode node,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken = default)
    {
        var actionModusMentis = originalAction.ActionModusMentis;
        if (actionModusMentis == null)
        {
            Console.Error.WriteLine("ThinkingExecutor: Item reformulation skipped — action has no resolved modusMentis.");
            return null;
        }

        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(actionModusMentis);
        _llmManager.ResetInstance(actionSlot);

        string prompt = _promptConstructor.BuildItemReformulationPrompt(
            originalAction.ActionText, item, actionModusMentis, node, protagonist, worldContext);
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateWhatSchema());

        string? jsonResponse = await RequestFromLLMAsync(actionSlot, prompt, gbnf, cancellationToken);
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            Console.Error.WriteLine("ThinkingExecutor: Item reformulation LLM call returned empty response.");
            return null;
        }

        string reformulated = ParseSingleTextField(jsonResponse, "what_should_i_do");
        if (string.IsNullOrWhiteSpace(reformulated))
            return null;

        // Strip "try to " prefix like the WHAT pipeline does
        return reformulated.StartsWith("try to ", StringComparison.OrdinalIgnoreCase)
            ? reformulated.Substring(7)
            : reformulated;
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
