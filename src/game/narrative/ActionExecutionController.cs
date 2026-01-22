using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents the current phase of action execution.
/// </summary>
public enum ActionExecutionPhase
{
    /// <summary>Evaluating if action is possible (plausibility + difficulty checks)</summary>
    EvaluatingAction,
    
    /// <summary>Rolling dice to determine success/failure</summary>
    RollingDice,
    
    /// <summary>Determining failure outcome and generating narration</summary>
    GeneratingOutcome,
    
    /// <summary>Execution complete</summary>
    Complete
}

/// <summary>
/// Intermediate result after plausibility and difficulty checks.
/// Used to transition from evaluation phase to dice rolling phase.
/// </summary>
public class ActionEvaluationResult
{
    public bool IsPlausible { get; set; }
    public string? PlausibilityError { get; set; }
    public double DifficultyScore { get; set; }
    public int DifficultyLevel { get; set; }
    public double SuccessProbability { get; set; }
    public Skill ActionSkill { get; set; } = null!;
    public Skill ThinkingSkill { get; set; } = null!;
    public ParsedNarrativeAction Action { get; set; } = null!;
    public NarrationNode CurrentNode { get; set; } = null!;
}

/// <summary>
/// Orchestrates action execution: skill checks, outcome determination, and narration.
/// Uses tree-based Critic evaluation for plausibility, difficulty, and failure outcomes.
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
    /// PHASE 1: Evaluate action plausibility and difficulty.
    /// Shows normal loading screen during this phase.
    /// Returns evaluation result with plausibility status and difficulty score.
    /// </summary>
    public async Task<ActionEvaluationResult> EvaluateActionAsync(
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
            return new ActionEvaluationResult
            {
                IsPlausible = false,
                PlausibilityError = "The skill required for this action is unavailable.",
                ActionSkill = thinkingSkillUsed, // Fallback
                ThinkingSkill = thinkingSkillUsed,
                Action = action,
                CurrentNode = currentNode
            };
        }

        // Get context description for trees
        var contextDescription = currentNode.GenerateNeutralDescription(_avatar.CurrentLocationId);

        // === STEP 1: PLAUSIBILITY TREE ===
        Console.WriteLine($"\n🔍 [PLAUSIBILITY CHECK] Evaluating if action is possible...");
        
        var plausibilityTree = CriticTrees.BuildPlausibilityTree(action.ActionText, contextDescription);
        var plausibilityResult = await _criticEvaluator.EvaluateTreeAsync(plausibilityTree);
        
        // If any plausibility check failed, return early
        if (!plausibilityResult.OverallSuccess)
        {
            var errorMessage = plausibilityResult.FinalErrorMessage.Length > 0 
                ? plausibilityResult.FinalErrorMessage 
                : "That action doesn't make sense in this situation.";
            
            Console.WriteLine($"   ❌ Action rejected: {errorMessage}\n");
            
            return new ActionEvaluationResult
            {
                IsPlausible = false,
                PlausibilityError = errorMessage,
                ActionSkill = actionSkill,
                ThinkingSkill = thinkingSkillUsed,
                Action = action,
                CurrentNode = currentNode
            };
        }
        
        Console.WriteLine($"   ✓ Action approved as plausible ({plausibilityResult.Trace.Count} checks passed)\n");

        // === STEP 2: DIFFICULTY TREE ===
        Console.WriteLine($"🎯 [DIFFICULTY CHECK] Evaluating action difficulty...");
        
        var difficultyTree = CriticTrees.BuildDifficultyTree(action.ActionText);
        var difficultyResult = await _criticEvaluator.EvaluateTreeAsync(difficultyTree);
        
        // Calculate difficulty score (0.0 to 1.0) from average YES probabilities
        double difficultyScore = CriticTrees.CalculateDifficultyFromResult(difficultyResult);
        int difficultyLevel = CriticTrees.DifficultyToScale(difficultyScore);
        
        Console.WriteLine($"   Difficulty: {difficultyScore:F3} (level {difficultyLevel}/10)");
        Console.WriteLine($"   Category: {(difficultyLevel <= 3 ? "Easy" : difficultyLevel <= 6 ? "Moderate" : "Hard")}");
        
        // Convert difficulty score to success probability
        // Easy (0.0) = 95% success, Moderate (0.5) = 70% success, Hard (1.0) = 40% success
        double successProbability = 0.95 - (difficultyScore * 0.55);
        
        // Adjust for skill level (body part value from 1-10)
        string bodyPartName = actionSkill.BodyParts.Length > 0 ? actionSkill.BodyParts[0].ToLower() : "hands";
        int bodyPartValue = _avatar.BodyPartLevels.TryGetValue(bodyPartName, out int bpValue) ? bpValue : 5;
        
        // Body part adds up to 10% success chance
        successProbability += (bodyPartValue - 5) * 0.02;
        successProbability = Math.Clamp(successProbability, 0.1, 0.95);
        
        Console.WriteLine($"   Success probability: {successProbability:F2} (body part '{bodyPartName}': {bodyPartValue})\n");
        
        return new ActionEvaluationResult
        {
            IsPlausible = true,
            DifficultyScore = difficultyScore,
            DifficultyLevel = difficultyLevel,
            SuccessProbability = successProbability,
            ActionSkill = actionSkill,
            ThinkingSkill = thinkingSkillUsed,
            Action = action,
            CurrentNode = currentNode
        };
    }

    /// <summary>
    /// Generate narration for a plausibility failure.
    /// Called when player has remaining noetic points or when they don't.
    /// </summary>
    public async Task<ActionExecutionResult> GeneratePlausibilityFailureNarrationAsync(
        ActionEvaluationResult evalResult,
        CancellationToken cancellationToken = default)
    {
        return await CreatePlausibilityFailureResultAsync(
            evalResult.Action,
            evalResult.ActionSkill,
            evalResult.ThinkingSkill,
            evalResult.PlausibilityError!,
            evalResult.CurrentNode,
            cancellationToken);
    }

    /// <summary>
    /// PHASE 2: Execute the dice roll and determine outcome.
    /// Shows dice rolling animation during this phase.
    /// Handles failure outcome evaluation and narration generation.
    /// </summary>
    public async Task<ActionExecutionResult> ExecuteDiceRollAsync(
        ActionEvaluationResult evalResult,
        bool succeeded,
        CancellationToken cancellationToken = default)
    {
        var action = evalResult.Action;
        var actionSkill = evalResult.ActionSkill;
        var thinkingSkillUsed = evalResult.ThinkingSkill;
        double difficultyScore = evalResult.DifficultyScore;
        int difficultyLevel = evalResult.DifficultyLevel;

        Console.WriteLine($"   Roll result: {(succeeded ? "✓ SUCCESS" : "✗ FAILURE")}\n");

        // Determine actual outcome
        OutcomeBase actualOutcome;
        CriticTrees.FailureOutcomeType? failureOutcomeType = null;
        
        if (succeeded)
        {
            actualOutcome = action.PreselectedOutcome;
        }
        else
        {
            // === STEP 3: FAILURE OUTCOME TREE ===
            Console.WriteLine($"💥 [FAILURE OUTCOME] Determining consequence of failure...");
            
            var failureTree = CriticTrees.BuildFailureOutcomeTree(action.ActionText);
            var failureResult = await _criticEvaluator.EvaluateTreeAsync(failureTree);
            
            failureOutcomeType = CriticTrees.GetFailureOutcomeFromResult(failureResult);
            
            Console.WriteLine($"   Outcome: {failureOutcomeType.Name}");
            Console.WriteLine($"   Effect: {failureOutcomeType.HumorAffected} +{failureOutcomeType.HumorAmount}");
            Console.WriteLine($"   Hint: {failureOutcomeType.NarratorHint}\n");
            
            actualOutcome = new HumorOutcome(
                failureOutcomeType.HumorAffected, 
                failureOutcomeType.HumorAmount, 
                failureOutcomeType.Description
            );
        }

        // Apply outcome to game state
        await _outcomeApplicator.ApplyOutcomeAsync(actualOutcome, _avatar);

        // Generate narration from thinking skill's perspective
        // Include failure hint if applicable
        string narration = await _outcomeNarrator.NarrateOutcomeAsync(
            action,
            actionSkill,
            thinkingSkillUsed,
            actualOutcome,
            succeeded,
            difficultyScore,
            _avatar,
            cancellationToken,
            failureOutcomeType?.NarratorHint);

        return new ActionExecutionResult
        {
            Action = action,
            ActionSkill = actionSkill,
            ThinkingSkill = thinkingSkillUsed,
            Difficulty = difficultyScore,
            DifficultyLevel = difficultyLevel,
            Succeeded = succeeded,
            ActualOutcome = actualOutcome,
            Narration = narration,
            FailureOutcomeType = failureOutcomeType,
            IsPlausibilityFailure = false
        };
    }

    /// <summary>
    /// Legacy method for backwards compatibility.
    /// Executes a player-selected action with skill check and outcome application.
    /// Returns the execution result with narration and final outcome.
    /// </summary>
    public async Task<ActionExecutionResult> ExecuteActionAsync(
        ParsedNarrativeAction action,
        NarrationNode currentNode,
        Skill thinkingSkillUsed,
        CancellationToken cancellationToken = default)
    {
        // Phase 1: Evaluate action
        var evalResult = await EvaluateActionAsync(action, currentNode, thinkingSkillUsed, cancellationToken);
        
        if (!evalResult.IsPlausible)
        {
            return await GeneratePlausibilityFailureNarrationAsync(evalResult, cancellationToken);
        }
        
        // Roll for success
        var rng = new Random();
        double roll = rng.NextDouble();
        bool succeeded = roll < evalResult.SuccessProbability;
        
        // Phase 2: Execute dice roll and get outcome
        return await ExecuteDiceRollAsync(evalResult, succeeded, cancellationToken);
    }

    /// <summary>
    /// Creates a failure result when the action fails plausibility checks.
    /// Generates appropriate narration explaining why the action is not possible.
    /// </summary>
    private async Task<ActionExecutionResult> CreatePlausibilityFailureResultAsync(
        ParsedNarrativeAction action,
        Skill actionSkill,
        Skill thinkingSkill,
        string plausibilityError,
        NarrationNode currentNode,
        CancellationToken cancellationToken)
    {
        // Generate narration explaining why the action is not possible
        var failureOutcome = new HumorOutcome("Melancholia", 1, "inability to act");
        
        string narration = await _outcomeNarrator.NarratePlausibilityFailureAsync(
            action,
            thinkingSkill,
            plausibilityError,
            _avatar,
            cancellationToken);
        
        return new ActionExecutionResult
        {
            Action = action,
            ActionSkill = actionSkill,
            ThinkingSkill = thinkingSkill,
            Difficulty = 0,
            DifficultyLevel = 0,
            Succeeded = false,
            ActualOutcome = failureOutcome,
            Narration = narration,
            PlausibilityError = plausibilityError,
            IsPlausibilityFailure = true
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
    public int DifficultyLevel { get; set; }
    public bool Succeeded { get; set; }
    public OutcomeBase ActualOutcome { get; set; } = null!;
    public string Narration { get; set; } = "";
    
    /// <summary>
    /// The failure outcome type if action failed (null if succeeded or plausibility failed).
    /// </summary>
    public CriticTrees.FailureOutcomeType? FailureOutcomeType { get; set; }
    
    /// <summary>
    /// The plausibility error message if action was rejected as implausible.
    /// </summary>
    public string? PlausibilityError { get; set; }
    
    /// <summary>
    /// True if this result is from a plausibility failure (not a dice roll failure).
    /// Used to determine if player can retry with remaining noetic points.
    /// </summary>
    public bool IsPlausibilityFailure { get; set; }
}
