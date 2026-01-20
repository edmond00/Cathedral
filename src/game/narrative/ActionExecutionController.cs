using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates action execution: skill checks, outcome determination, and narration.
/// Uses tree-based Critic evaluation for action plausibility checks.
/// </summary>
public class ActionExecutionController
{
    private readonly ActionScorer _actionScorer;
    private readonly ActionDifficultyEvaluator _difficultyEvaluator;
    private readonly OutcomeNarrator _outcomeNarrator;
    private readonly OutcomeApplicator _outcomeApplicator;
    private readonly Avatar _avatar;
    private readonly CriticEvaluator _criticEvaluator;

    public ActionExecutionController(
        ActionScorer actionScorer,
        ActionDifficultyEvaluator difficultyEvaluator,
        OutcomeNarrator outcomeNarrator,
        OutcomeApplicator outcomeApplicator,
        Avatar avatar,
        CriticEvaluator criticEvaluator)
    {
        _actionScorer = actionScorer;
        _difficultyEvaluator = difficultyEvaluator;
        _outcomeNarrator = outcomeNarrator;
        _outcomeApplicator = outcomeApplicator;
        _avatar = avatar;
        _criticEvaluator = criticEvaluator;
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

        // Step 1: Use Critic tree evaluation to verify action plausibility/coherency
        Console.WriteLine($"\n🔍 Evaluating action plausibility with Critic Tree...");
        
        // Build a tree of plausibility checks - all must pass for action to be valid
        var plausibilityTree = BuildPlausibilityTree(action.ActionText);
        var plausibilityResult = await _criticEvaluator.EvaluateTreeAsync(plausibilityTree);
        
        // If any plausibility check failed, reject the action
        if (!plausibilityResult.OverallSuccess)
        {
            Console.WriteLine($"   ❌ Action rejected: {plausibilityResult.FinalErrorMessage}\n");
            return CreateFailureResult(action, thinkingSkillUsed,
                plausibilityResult.FinalErrorMessage.Length > 0 
                    ? plausibilityResult.FinalErrorMessage 
                    : "That action doesn't make sense in this situation.");
        }
        
        Console.WriteLine($"   ✓ Action approved as plausible ({plausibilityResult.Trace.Count} checks passed)\n");

        // Step 2: Use Critic tree to evaluate difficulty
        Console.WriteLine($"🎯 Evaluating action difficulty with Critic Tree...");
        
        var difficultyTree = BuildDifficultyTree(action.ActionText);
        var difficultyResult = await _criticEvaluator.EvaluateTreeAsync(difficultyTree);
        
        // Calculate difficulty score from tree result
        // If "difficult" checks pass, action is harder
        double difficultyScore = CalculateDifficultyFromTree(difficultyResult);
        
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
    /// Builds a tree of plausibility checks for an action.
    /// All checks must pass for the action to be considered valid.
    /// </summary>
    private CriticNode BuildPlausibilityTree(string actionText)
    {
        // Chain of plausibility checks - all must pass
        var logicalSense = new CriticNode(
            name: "LogicalSense",
            question: $"Given the context, does the action '{actionText}' make logical sense?",
            threshold: 0.5,
            errorMessage: "The action doesn't make logical sense in this context"
        );
        
        var reasonable = new CriticNode(
            name: "Reasonable",
            question: $"Is '{actionText}' a reasonable thing to attempt in this situation?",
            threshold: 0.5,
            errorMessage: "The action is not reasonable to attempt"
        );
        
        var possible = new CriticNode(
            name: "Possible",
            question: $"Would '{actionText}' be physically or logically possible given the circumstances?",
            threshold: 0.5,
            errorMessage: "The action is not possible in these circumstances"
        );
        
        // Chain them together
        logicalSense.SuccessBranch = reasonable;
        reasonable.SuccessBranch = possible;
        
        return logicalSense;
    }
    
    /// <summary>
    /// Builds a tree to evaluate action difficulty.
    /// </summary>
    private CriticNode BuildDifficultyTree(string actionText)
    {
        // Difficulty checks - we want to know if it's difficult
        var isDifficult = new CriticNode(
            name: "IsDifficult",
            question: $"Is the action '{actionText}' difficult to perform?",
            threshold: 0.5,
            errorMessage: ""
        );
        
        var requiresExpertise = new CriticNode(
            name: "RequiresExpertise",
            question: $"Does '{actionText}' require special expertise or training?",
            threshold: 0.5,
            errorMessage: ""
        );
        
        var hasRisks = new CriticNode(
            name: "HasRisks",
            question: $"Does attempting '{actionText}' carry significant risks?",
            threshold: 0.5,
            errorMessage: ""
        );
        
        // For difficulty, we evaluate all regardless of pass/fail
        // Use success branches to continue evaluation
        isDifficult.SuccessBranch = requiresExpertise;
        isDifficult.FailureBranch = requiresExpertise; // Continue even if "not difficult"
        requiresExpertise.SuccessBranch = hasRisks;
        requiresExpertise.FailureBranch = hasRisks;
        
        return isDifficult;
    }
    
    /// <summary>
    /// Calculates a difficulty score from the difficulty tree result.
    /// Higher score = more difficult (0.0 to 1.0).
    /// </summary>
    private double CalculateDifficultyFromTree(CriticTreeResult result)
    {
        if (result.Trace.Count == 0)
            return 0.5;
        
        // Average the scores of all difficulty-related checks
        // Higher scores mean "yes it's difficult"
        double totalScore = 0;
        foreach (var node in result.Trace)
        {
            totalScore += node.Score;
        }
        
        return totalScore / result.Trace.Count;
    }

    /// <summary>
    /// Determines what outcome occurs when an action fails.
    /// Uses tree-based Critic evaluation to score failure outcomes.
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
        
        // Build a tree to find the most coherent failure outcome
        var context = currentNode.GenerateNeutralDescription(_avatar.CurrentLocationId);
        HumorOutcome? bestOutcome = null;
        double bestScore = 0;
        
        foreach (var (outcome, description) in genericFailures)
        {
            var coherenceNode = new CriticNode(
                name: $"FailureCoherence_{outcome.HumorName}",
                question: $"In the context '{context}', if the action '{action.ActionText}' fails, is '{description}' a coherent emotional consequence?",
                threshold: 0.5,
                errorMessage: ""
            );
            
            var result = await _criticEvaluator.EvaluateTreeAsync(coherenceNode);
            
            if (result.Trace.Count > 0)
            {
                var score = result.Trace[0].Score;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestOutcome = outcome;
                }
            }
        }
        
        return bestOutcome ?? genericFailures[0].outcome;
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
