using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the thinking phase: skill popup, LLM reasoning generation, action parsing.
/// Manages thinking attempts and generates fallback actions if LLM fails.
/// </summary>
public class ThinkingPhaseController
{
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly ThinkingSkillPopup _skillPopup;
    private readonly Avatar _avatar;

    public ThinkingPhaseController(
        ThinkingExecutor thinkingExecutor,
        Avatar avatar)
    {
        _thinkingExecutor = thinkingExecutor;
        _avatar = avatar;
        
        // Initialize popup with avatar's thinking skills
        var thinkingSkills = avatar.GetThinkingSkills();
        _skillPopup = new ThinkingSkillPopup(thinkingSkills);
    }

    /// <summary>
    /// Shows the thinking skill popup at the mouse position.
    /// </summary>
    public void ShowSkillPopup(int mouseX, int mouseY)
    {
        _skillPopup.Show(mouseX, mouseY);
    }

    /// <summary>
    /// Handles input for the skill popup.
    /// Returns the selected skill or null if cancelled/no input.
    /// </summary>
    public Skill? HandlePopupInput(ConsoleKeyInfo keyInfo)
    {
        return _skillPopup.HandleInput(keyInfo);
    }

    /// <summary>
    /// Renders the skill popup if visible.
    /// </summary>
    public void RenderPopup(Action<int, int, string, ConsoleColor> writeAt)
    {
        _skillPopup.Render(writeAt);
    }

    /// <summary>
    /// Executes the thinking phase with the selected skill.
    /// Generates CoT reasoning and actions, or fallback if LLM fails.
    /// </summary>
    public async Task<ThinkingPhaseResult> ExecuteThinkingPhaseAsync(
        Skill selectedThinkingSkill,
        string keyword,
        NarrationNode node,
        NarrationState state,
        CancellationToken cancellationToken = default)
    {
        // Get possible outcomes for this keyword
        var possibleOutcomes = node.GetOutcomesForKeyword(keyword);
        
        // Always add FeelGoodOutcome as a fallback option
        var feelGoodOutcome = new FeelGoodOutcome();
        if (!possibleOutcomes.Any(o => o is FeelGoodOutcome))
        {
            possibleOutcomes.Add(feelGoodOutcome);
        }

        // Get action skills
        var actionSkills = _avatar.GetActionSkills();

        // Try to generate from LLM
        var response = await _thinkingExecutor.GenerateThinkingAsync(
            selectedThinkingSkill,
            keyword,
            node,
            possibleOutcomes,
            actionSkills,
            _avatar,
            cancellationToken);

        if (response != null && response.Actions.Count > 0)
        {
            return new ThinkingPhaseResult
            {
                Success = true,
                ReasoningText = response.ReasoningText,
                Actions = response.Actions,
                ThinkingSkillUsed = selectedThinkingSkill
            };
        }

        // Fallback: generate basic actions
        var fallbackActions = GenerateFallbackActions(
            keyword,
            possibleOutcomes,
            actionSkills);

        return new ThinkingPhaseResult
        {
            Success = false,
            ReasoningText = GenerateFallbackReasoning(selectedThinkingSkill, keyword),
            Actions = fallbackActions,
            ThinkingSkillUsed = selectedThinkingSkill
        };
    }

    /// <summary>
    /// Generates fallback actions when LLM fails.
    /// Creates 2-3 simple actions matching outcomes with random action skills.
    /// </summary>
    private List<ParsedNarrativeAction> GenerateFallbackActions(
        string keyword,
        List<OutcomeBase> possibleOutcomes,
        List<Skill> actionSkills)
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
            var actionSkill = actionSkills[random.Next(actionSkills.Count)];

            // Generate simple action text based on outcome type
            string actionText = outcome switch
            {
                Item item => $"try to obtain {item.DisplayName} from the {keyword}",
                NarrationNode node => $"try to explore the {keyword} to discover {node.NodeId}",
                FeelGoodOutcome => $"try to appreciate the {keyword}",
                HumorOutcome => $"try to contemplate the {keyword}",
                _ => $"try to interact with the {keyword}"
            };

            actions.Add(new ParsedNarrativeAction
            {
                ActionSkillId = actionSkill.SkillId,
                PreselectedOutcome = outcome,
                ActionText = actionText
            });
        }

        return actions;
    }

    /// <summary>
    /// Generates fallback reasoning when LLM fails.
    /// </summary>
    private string GenerateFallbackReasoning(Skill thinkingSkill, string keyword)
    {
        return $"{thinkingSkill.DisplayName} considers the '{keyword}' carefully but struggles to formulate a coherent plan.";
    }

    public bool IsPopupVisible => _skillPopup.IsVisible;
}

/// <summary>
/// Result from executing the thinking phase.
/// </summary>
public class ThinkingPhaseResult
{
    public bool Success { get; set; }
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
    public Skill? ThinkingSkillUsed { get; set; }
}
