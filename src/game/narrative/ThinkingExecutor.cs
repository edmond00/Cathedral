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
    
    // Thinking skills use slots 10-29
    private const int THINKING_SLOT_START = 10;
    private const int THINKING_SLOT_END = 29;
    private readonly Dictionary<string, int> _skillSlots = new();

    public ThinkingExecutor(
        LlamaServerManager llmManager,
        ThinkingPromptConstructor promptConstructor)
    {
        _llmManager = llmManager;
        _promptConstructor = promptConstructor;
    }

    /// <summary>
    /// Gets or creates a slot for the given thinking skill.
    /// Caches the persona prompt in the slot for reuse.
    /// </summary>
    private async Task<int> GetOrCreateSlotForSkillAsync(Skill skill)
    {
        if (_skillSlots.TryGetValue(skill.SkillId, out int existingSlot))
        {
            return existingSlot;
        }

        // Find next available slot
        int nextSlot = THINKING_SLOT_START;
        while (nextSlot <= THINKING_SLOT_END && _skillSlots.ContainsValue(nextSlot))
        {
            nextSlot++;
        }

        if (nextSlot > THINKING_SLOT_END)
        {
            // Out of slots, evict the least recently used
            // For now, just use the first slot
            nextSlot = THINKING_SLOT_START;
            var toRemove = _skillSlots.FirstOrDefault(kvp => kvp.Value == nextSlot);
            if (toRemove.Key != null)
            {
                _skillSlots.Remove(toRemove.Key);
            }
        }

        _skillSlots[skill.SkillId] = nextSlot;

        // Note: Can't pre-assign slots. CreateInstanceAsync returns auto-incremented slot.
        // For now, create instance and track it.
        int actualSlot = await _llmManager.CreateInstanceAsync(skill.PersonaPrompt);
        _skillSlots[skill.SkillId] = actualSlot;

        return actualSlot;
    }

    /// <summary>
    /// Generates CoT reasoning and actions for the given keyword.
    /// Returns the parsed thinking response or null if generation failed.
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        Skill thinkingSkill,
        string keyword,
        NarrationNode node,
        List<Outcome> possibleOutcomes,
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
            avatar);

        // Build JSON schema
        var schema = BuildThinkingJsonSchema(
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
        return ParseThinkingResponse(jsonResponse, possibleOutcomes);
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

        return await tcs.Task;
    }

    /// <summary>
    /// Builds the JSON schema for thinking responses.
    /// Includes constraints for valid action skills and outcomes.
    /// </summary>
    private CompositeField BuildThinkingJsonSchema(List<string> validActionSkills, List<string> validOutcomes)
    {
        return new CompositeField("ThinkingResponse",
            new StringField("reasoning_text", MinLength: 100, MaxLength: 400),
            new ArrayField("actions",
                ElementType: new CompositeField("Action",
                    new ChoiceField<string>("action_skill", validActionSkills.ToArray()),
                    new ChoiceField<string>("outcome", validOutcomes.ToArray()),
                    new StringField("action_description", MinLength: 30, MaxLength: 160)
                ),
                MinLength: 2,
                MaxLength: 5
            )
        );
    }

    /// <summary>
    /// Parses the LLM JSON response into a ThinkingResponse.
    /// Returns null if parsing fails.
    /// </summary>
    private ThinkingResponse? ParseThinkingResponse(string jsonResponse, List<Outcome> possibleOutcomes)
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

                // Parse outcome from natural language string
                try
                {
                    var outcome = Outcome.FromNaturalLanguageString(outcomeStr, possibleOutcomes);
                    
                    actions.Add(new ParsedNarrativeAction
                    {
                        ActionSkillId = actionSkill,
                        PreselectedOutcome = outcome,
                        ActionText = actionDesc
                    });
                }
                catch (InvalidOperationException)
                {
                    // Couldn't parse this outcome, skip it
                    continue;
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
