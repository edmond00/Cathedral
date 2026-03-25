using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the thinking phase: modusMentis popup, LLM reasoning generation, action parsing.
/// Manages thinking attempts and generates fallback actions if LLM fails.
/// </summary>
public class ThinkingPhaseController
{
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly ThinkingModusMentisPopup _modusMentisPopup;
    private readonly Protagonist _protagonist;

    public ThinkingPhaseController(
        ThinkingExecutor thinkingExecutor,
        Protagonist protagonist)
    {
        _thinkingExecutor = thinkingExecutor;
        _protagonist = protagonist;
        
        // Initialize popup with protagonist's thinking modiMentis
        var thinkingModiMentis = protagonist.GetThinkingModiMentis();
        _modusMentisPopup = new ThinkingModusMentisPopup(thinkingModiMentis);
    }

    /// <summary>
    /// Shows the thinking modusMentis popup at the mouse position.
    /// </summary>
    public void ShowModusMentisPopup(int mouseX, int mouseY)
    {
        _modusMentisPopup.Show(mouseX, mouseY);
    }

    /// <summary>
    /// Handles input for the modusMentis popup.
    /// Returns the selected modusMentis or null if cancelled/no input.
    /// </summary>
    public ModusMentis? HandlePopupInput(ConsoleKeyInfo keyInfo)
    {
        return _modusMentisPopup.HandleInput(keyInfo);
    }

    /// <summary>
    /// Renders the modusMentis popup if visible.
    /// </summary>
    public void RenderPopup(Action<int, int, string, ConsoleColor> writeAt)
    {
        _modusMentisPopup.Render(writeAt);
    }

    /// <summary>
    /// Executes the thinking phase with the selected modusMentis.
    /// Generates CoT reasoning and actions, or fallback if LLM fails.
    /// </summary>
    public async Task<ThinkingPhaseResult> ExecuteThinkingPhaseAsync(
        ModusMentis selectedThinkingModusMentis,
        string keyword,
        NarrationNode node,
        NarrationState state,
        CancellationToken cancellationToken = default)
    {
        // Get possible outcomes for this keyword
        var possibleOutcomes = node.GetOutcomesForKeyword(keyword);
        
        // Get action modiMentis
        var actionModiMentis = _protagonist.GetActionModiMentis();
        
        // Get the outcome that owns this keyword for context
        var keywordSourceOutcome = node.GetOutcomeOwningKeyword(keyword);
        string? keywordSourceOutcomeName = keywordSourceOutcome?.DisplayName;

        // Try to generate from LLM
        var response = await _thinkingExecutor.GenerateThinkingAsync(
            selectedThinkingModusMentis,
            keyword,
            keywordSourceOutcomeName,
            node,
            possibleOutcomes,
            actionModiMentis,
            _protagonist,
            cancellationToken);

        if (response != null && response.Actions.Count > 0)
        {
            return new ThinkingPhaseResult
            {
                Success = true,
                ReasoningText = response.ReasoningText,
                Actions = response.Actions,
                ThinkingModusMentisUsed = selectedThinkingModusMentis
            };
        }

        // Fallback: generate basic actions
        var fallbackActions = GenerateFallbackActions(
            keyword,
            possibleOutcomes,
            actionModiMentis);

        return new ThinkingPhaseResult
        {
            Success = false,
            ReasoningText = GenerateFallbackReasoning(selectedThinkingModusMentis, keyword),
            Actions = fallbackActions,
            ThinkingModusMentisUsed = selectedThinkingModusMentis
        };
    }

    /// <summary>
    /// Generates fallback actions when LLM fails.
    /// Creates 2-3 simple actions matching outcomes with random action modiMentis.
    /// </summary>
    private List<ParsedNarrativeAction> GenerateFallbackActions(
        string keyword,
        List<OutcomeBase> possibleOutcomes,
        List<ModusMentis> actionModiMentis)
    {
        var random = new Random();
        var actions = new List<ParsedNarrativeAction>();

        // Generate 2-3 actions
        int actionCount = Math.Min(random.Next(2, 4), possibleOutcomes.Count);
        
        // Shuffle outcomes to get variety
        var shuffledOutcomes = possibleOutcomes.OrderBy(_ => random.Next()).ToList();

        for (int i = 0; i < actionCount; i++)
        {
            var outcome = shuffledOutcomes[i];
            var actionModusMentis = actionModiMentis[random.Next(actionModiMentis.Count)];

            // Generate simple action text based on outcome type
            string actionText = outcome switch
            {
                Item item => $"try to obtain {item.DisplayName} from the {keyword}",
                NarrationNode node => $"try to explore the {keyword} to discover {node.NodeId}",
HumorOutcome => $"try to contemplate the {keyword}",
                _ => $"try to interact with the {keyword}"
            };

            actions.Add(new ParsedNarrativeAction
            {
                ActionModusMentisId = actionModusMentis.ModusMentisId,
                ActionModusMentis = actionModusMentis, // Set the actual modusMentis instance with proper level
                PreselectedOutcome = outcome,
                ActionText = actionText
            });
        }

        return actions;
    }

    /// <summary>
    /// Generates fallback reasoning when LLM fails.
    /// </summary>
    private string GenerateFallbackReasoning(ModusMentis thinkingModusMentis, string keyword)
    {
        return $"{thinkingModusMentis.DisplayName} considers the '{keyword}' carefully but struggles to formulate a coherent plan.";
    }

    public bool IsPopupVisible => _modusMentisPopup.IsVisible;
}

/// <summary>
/// Result from executing the thinking phase.
/// </summary>
public class ThinkingPhaseResult
{
    public bool Success { get; set; }
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
    public ModusMentis? ThinkingModusMentisUsed { get; set; }
}
