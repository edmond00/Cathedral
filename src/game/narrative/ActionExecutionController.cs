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

        // Step 1: Use Critic to verify action plausibility/coherency
        Console.WriteLine($"\n🔍 Evaluating action plausibility with Critic...");
        
        // Ask multiple questions about the action to ensure it makes sense
        var plausibilityQuestions = new[]
        {
            $"Given the context, does the action '{action.ActionText}' make logical sense?",
            $"Is '{action.ActionText}' a reasonable thing to attempt in this situation?",
            $"Would '{action.ActionText}' be physically or logically possible given the circumstances?"
        };
        
        double plausibilitySum = 0.0;
        foreach (var question in plausibilityQuestions)
        {
            double score = await _difficultyEvaluator.EvaluateCoherence(question);
            plausibilitySum += score;
            Console.WriteLine($"   Q: {question}");
            Console.WriteLine($"   A: {score:F3} ({(score > 0.7 ? "plausible" : score > 0.4 ? "uncertain" : "implausible")})");
        }
        
        double avgPlausibility = plausibilitySum / plausibilityQuestions.Length;
        Console.WriteLine($"   Average plausibility: {avgPlausibility:F3}");
        
        // If action is deemed implausible, fail immediately without skill check
        if (avgPlausibility < 0.3)
        {
            Console.WriteLine($"   ❌ Action rejected as implausible\n");
            return CreateFailureResult(action, thinkingSkillUsed,
                "That action doesn't make sense in this situation.");
        }
        
        Console.WriteLine($"   ✓ Action approved as plausible\n");

        // Step 2: Use Critic to evaluate difficulty (returns 0.0=easy to 1.0=hard)
        Console.WriteLine($"🎯 Evaluating action difficulty with Critic...");
        
        var difficultyQuestion = $"Is the action '{action.ActionText}' difficult to perform?";
        double difficultyScore = await _difficultyEvaluator.EvaluateCoherence(difficultyQuestion);
        
        Console.WriteLine($"   Difficulty score: {difficultyScore:F3} ({(difficultyScore < 0.3 ? "easy" : difficultyScore < 0.7 ? "moderate" : "hard")})");
        
        // Convert difficulty score to success probability
        // Easy (0.0) = 95% success, Moderate (0.5) = 70% success, Hard (1.0) = 40% success
        double successProbability = 0.95 - (difficultyScore * 0.55);
        
        // Adjust for skill level (body part value from 1-10)
        string bodyPartName = actionSkill.BodyParts.Length > 0 ? actionSkill.BodyParts[0].ToLower() : "hands";
        int bodyPartValue = _avatar.BodyPartLevels.TryGetValue(bodyPartName, out int bpValue) ? bpValue : 5;
        
        // Body part adds up to 10% success chance
        successProbability += (bodyPartValue - 5) * 0.02;
        successProbability = Math.Clamp(successProbability, 0.1, 0.95);
        
        // Roll for success
        var rng = new Random();
        double roll = rng.NextDouble();
        bool succeeded = roll < successProbability;
        
        Console.WriteLine($"   Success probability: {successProbability:F2} (body part '{bodyPartName}': {bodyPartValue})");
        Console.WriteLine($"   Roll: {roll:F3} → {(succeeded ? "✓ SUCCESS" : "✗ FAILURE")}\n");

        // Determine actual outcome
        OutcomeBase actualOutcome;
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
        await _outcomeApplicator.ApplyOutcomeAsync(actualOutcome, _avatar);

        // Generate narration from thinking skill's perspective
        string narration = await _outcomeNarrator.NarrateOutcomeAsync(
            action,
            actionSkill,
            thinkingSkillUsed,
            actualOutcome,
            succeeded,
            difficultyScore,
            _avatar,
            cancellationToken);

        return new ActionExecutionResult
        {
            Action = action,
            ActionSkill = actionSkill,
            ThinkingSkill = thinkingSkillUsed,
            Difficulty = difficultyScore,
            Succeeded = succeeded,
            ActualOutcome = actualOutcome,
            Narration = narration
        };
    }

    /// <summary>
    /// Determines what outcome occurs when an action fails.
    /// Uses CriticEvaluator to score predefined generic failures for coherence.
    /// </summary>
    private async Task<OutcomeBase> DetermineFailureOutcomeAsync(ParsedNarrativeAction action, NarrationNode currentNode)
    {
        // Predefined list of generic failure outcomes
        var genericFailures = new List<(HumorOutcome outcome, string description)>
        {
            (new HumorOutcome("Black Bile", 3, "frustration and self-criticism"), "frustration and self-criticism"),
            (new HumorOutcome("Yellow Bile", 2, "irritation and impatience"), "irritation and impatience"),
            (new HumorOutcome("Phlegm", 2, "resignation and acceptance"), "resignation and acceptance"),
            (new HumorOutcome("Melancholia", 1, "mild disappointment"), "mild disappointment"),
            (new HumorOutcome("Ether", 1, "momentary confusion"), "momentary confusion")
        };
        
        // Use CriticEvaluator to score each failure for coherence with action/context
        var failureScores = new Dictionary<HumorOutcome, double>();
        
        foreach (var (outcome, description) in genericFailures)
        {
            var question = $"In the context '{currentNode.GenerateNeutralDescription(_avatar.CurrentLocationId)}', if the action '{action.ActionText}' fails, is '{description}' a coherent emotional consequence?";
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
            ActualOutcome = new HumorOutcome("Melancholia", 1, "inability to act"),
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
    public double Difficulty { get; set; }
    public bool Succeeded { get; set; }
    public OutcomeBase ActualOutcome { get; set; } = null!;
    public string Narration { get; set; } = "";
}
