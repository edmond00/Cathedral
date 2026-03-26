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
        string whyPrompt = _promptConstructor.BuildWhyPrompt(outcomeDescription, node, thinkingModusMentis, protagonist, worldContext, targetOutcome);
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

}

/// <summary>
/// Represents the response from a thinking modusMentis LLM request.
/// </summary>
public class ThinkingResponse
{
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
}
