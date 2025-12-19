using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates action execution: skill checks, outcome determination, and narration.
/// Integrates with existing ActionScorer and ActionDifficultyEvaluator.
/// </summary>
public class ActionExecutionController
{
    private readonly ActionScorer _actionScorer;
    private readonly ActionDifficultyEvaluator _difficultyEvaluator;
    private readonly OutcomeNarrator _outcomeNarrator;
    private readonly OutcomeApplicator _outcomeApplicator;
    private readonly Avatar _avatar;

    public ActionExecutionController(
        ActionScorer actionScorer,
        ActionDifficultyEvaluator difficultyEvaluator,
        OutcomeNarrator outcomeNarrator,
        OutcomeApplicator outcomeApplicator,
        Avatar avatar)
    {
        _actionScorer = actionScorer;
        _difficultyEvaluator = difficultyEvaluator;
        _outcomeNarrator = outcomeNarrator;
        _outcomeApplicator = outcomeApplicator;
        _avatar = avatar;
    }

    /// <summary>
    /// Executes a player-selected action with skill check and outcome application.
    /// Returns the execution result with narration and final outcome.
    /// </summary>
    public async Task<ActionExecutionResult> ExecuteActionAsync(
        ParsedNarrativeAction action,
        NarrationNode currentNode,
        Skill thinkingSkillUsed,
        CancellationToken cancellationToken = default)
    {
        // Debug: Show what we're searching for and what we have
        Console.WriteLine($"DEBUG: Looking for action skill ID: '{action.ActionSkillId}'");
        Console.WriteLine($"DEBUG: Avatar has {_avatar.Skills.Count} skills:");
        foreach (var skill in _avatar.Skills)
        {
            Console.WriteLine($"  - {skill.SkillId} ({skill.DisplayName})");
        }
        
        // Resolve action skill
        var actionSkill = _avatar.Skills.FirstOrDefault(s => s.SkillId == action.ActionSkillId);
        if (actionSkill == null)
        {
            Console.WriteLine($"DEBUG: Skill '{action.ActionSkillId}' NOT FOUND in avatar's skills!");
            return CreateFailureResult(action, thinkingSkillUsed, 
                "The skill required for this action is unavailable.");
        }

        // Calculate difficulty based on avatar state and context
        int difficulty = _difficultyEvaluator.CalculateDifficulty(
            action.ActionText,
            actionSkill,
            _avatar);

        // Perform skill check
        bool succeeded = _actionScorer.RollSkillCheck(
            actionSkill,
            difficulty,
            _avatar);

        // Determine actual outcome
        Outcome actualOutcome;
        if (succeeded)
        {
            actualOutcome = action.PreselectedOutcome;
        }
        else
        {
            // Generate failure outcome (could be partial success or failure)
            actualOutcome = await DetermineFailureOutcomeAsync(action, currentNode);
        }

        // Apply outcome to game state
        _outcomeApplicator.ApplyOutcome(actualOutcome, _avatar);

        // Generate narration from thinking skill's perspective
        string narration = await _outcomeNarrator.NarrateOutcomeAsync(
            action,
            actionSkill,
            thinkingSkillUsed,
            actualOutcome,
            succeeded,
            difficulty,
            _avatar,
            cancellationToken);

        return new ActionExecutionResult
        {
            Action = action,
            ActionSkill = actionSkill,
            ThinkingSkill = thinkingSkillUsed,
            Difficulty = difficulty,
            Succeeded = succeeded,
            ActualOutcome = actualOutcome,
            Narration = narration
        };
    }

    /// <summary>
    /// Determines what outcome occurs when an action fails.
    /// Uses CriticEvaluator to score predefined generic failures for coherence.
    /// </summary>
    private async Task<Outcome> DetermineFailureOutcomeAsync(ParsedNarrativeAction action, NarrationNode currentNode)
    {
        // Predefined list of generic failure outcomes
        var genericFailures = new List<(Outcome outcome, string description)>
        {
            (new Outcome(OutcomeType.Humor, "", new Dictionary<string, int> { ["Black Bile"] = 3 }), "frustration and self-criticism"),
            (new Outcome(OutcomeType.Humor, "", new Dictionary<string, int> { ["Yellow Bile"] = 2 }), "irritation and impatience"),
            (new Outcome(OutcomeType.Humor, "", new Dictionary<string, int> { ["Phlegm"] = 2 }), "resignation and acceptance"),
            (new Outcome(OutcomeType.Humor, "", new Dictionary<string, int> { ["Melancholia"] = 1 }), "mild disappointment"),
            (new Outcome(OutcomeType.Humor, "", new Dictionary<string, int> { ["Ether"] = 1 }), "momentary confusion")
        };
        
        // Use CriticEvaluator to score each failure for coherence with action/context
        var failureScores = new Dictionary<Outcome, double>();
        
        foreach (var (outcome, description) in genericFailures)
        {
            var question = $"In the context '{currentNode.NeutralDescription}', if the action '{action.ActionText}' fails, is '{description}' a coherent emotional consequence?";
            var coherenceScore = await _difficultyEvaluator.EvaluateCoherence(question);
            failureScores[outcome] = coherenceScore;
        }
        
        // Return failure with highest coherence score
        return failureScores.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    /// <summary>
    /// Creates a failure result when the action cannot be executed.
    /// </summary>
    private ActionExecutionResult CreateFailureResult(
        ParsedNarrativeAction action,
        Skill thinkingSkill,
        string reason)
    {
        return new ActionExecutionResult
        {
            Action = action,
            ActionSkill = null,
            ThinkingSkill = thinkingSkill,
            Difficulty = 0,
            Succeeded = false,
            ActualOutcome = new Outcome(OutcomeType.Humor, "", null),
            Narration = reason
        };
    }
}

/// <summary>
/// Result of executing a narrative action.
/// </summary>
public class ActionExecutionResult
{
    public ParsedNarrativeAction Action { get; set; } = null!;
    public Skill? ActionSkill { get; set; }
    public Skill ThinkingSkill { get; set; } = null!;
    public int Difficulty { get; set; }
    public bool Succeeded { get; set; }
    public Outcome ActualOutcome { get; set; } = null!;
    public string Narration { get; set; } = "";
}
