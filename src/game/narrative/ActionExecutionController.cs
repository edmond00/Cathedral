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

    /// <summary>
    /// Witness context computed before the pipeline. Carried forward so
    /// <see cref="ActionExecutionController.ExecuteDiceRollAsync"/> can re-ask
    /// the witness detection question on failure without needing the scene again.
    /// </summary>
    public Cathedral.Game.Scene.WitnessContext WitnessContext { get; set; }
        = Cathedral.Game.Scene.WitnessContext.None;

    /// <summary>
    /// Threat context computed before the pipeline (enemy proximity).
    /// Carried forward so <see cref="ActionExecutionController.ExecuteDiceRollAsync"/>
    /// can ask the under-threat opportunity question on failure.
    /// </summary>
    public Cathedral.Game.Scene.ThreatContext ThreatContext { get; set; }
        = Cathedral.Game.Scene.ThreatContext.None;
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
    private readonly WorldContext _worldContext;
    private readonly int _locationId;

    /// <summary>Exposes the outcome narrator for item combination failure narration.</summary>
    public OutcomeNarrator OutcomeNarrator => _outcomeNarrator;

    /// <summary>Exposes the critic evaluator for item appropriateness checks.</summary>
    public CriticEvaluator CriticEvaluator => _criticEvaluator;

    public ActionExecutionController(
        OutcomeNarrator outcomeNarrator,
        OutcomeApplicator outcomeApplicator,
        Protagonist protagonist,
        CriticEvaluator criticEvaluator,
        WorldContext worldContext,
        int locationId)
    {
        _outcomeNarrator = outcomeNarrator;
        _outcomeApplicator = outcomeApplicator;
        _protagonist = protagonist;
        _criticEvaluator = criticEvaluator;
        _worldContext = worldContext;
        _locationId = locationId;
    }

    /// <summary>
    /// PHASE 1: Evaluate action plausibility and difficulty.
    /// Shows normal loading screen during this phase.
    /// Returns evaluation result with plausibility status and difficulty score.
    /// When <paramref name="witnessContext"/> is non-None, also asks the LLM
    /// how likely the witness is to detect the action (stored for step 4b re-ask on failure).
    /// When <paramref name="threatContext"/> is non-None and the action cannot be used under
    /// threat, asks the LLM whether the enemy gets an opportunity (informational).
    /// </summary>
    public async Task<ActionEvaluationResult> EvaluateActionAsync(
        ParsedNarrativeAction action,
        NarrationNode currentNode,
        ModusMentis thinkingModusMentisUsed,
        Cathedral.Game.Scene.WitnessContext? witnessContext = null,
        Cathedral.Game.Scene.ThreatContext? threatContext = null,
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

        // Build rich context for critic trees
        var goalDescription = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
        var criticContext = new CriticContext(
            currentNode, _worldContext, _locationId, goalDescription);

        // Attach item context so tool-missing checks don't misfire when a proper item is in use
        if (action.CombinedItem != null)
            criticContext.CombinedItemContext = $"{action.CombinedItem.DisplayName} ({action.CombinedItem.Description})";

        // === STEP 1: PLAUSIBILITY TREE ===
        Console.WriteLine($"\n🔍 [PLAUSIBILITY CHECK] Evaluating if action is possible...");

        var plausibilityTree = CriticTrees.BuildPlausibilityTree(action.ActionText, criticContext);
        var plausibilityResult = await _criticEvaluator.EvaluateTreeAsync(plausibilityTree, continueOnFailure: true);

        // If any plausibility check failed, try second opinions before rejecting
        if (!plausibilityResult.OverallSuccess)
        {
            bool overridden = await EvaluateSecondOpinionsAsync(
                plausibilityResult, action, criticContext, cancellationToken);

            if (!overridden)
            {
                // Prefer the critic's free-text reason over the generic error label
                var errorMessage = plausibilityResult.CombinedFailureReason.Length > 0
                    ? plausibilityResult.CombinedFailureReason
                    : plausibilityResult.FirstErrorMessage.Length > 0
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

            Console.WriteLine($"   ✓ Second opinion overrode plausibility failure — action approved.\n");
        }

        Console.WriteLine($"   ✓ Action approved as plausible ({plausibilityResult.Trace.Count} checks passed)\n");

        // === STEP 2: DIFFICULTY (reuse pre-computed level from narration menu) ===
        // Difficulty is already evaluated during the thinking phase; reuse it to avoid a
        // second LLM call that could produce a different value and cause a mismatch.
        int difficultyLevel;
        if (action.DifficultyLevel > 0)
        {
            difficultyLevel = action.DifficultyLevel;
            Console.WriteLine($"🎯 [DIFFICULTY CHECK] Reusing pre-computed difficulty: {difficultyLevel}/10");
        }
        else
        {
            Console.WriteLine($"🎯 [DIFFICULTY CHECK] No pre-computed difficulty — evaluating now...");
            var difficultyTree = CriticTrees.BuildDifficultyTree(action.ActionText, criticContext);
            var difficultyResult = await _criticEvaluator.EvaluateTreeAsync(difficultyTree);
            difficultyLevel = CriticTrees.CalculateFinalDifficulty(action.Verb, difficultyResult);
        }
        double difficultyScore = CriticTrees.DifficultyLevelToScore(difficultyLevel);
        
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

        // === STEP 3: WITNESS DETECTION QUESTION (if a witness is present) ===
        var resolvedWitnessContext = witnessContext ?? Cathedral.Game.Scene.WitnessContext.None;
        if (resolvedWitnessContext.Type != Cathedral.Game.Scene.WitnessType.None)
        {
            Console.WriteLine($"👁 [WITNESS DETECTION] {resolvedWitnessContext.Type} witness present — asking detection probability...");
            var witnessTree = CriticTrees.BuildWitnessDetectionTree(
                action.ActionText, resolvedWitnessContext, criticContext, actionFailed: false);
            var witnessResult = await _criticEvaluator.EvaluateTreeAsync(witnessTree);
            Console.WriteLine($"   Detection chance: {witnessResult.FinalChosenId}\n");
            // Result stored for context only; step 4b re-asks with failure context.
        }

        // === STEP 3b: UNDER-THREAT OPPORTUNITY QUESTION (visual enemy + action can't be used under threat) ===
        var resolvedThreatContext = threatContext ?? Cathedral.Game.Scene.ThreatContext.None;
        if (resolvedThreatContext.Level == Cathedral.Game.Scene.ThreatLevel.Visual)
        {
            bool canBeUsedUnderThreat = action.Verb.CanBeUsedUnderThreat;

            if (!canBeUsedUnderThreat)
            {
                Console.WriteLine($"⚔ [UNDER THREAT] Visual enemy present — asking opportunity probability...");
                var threatTree = CriticTrees.BuildUnderThreatTree(
                    action.ActionText, resolvedThreatContext, criticContext, actionFailed: false);
                var threatResult = await _criticEvaluator.EvaluateTreeAsync(threatTree);
                Console.WriteLine($"   Opportunity chance: {threatResult.FinalChosenId}\n");
                // Informational only — step 4b re-asks if action fails.
            }
        }

        return new ActionEvaluationResult
        {
            IsPlausible = true,
            DifficultyScore = difficultyScore,
            DifficultyLevel = difficultyLevel,
            SuccessProbability = successProbability,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
            Action = action,
            CurrentNode = currentNode,
            WitnessContext = resolvedWitnessContext,
            ThreatContext = resolvedThreatContext,
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

            var goalDescription2 = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var failureCriticContext = new CriticContext(
                currentNode, _worldContext, _locationId, goalDescription2);
            var wildcardCandidates = BuildWildcardCandidates();
            var failureTree = CriticTrees.BuildFailureOutcomeTree(action.ActionText, failureCriticContext, wildcardCandidates);
            DebugMode.InFailureOutcomeTree = true;
            var failureResult = await _criticEvaluator.EvaluateTreeAsync(failureTree);
            DebugMode.InFailureOutcomeTree = false;

            failureWound = CriticTrees.GetWoundFromResult(failureResult, wildcardCandidates);

            if (failureWound != null)
                Console.WriteLine($"   Wound: {failureWound.WoundName} ({WoundLocationLabel(failureWound)}, {failureWound.Handicap})\n");
            else
                Console.WriteLine("   No wound inflicted.\n");

            actualOutcome = new WoundOutcome(failureWound);
        }

        // Apply outcome to game state
        await _outcomeApplicator.ApplyOutcomeAsync(actualOutcome, _protagonist);

        // === STEP 4: ITEM CONSUMPTION CHECK ===
        if (action.CombinedItem != null)
        {
            Console.WriteLine($"🧪 [ITEM CONSUMPTION] Checking if {action.CombinedItem.ItemId} was consumed...");
            string itemContext = $"{action.CombinedItem.DisplayName} ({action.CombinedItem.Description})";
            var goalDescription3 = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var consumptionCriticContext = new CriticContext(currentNode, _worldContext, _locationId, goalDescription3);
            var consumptionTree = CriticTrees.BuildItemConsumptionTree(action.ActionText, itemContext, consumptionCriticContext);
            var consumptionResult = await _criticEvaluator.EvaluateTreeAsync(consumptionTree);
            if (CriticTrees.IsItemConsumedFromResult(consumptionResult))
            {
                _protagonist.RemoveItem(action.CombinedItem);
                Console.WriteLine($"   Item consumed and removed: {action.CombinedItem.ItemId}");
            }
            else
            {
                Console.WriteLine($"   Item retained: {action.CombinedItem.ItemId}");
            }
        }

        // === STEP 4b: WITNESS DETECTION RE-ASK (failure path only) ===
        // On success the action was clean — no confrontation regardless of witnesses.
        // On failure, re-ask whether the witness noticed, now knowing the action failed.
        bool witnessDetected = false;
        Cathedral.Game.Npc.NpcEntity? detectedWitness = null;
        if (!succeeded && evalResult.WitnessContext.Type != Cathedral.Game.Scene.WitnessType.None)
        {
            Console.WriteLine($"👁 [WITNESS DETECTION — FAILURE] Re-evaluating witness detection after failed action...");
            var goalDescription4 = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var witnessCtx = new CriticContext(currentNode, _worldContext, _locationId, goalDescription4);
            var witnessTree = CriticTrees.BuildWitnessDetectionTree(
                action.ActionText, evalResult.WitnessContext, witnessCtx, actionFailed: true);
            var witnessResult = await _criticEvaluator.EvaluateTreeAsync(witnessTree);
            witnessDetected = CriticTrees.IsWitnessDetectedFromResult(witnessResult);
            if (witnessDetected)
            {
                detectedWitness = evalResult.WitnessContext.Witness;
                Console.WriteLine($"   Witness detected the failed action — confrontation pending.\n");
            }
            else
            {
                Console.WriteLine($"   Witness did not detect the failed action ({witnessResult.FinalChosenId}).\n");
            }
        }

        // === STEP 4c: UNDER-THREAT OPPORTUNITY RE-ASK (failure path only) ===
        // If an enemy is nearby and the action fails, ask whether they seize the moment.
        bool fightTriggered = false;
        Cathedral.Game.Npc.NpcEntity? fightEnemy = null;
        if (!succeeded && evalResult.ThreatContext.Level != Cathedral.Game.Scene.ThreatLevel.None)
        {
            Console.WriteLine($"⚔ [UNDER THREAT — FAILURE] Re-evaluating enemy opportunity after failed action...");
            var goalDescription5 = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var threatCtx = new CriticContext(currentNode, _worldContext, _locationId, goalDescription5);
            var threatTree = CriticTrees.BuildUnderThreatTree(
                action.ActionText, evalResult.ThreatContext, threatCtx, actionFailed: true);
            var threatResult = await _criticEvaluator.EvaluateTreeAsync(threatTree);
            fightTriggered = CriticTrees.IsOpportunityFromResult(threatResult);
            if (fightTriggered)
            {
                fightEnemy = evalResult.ThreatContext.Threat;
                Console.WriteLine($"   Enemy seized the opportunity — fight triggered.\n");
            }
            else
            {
                Console.WriteLine($"   Enemy did not seize an opportunity ({threatResult.FinalChosenId}).\n");
            }
        }

        // Generate narration — pass wound description as failure hint
        string? failureHint = failureWound != null
            ? $"The character suffered a wound: {failureWound.WoundName} to their {WoundLocationLabel(failureWound)}"
            : null;

        string narration = await _outcomeNarrator.NarrateOutcomeAsync(
            action,
            actionModusMentis,
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
            IsPlausibilityFailure = false,
            WitnessDetected = witnessDetected,
            DetectedWitness = detectedWitness,
            FightTriggered = fightTriggered,
            FightEnemy = fightEnemy,
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
        var evalResult = await EvaluateActionAsync(action, currentNode, thinkingModusMentisUsed, witnessContext: null, threatContext: null, cancellationToken);
        
        if (!evalResult.IsPlausible)
        {
            return await GeneratePlausibilityFailureNarrationAsync(evalResult, cancellationToken);
        }
        
        // Roll n dice (1–6 each), succeed if sixes >= difficulty
        var rng = new Random();
        int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel());
        int sixes = 0;
        for (int i = 0; i < numberOfDice; i++)
            if (rng.Next(1, 7) == 6) sixes++;
        bool succeeded = sixes >= evalResult.DifficultyLevel;
        
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
    /// For each failing node in <paramref name="plausibilityResult"/>, checks whether the
    /// corresponding choice has a <see cref="Config.PlausibilityQuestions.SecondOpinion"/> whose
    /// runtime condition is satisfied.  If so, evaluates it and returns <c>true</c> the first
    /// time one passes — meaning the original failure is overridden and the action is plausible.
    /// Returns <c>false</c> if no second opinion overrides the failure.
    /// </summary>
    private async Task<bool> EvaluateSecondOpinionsAsync(
        CriticTreeResult plausibilityResult,
        ParsedNarrativeAction action,
        CriticContext criticContext,
        CancellationToken cancellationToken)
    {
        foreach (var failedNode in plausibilityResult.Trace.Where(r => r.IsFailure))
        {
            var question = Config.PlausibilityQuestions.Questions
                .FirstOrDefault(q => q.Name == failedNode.NodeName);
            if (question == null) continue;

            var failedChoice = question.Choices.FirstOrDefault(c => c.Id == failedNode.ChosenId);
            if (failedChoice?.SecondOpinions == null) continue;

            foreach (var secondOpinion in failedChoice.SecondOpinions)
            {
                if (!IsSecondOpinionConditionMet(secondOpinion.Condition, action)) continue;

                Console.WriteLine($"\n🔄 [SECOND OPINION — {secondOpinion.Question.Name}] " +
                    (action.CombinedItem != null
                        ? $"Checking if '{action.CombinedItem.DisplayName}' can serve as the required tool..."
                        : "Checking if bare hands can substitute..."));

                var soTree = CriticTrees.BuildSecondOpinionTree(
                    secondOpinion.Question, action.ActionText, criticContext);
                var soResult = await _criticEvaluator.EvaluateTreeAsync(soTree);

                if (soResult.OverallSuccess)
                {
                    Console.WriteLine($"   ✓ [{soResult.FinalChosenId}] Overriding '{failedNode.ChosenId}'.");
                    return true;
                }

                Console.WriteLine($"   ✗ [{soResult.FinalChosenId}] Second opinion confirms failure.");
            }
        }

        return false;
    }

    /// <summary>Returns true when the runtime condition for a second opinion is satisfied.</summary>
    private static bool IsSecondOpinionConditionMet(
        Config.PlausibilityQuestions.SecondOpinionCondition condition,
        ParsedNarrativeAction action)
    {
        return condition switch
        {
            Config.PlausibilityQuestions.SecondOpinionCondition.ItemInUse   => action.CombinedItem != null,
            Config.PlausibilityQuestions.SecondOpinionCondition.NoItemInUse => action.CombinedItem == null,
            _ => false,
        };
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

    /// <summary>
    /// True when a witness detected the failed action (step 4b).
    /// Always false on success — the new design never triggers witness confrontation on success.
    /// </summary>
    public bool WitnessDetected { get; set; }

    /// <summary>
    /// The NPC who detected the crime. Non-null only when <see cref="WitnessDetected"/> is true.
    /// </summary>
    public Cathedral.Game.Npc.NpcEntity? DetectedWitness { get; set; }

    /// <summary>
    /// True when a nearby enemy seized an opportunity to attack on the failure path (step 4c).
    /// Always false on success.
    /// </summary>
    public bool FightTriggered { get; set; }

    /// <summary>
    /// The enemy who triggered the fight. Non-null only when <see cref="FightTriggered"/> is true.
    /// </summary>
    public Cathedral.Game.Npc.NpcEntity? FightEnemy { get; set; }
}
