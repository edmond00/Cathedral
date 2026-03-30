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
    public ModusMentis ActionModusMentis { get; set; } = null!;
    public ModusMentis ThinkingModusMentis { get; set; } = null!;
    public ParsedNarrativeAction Action { get; set; } = null!;
    public NarrationNode CurrentNode { get; set; } = null!;
}

/// <summary>
/// Orchestrates action execution: modusMentis checks, outcome determination, and narration.
/// Uses tree-based Critic evaluation for plausibility, difficulty, and failure outcomes.
/// </summary>
public class ActionExecutionController
{
    private readonly OutcomeNarrator _outcomeNarrator;
    private readonly OutcomeApplicator _outcomeApplicator;
    private readonly Protagonist _protagonist;
    private readonly CriticEvaluator _criticEvaluator;

    public ActionExecutionController(
        OutcomeNarrator outcomeNarrator,
        OutcomeApplicator outcomeApplicator,
        Protagonist protagonist,
        CriticEvaluator criticEvaluator)
    {
        _outcomeNarrator = outcomeNarrator;
        _outcomeApplicator = outcomeApplicator;
        _protagonist = protagonist;
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
        ModusMentis thinkingModusMentisUsed,
        CancellationToken cancellationToken = default)
    {
        // Debug: Show what we're searching for and what we have
        Console.WriteLine($"DEBUG: Looking for action modusMentis ID: '{action.ActionModusMentisId}'");
        Console.WriteLine($"DEBUG: Protagonist has {_protagonist.ModiMentis.Count} modiMentis:");
        foreach (var modusMentis in _protagonist.ModiMentis)
        {
            Console.WriteLine($"  - {modusMentis.ModusMentisId} ({modusMentis.DisplayName})");
        }
        
        // Resolve action modusMentis
        var actionModusMentis = _protagonist.ModiMentis.FirstOrDefault(s => s.ModusMentisId == action.ActionModusMentisId);
        if (actionModusMentis == null)
        {
            Console.WriteLine($"DEBUG: ModusMentis '{action.ActionModusMentisId}' NOT FOUND in protagonist's modiMentis!");
            return new ActionEvaluationResult
            {
                IsPlausible = false,
                PlausibilityError = "The modusMentis required for this action is unavailable.",
                ActionModusMentis = thinkingModusMentisUsed, // Fallback
                ThinkingModusMentis = thinkingModusMentisUsed,
                Action = action,
                CurrentNode = currentNode
            };
        }

        // Get context description for trees
        var contextDescription = currentNode.ContextDescription;

        // === STEP 1: PLAUSIBILITY TREE ===
        Console.WriteLine($"\n🔍 [PLAUSIBILITY CHECK] Evaluating if action is possible...");
        
        var plausibilityTree = CriticTrees.BuildPlausibilityTree(action.ActionText, contextDescription);
        var plausibilityResult = await _criticEvaluator.EvaluateTreeAsync(plausibilityTree);
        
        // If any plausibility check failed, return early
        if (!plausibilityResult.OverallSuccess)
        {
            var errorMessage = plausibilityResult.FirstErrorMessage.Length > 0
                ? plausibilityResult.FirstErrorMessage
                : "That action doesn't make sense in this situation.";
            
            Console.WriteLine($"   ❌ Action rejected: {errorMessage}\n");
            
            return new ActionEvaluationResult
            {
                IsPlausible = false,
                PlausibilityError = errorMessage,
                ActionModusMentis = actionModusMentis,
                ThinkingModusMentis = thinkingModusMentisUsed,
                Action = action,
                CurrentNode = currentNode
            };
        }
        
        Console.WriteLine($"   ✓ Action approved as plausible ({plausibilityResult.Trace.Count} checks passed)\n");

        // === STEP 2: DIFFICULTY TREE ===
        Console.WriteLine($"🎯 [DIFFICULTY CHECK] Evaluating action difficulty...");
        
        var difficultyTree = CriticTrees.BuildDifficultyTree(action.ActionText, contextDescription);
        var difficultyResult = await _criticEvaluator.EvaluateTreeAsync(difficultyTree);

        // Map the chosen difficulty level to a 0.0–1.0 score
        double difficultyScore = CriticTrees.CalculateDifficultyFromResult(difficultyResult);

        int difficultyLevel = CriticTrees.DifficultyToScale(difficultyScore);
        
        Console.WriteLine($"   Difficulty: {difficultyScore:F3} (level {difficultyLevel}/10)");
        Console.WriteLine($"   Category: {(difficultyLevel <= 3 ? "Easy" : difficultyLevel <= 6 ? "Moderate" : "Hard")}");
        
        // Convert difficulty score to success probability
        // Easy (0.0) = 95% success, Moderate (0.5) = 70% success, Hard (1.0) = 40% success
        double successProbability = 0.95 - (difficultyScore * 0.55);
        
        // Adjust for organ score
        string organId = actionModusMentis.Organs.Length > 0 ? actionModusMentis.Organs[0] : "hands";
        int organScore = _protagonist.GetOrganById(organId)?.Score ?? 5;
        
        // Organ score adds up to 10% success chance
        successProbability += (organScore - 5) * 0.02;
        successProbability = Math.Clamp(successProbability, 0.1, 0.95);
        
        Console.WriteLine($"   Success probability: {successProbability:F2} (organ '{organId}': {organScore})\n");
        
        return new ActionEvaluationResult
        {
            IsPlausible = true,
            DifficultyScore = difficultyScore,
            DifficultyLevel = difficultyLevel,
            SuccessProbability = successProbability,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
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
            evalResult.ActionModusMentis,
            evalResult.ThinkingModusMentis,
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
        var actionModusMentis = evalResult.ActionModusMentis;
        var thinkingModusMentisUsed = evalResult.ThinkingModusMentis;
        double difficultyScore = evalResult.DifficultyScore;
        int difficultyLevel = evalResult.DifficultyLevel;
        var currentNode = evalResult.CurrentNode;

        Console.WriteLine($"   Roll result: {(succeeded ? "✓ SUCCESS" : "✗ FAILURE")}\n");

        // Determine actual outcome
        OutcomeBase actualOutcome;
        Wound? failureWound = null;

        if (succeeded)
        {
            actualOutcome = action.PreselectedOutcome;
        }
        else
        {
            // === STEP 3: FAILURE OUTCOME TREE ===
            Console.WriteLine($"💥 [FAILURE OUTCOME] Determining consequence of failure...");

            var contextDescription = currentNode.ContextDescription;
            var wildcardCandidates = BuildWildcardCandidates();
            var failureTree = CriticTrees.BuildFailureOutcomeTree(action.ActionText, contextDescription, wildcardCandidates);
            var failureResult = await _criticEvaluator.EvaluateTreeAsync(failureTree);

            failureWound = CriticTrees.GetWoundFromResult(failureResult, wildcardCandidates);

            if (failureWound != null)
                Console.WriteLine($"   Wound: {failureWound.WoundName} ({WoundLocationLabel(failureWound)}, {failureWound.Handicap})\n");
            else
                Console.WriteLine("   No wound inflicted.\n");

            actualOutcome = new WoundOutcome(failureWound);
        }

        // Apply outcome to game state
        await _outcomeApplicator.ApplyOutcomeAsync(actualOutcome, _protagonist);

        // Generate narration — pass wound description as failure hint
        string? failureHint = failureWound != null
            ? $"The character suffered a wound: {failureWound.WoundName} to their {WoundLocationLabel(failureWound)}"
            : null;

        string narration = await _outcomeNarrator.NarrateOutcomeAsync(
            action,
            actionModusMentis,
            thinkingModusMentisUsed,
            actualOutcome,
            succeeded,
            difficultyScore,
            _protagonist,
            cancellationToken,
            failureHint);

        return new ActionExecutionResult
        {
            Action = action,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
            Difficulty = difficultyScore,
            DifficultyLevel = difficultyLevel,
            Succeeded = succeeded,
            ActualOutcome = actualOutcome,
            Narration = narration,
            FailureWound = failureWound,
            IsPlausibilityFailure = false
        };
    }

    /// <summary>
    /// Legacy method for backwards compatibility.
    /// Executes a player-selected action with modusMentis check and outcome application.
    /// Returns the execution result with narration and final outcome.
    /// </summary>
    public async Task<ActionExecutionResult> ExecuteActionAsync(
        ParsedNarrativeAction action,
        NarrationNode currentNode,
        ModusMentis thinkingModusMentisUsed,
        CancellationToken cancellationToken = default)
    {
        // Phase 1: Evaluate action
        var evalResult = await EvaluateActionAsync(action, currentNode, thinkingModusMentisUsed, cancellationToken);
        
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
    /// Builds the list of locations that can receive wildcard wounds in the failure tree.
    /// Body parts with AcceptsWildcardWounds=true contribute one candidate each.
    /// Organs with AcceptsWildcardWounds=true contribute one candidate per organ part.
    /// </summary>
    private IReadOnlyList<WildcardCandidate> BuildWildcardCandidates()
    {
        var candidates = new List<WildcardCandidate>();

        foreach (var bp in _protagonist.BodyParts)
        {
            if (bp.AcceptsWildcardWounds)
                candidates.Add(new WildcardCandidate(bp.Id, bp.DisplayName, bp.Id));

            foreach (var organ in bp.Organs.Where(o => o.AcceptsWildcardWounds))
                foreach (var part in organ.Parts)
                    candidates.Add(new WildcardCandidate(part.Id, part.DisplayName, part.Id));
        }

        return candidates;
    }

    /// <summary>Returns a readable location label for a wound, using WildcardZoneHint as fallback.</summary>
    private static string WoundLocationLabel(Wound wound)
    {
        var raw = wound.TargetId.Length > 0
            ? wound.TargetId
            : wound.WildcardZoneHint ?? "body";
        return raw.Replace('_', ' ');
    }

    /// <summary>
    /// Creates a failure result when the action fails plausibility checks.
    /// Generates appropriate narration explaining why the action is not possible.
    /// </summary>
    private async Task<ActionExecutionResult> CreatePlausibilityFailureResultAsync(
        ParsedNarrativeAction action,
        ModusMentis actionModusMentis,
        ModusMentis thinkingModusMentis,
        string plausibilityError,
        NarrationNode currentNode,
        CancellationToken cancellationToken)
    {
        // Generate narration explaining why the action is not possible
        var failureOutcome = new HumorOutcome("Melancholia", 1, "inability to act");
        
        string narration = await _outcomeNarrator.NarratePlausibilityFailureAsync(
            action,
            actionModusMentis,
            plausibilityError,
            _protagonist,
            cancellationToken);
        
        return new ActionExecutionResult
        {
            Action = action,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentis,
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
    public ModusMentis? ActionModusMentis { get; set; }
    public ModusMentis ThinkingModusMentis { get; set; } = null!;
    public double Difficulty { get; set; }
    public int DifficultyLevel { get; set; }
    public bool Succeeded { get; set; }
    public OutcomeBase ActualOutcome { get; set; } = null!;
    public string Narration { get; set; } = "";
    
    /// <summary>
    /// The wound inflicted on the protagonist if action failed with a physical injury (null otherwise).
    /// </summary>
    public Wound? FailureWound { get; set; }
    
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
