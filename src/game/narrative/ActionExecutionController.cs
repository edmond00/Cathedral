using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game;
using Cathedral.Game.Narrative.Preview;

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

    /// <summary>
    /// The party member currently performing actions. Defaults to the protagonist but is
    /// reassigned by <see cref="NarrativeController"/> whenever a companion becomes the active
    /// party member (after a "Speak About"), so skill resolution, organ scores, XP, wounds, and
    /// item consumption all operate on whoever is actually acting.
    /// </summary>
    public PartyMember ActingMember { get; set; }
    private readonly ItemUseCritic _criticEvaluator;
    private readonly WorldContext _worldContext;
    private readonly int _locationId;
    private readonly Random _rng = GameRng.Stream("action-execution");

    /// <summary>Exposes the outcome narrator for item combination failure narration.</summary>
    public OutcomeNarrator OutcomeNarrator => _outcomeNarrator;

    /// <summary>Exposes the item-use critic for item appropriateness / consumption checks.</summary>
    public ItemUseCritic ItemUseCritic => _criticEvaluator;

    public ActionExecutionController(
        OutcomeNarrator outcomeNarrator,
        Protagonist protagonist,
        ItemUseCritic criticEvaluator,
        WorldContext worldContext,
        int locationId)
    {
        _outcomeNarrator = outcomeNarrator;
        ActingMember = protagonist;
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
    // Not async: every LLM step formerly here (plausibility, difficulty, witness/threat) has moved
    // out, so evaluation is now pure arithmetic. Kept returning Task so callers still await it.
    public Task<ActionEvaluationResult> EvaluateActionAsync(
        ParsedNarrativeAction action,
        NarrationNode currentNode,
        ModusMentis thinkingModusMentisUsed,
        Cathedral.Game.Scene.WitnessContext? witnessContext = null,
        Cathedral.Game.Scene.ThreatContext? threatContext = null,
        CancellationToken cancellationToken = default)
    {
        // Debug: Show what we're searching for and what we have
        Console.WriteLine($"DEBUG: Looking for action modusMentis ID: '{action.ActionModusMentisId}'");
        Console.WriteLine($"DEBUG: Protagonist has {ActingMember.ModiMentis.Count} modiMentis:");
        foreach (var modusMentis in ActingMember.ModiMentis)
        {
            Console.WriteLine($"  - {modusMentis.ModusMentisId} ({modusMentis.DisplayName})");
        }
        
        // Resolve action modusMentis. The action was assembled from the acting member's own
        // modiMentis during the thinking phase, so the id always resolves here — a miss would be a
        // programming error, not a gameplay outcome, so fail loud rather than manufacturing a
        // "plausibility failure" the player could never provoke.
        var actionModusMentis = ActingMember.ModiMentis.FirstOrDefault(s => s.ModusMentisId == action.ActionModusMentisId)
            ?? throw new InvalidOperationException(
                $"EvaluateActionAsync: action modusMentis '{action.ActionModusMentisId}' is not present on "
                + $"the acting member '{ActingMember.DisplayName}'.");

        // Difficulty was decided at thinking time from the persona-fit answer (verb base ± the
        // eager/willing/unsure modifier) and stored on the action. Possibility is likewise settled
        // before this point — by the coded rules (pre-execution) and persona-fit cancellation — so
        // all this method does is normalise the difficulty into the 0..1 score the narration
        // prompts want. It decides nothing about the outcome: success is the dice roll alone
        // (difficulty = 6s needed, dice = summed modus-mentis levels), which is where the anatomy
        // enters, and only through the level cap those modi mentis were raised under.
        int difficultyLevel = action.DifficultyLevel > 0
            ? action.DifficultyLevel
            : Math.Clamp(action.Verb.DifficultyFor(action.PreselectedOutcome?.VerbView.Target), 1, 10);
        double difficultyScore = (Math.Clamp(difficultyLevel, 1, 10) - 1) / 9.0;

        Console.WriteLine($"🎯 [DIFFICULTY] level {difficultyLevel}/10 (score {difficultyScore:F3}, " +
            $"{(difficultyLevel <= 3 ? "Easy" : difficultyLevel <= 6 ? "Moderate" : "Hard")})");

        return Task.FromResult(new ActionEvaluationResult
        {
            IsPlausible = true,
            DifficultyScore = difficultyScore,
            DifficultyLevel = difficultyLevel,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
            Action = action,
            CurrentNode = currentNode,
            WitnessContext = witnessContext ?? Cathedral.Game.Scene.WitnessContext.None,
            ThreatContext = threatContext ?? Cathedral.Game.Scene.ThreatContext.None,
        });
    }

    /// <summary>
    /// Generate narration for a plausibility failure.
    /// Called when player has remaining noetic points or when they don't.
    /// </summary>
    public async Task<ActionExecutionResult> GeneratePlausibilityFailureNarrationAsync(
        ActionEvaluationResult evalResult,
        CancellationToken cancellationToken = default,
        ILlmPreviewSink? preview = null)
    {
        return await CreatePlausibilityFailureResultAsync(
            evalResult.Action,
            evalResult.ActionModusMentis,
            evalResult.ThinkingModusMentis,
            evalResult.PlausibilityError!,
            evalResult.CurrentNode,
            cancellationToken,
            preview);
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

        // Determine actual outcome and (on failure) its consequences.
        OutcomeBase actualOutcome;
        WoundInstance? failureWound = null;
        bool witnessDetected = false;
        Cathedral.Game.Npc.NpcEntity? detectedWitness = null;
        bool fightTriggered = false;
        Cathedral.Game.Npc.NpcEntity? fightEnemy = null;

        if (succeeded)
        {
            actualOutcome = action.PreselectedOutcome;

            // Award +1 XP to every modusMentis in the action chain (observation → thinking → action).
            foreach (var chainModusMentis in action.GetModusMentisChain())
                ActingMember.AwardModusMentisXp(chainModusMentis);
        }
        else
        {
            var (wound, wDetected, wWitness, fTriggered, fEnemy) = ResolveFailureConsequences(evalResult);
            failureWound    = wound;
            witnessDetected = wDetected;
            detectedWitness = wWitness;
            fightTriggered  = fTriggered;
            fightEnemy      = fEnemy;
            actualOutcome   = new WoundOutcome(failureWound);
        }

        // Wound-infliction report (failure only). Verb-specific reports are built later in
        // NarrativeController via SuccessReports()/FailureReports().
        var llmDecidedReports = new System.Collections.Generic.List<OutcomeReport>();
        if (!succeeded && failureWound != null)
            llmDecidedReports.Add(new WoundInflictionOutcome(failureWound));

        // === ITEM CONSUMPTION CHECK (item critic) ===
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
                ActingMember.RemoveItem(action.CombinedItem);
                Console.WriteLine($"   Item consumed and removed: {action.CombinedItem.ItemId}");
            }
            else
            {
                Console.WriteLine($"   Item retained: {action.CombinedItem.ItemId}");
            }
        }

        // Generate narration — the wound (if any) contributes its Verbatim to the consequence list.
        string narration = await _outcomeNarrator.NarrateOutcomeAsync(
            action,
            actionModusMentis,
            actualOutcome,
            succeeded,
            difficultyScore,
            ActingMember,
            cancellationToken,
            outcomeVerbatims: CollectVerbatims(llmDecidedReports));

        return new ActionExecutionResult
        {
            Action = action,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
            Difficulty = difficultyScore,
            DifficultyLevel = difficultyLevel,
            Succeeded = succeeded,
            ActualOutcome = actualOutcome,
            LlmDecidedReports = llmDecidedReports,
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
    /// PHASE 2 (post-dice variant): compute ONLY the actual outcome once the final (possibly
    /// humor-modified) success/failure is known, streaming its narration into <paramref name="preview"/>
    /// like every other text. No game-state side-effects are applied here; the caller commits them on
    /// the preview's CONTINUE. Item consumption is decided regardless of success (it does not depend on
    /// the outcome); the failure branch also samples the wound and resolves witness/threat consequences.
    /// </summary>
    public async Task<ActionExecutionResult> PrepareSingleOutcomeAsync(
        ActionEvaluationResult evalResult, bool succeeded,
        IReadOnlyList<OutcomeReport> verbReports,
        ILlmPreviewSink? preview = null,
        CancellationToken cancellationToken = default)
    {
        var action = evalResult.Action;
        var actionModusMentis = evalResult.ActionModusMentis;
        var thinkingModusMentisUsed = evalResult.ThinkingModusMentis;
        double difficultyScore = evalResult.DifficultyScore;
        int difficultyLevel = evalResult.DifficultyLevel;
        var currentNode = evalResult.CurrentNode;

        // ── Item consumption decision (independent of the dice outcome) ──
        bool itemConsumed = false;
        if (action.CombinedItem != null)
        {
            string itemContext = $"{action.CombinedItem.DisplayName} ({action.CombinedItem.Description})";
            var goalDesc = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var consumptionCtx = new CriticContext(currentNode, _worldContext, _locationId, goalDesc);
            var consumptionTree = CriticTrees.BuildItemConsumptionTree(action.ActionText, itemContext, consumptionCtx);
            var consumptionResult = await _criticEvaluator.EvaluateTreeAsync(consumptionTree);
            itemConsumed = CriticTrees.IsItemConsumedFromResult(consumptionResult);
        }

        if (succeeded)
        {
            OutcomeBase successOutcome = action.PreselectedOutcome;

            // The full report list is exactly the verb's success reports; their verbatims become the
            // "Thanks to this success I …" consequence clause.
            var allReports = new List<OutcomeReport>(verbReports);
            string narration = await _outcomeNarrator.NarrateOutcomeAsync(
                action, actionModusMentis, successOutcome, true, difficultyScore, ActingMember,
                cancellationToken, outcomeVerbatims: CollectVerbatims(allReports), preview: preview);
            return new ActionExecutionResult
            {
                Action = action,
                ActionModusMentis = actionModusMentis,
                ThinkingModusMentis = thinkingModusMentisUsed,
                Difficulty = difficultyScore,
                DifficultyLevel = difficultyLevel,
                Succeeded = true,
                ActualOutcome = successOutcome,
                LlmDecidedReports = System.Array.Empty<OutcomeReport>(),
                OutcomeReports = allReports,
                Narration = narration,
                FailureWound = null,
                IsPlausibilityFailure = false,
                ItemConsumed = itemConsumed,
            };
        }
        else
        {
            var (failureWound, witnessDetected, detectedWitness, fightTriggered, fightEnemy) =
                ResolveFailureConsequences(evalResult);
            OutcomeBase failureOutcome = new WoundOutcome(failureWound);

            var llmDecidedReports = new List<OutcomeReport>();
            if (failureWound != null)
                llmDecidedReports.Add(new WoundInflictionOutcome(failureWound));

            // Full list = the verb's failure reports followed by the sampled wound. The wound is no
            // longer a separate free-text hint: its Verbatim ("suffered …") lands in the consequence
            // clause like every other report.
            var allReports = new List<OutcomeReport>(verbReports);
            allReports.AddRange(llmDecidedReports);

            string narration = await _outcomeNarrator.NarrateOutcomeAsync(
                action, actionModusMentis, failureOutcome, false, difficultyScore, ActingMember,
                cancellationToken, outcomeVerbatims: CollectVerbatims(allReports), preview: preview);

            return new ActionExecutionResult
            {
                Action = action,
                ActionModusMentis = actionModusMentis,
                ThinkingModusMentis = thinkingModusMentisUsed,
                Difficulty = difficultyScore,
                DifficultyLevel = difficultyLevel,
                Succeeded = false,
                ActualOutcome = failureOutcome,
                LlmDecidedReports = llmDecidedReports,
                OutcomeReports = allReports,
                Narration = narration,
                FailureWound = failureWound,
                IsPlausibilityFailure = false,
                WitnessDetected = witnessDetected,
                DetectedWitness = detectedWitness,
                FightTriggered = fightTriggered,
                FightEnemy = fightEnemy,
                ItemConsumed = itemConsumed,
            };
        }
    }

    /// <summary>The non-empty <see cref="OutcomeReport.Verbatim"/> phrases, in order.</summary>
    private static IReadOnlyList<string> CollectVerbatims(IEnumerable<OutcomeReport> reports)
        => reports.Select(r => r.Verbatim)
                  .Where(v => !string.IsNullOrWhiteSpace(v))
                  .ToList();

    /// <summary>
    /// PHASE 2 (humor-modifier variant): pre-compute BOTH the success and failure outcomes for an
    /// action during the dice animation, generating both narration texts and the failure wound /
    /// witness / threat data, but applying NO game-state side-effects. The caller commits the
    /// chosen branch's side-effects at the dice-roll Continue step (XP, wound reports, item
    /// consumption, witness/threat) based on the final (possibly humor-modified) result.
    /// Item consumption is decided once (it does not depend on success/failure).
    /// </summary>
    public async Task<(ActionExecutionResult success, ActionExecutionResult failure)>
        PrepareDualOutcomesAsync(ActionEvaluationResult evalResult, CancellationToken cancellationToken = default)
    {
        var action = evalResult.Action;
        var actionModusMentis = evalResult.ActionModusMentis;
        var thinkingModusMentisUsed = evalResult.ThinkingModusMentis;
        double difficultyScore = evalResult.DifficultyScore;
        int difficultyLevel = evalResult.DifficultyLevel;
        var currentNode = evalResult.CurrentNode;

        // ── Item consumption decision (independent of the dice outcome) ──
        bool itemConsumed = false;
        if (action.CombinedItem != null)
        {
            string itemContext = $"{action.CombinedItem.DisplayName} ({action.CombinedItem.Description})";
            var goalDesc = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var consumptionCtx = new CriticContext(currentNode, _worldContext, _locationId, goalDesc);
            var consumptionTree = CriticTrees.BuildItemConsumptionTree(action.ActionText, itemContext, consumptionCtx);
            var consumptionResult = await _criticEvaluator.EvaluateTreeAsync(consumptionTree);
            itemConsumed = CriticTrees.IsItemConsumedFromResult(consumptionResult);
        }

        // ── Failure branch data: sampled wound + deterministic witness/threat consequences ──
        var (failureWound, witnessDetected, detectedWitness, fightTriggered, fightEnemy) =
            ResolveFailureConsequences(evalResult);
        OutcomeBase failureOutcome = new WoundOutcome(failureWound);

        var llmDecidedReports = new List<OutcomeReport>();
        if (failureWound != null)
            llmDecidedReports.Add(new WoundInflictionOutcome(failureWound));

        OutcomeBase successOutcome = action.PreselectedOutcome;

        // ── Generate both narration texts (snapshot/restore keeps the slot history clean) ──
        // No verb reports are pre-gathered on this legacy dual path, so only the failure branch's
        // wound contributes a consequence verbatim.
        var (successNarration, failureNarration) = await _outcomeNarrator.NarrateBothOutcomesAsync(
            action, actionModusMentis, successOutcome, failureOutcome,
            difficultyScore, ActingMember,
            successVerbatims: System.Array.Empty<string>(),
            failureVerbatims: CollectVerbatims(llmDecidedReports),
            cancellationToken);

        var success = new ActionExecutionResult
        {
            Action = action,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
            Difficulty = difficultyScore,
            DifficultyLevel = difficultyLevel,
            Succeeded = true,
            ActualOutcome = successOutcome,
            LlmDecidedReports = System.Array.Empty<OutcomeReport>(),
            Narration = successNarration,
            FailureWound = null,
            IsPlausibilityFailure = false,
            ItemConsumed = itemConsumed,
        };

        var failure = new ActionExecutionResult
        {
            Action = action,
            ActionModusMentis = actionModusMentis,
            ThinkingModusMentis = thinkingModusMentisUsed,
            Difficulty = difficultyScore,
            DifficultyLevel = difficultyLevel,
            Succeeded = false,
            ActualOutcome = failureOutcome,
            LlmDecidedReports = llmDecidedReports,
            Narration = failureNarration,
            FailureWound = failureWound,
            IsPlausibilityFailure = false,
            WitnessDetected = witnessDetected,
            DetectedWitness = detectedWitness,
            FightTriggered = fightTriggered,
            FightEnemy = fightEnemy,
            ItemConsumed = itemConsumed,
        };

        return (success, failure);
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
        var rng = GameRng.Stream("dice");
        int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel());
        int sixes = 0;
        for (int i = 0; i < numberOfDice; i++)
            if (rng.Next(1, 7) == 6) sixes++;
        bool succeeded = sixes >= evalResult.DifficultyLevel;
        
        // Phase 2: Execute dice roll and get outcome
        return await ExecuteDiceRollAsync(evalResult, succeeded, cancellationToken);
    }

    /// <summary>
    /// Resolves the consequences of a failed action deterministically (no LLM):
    ///   • a sampled physical penalty from the verb's <see cref="Verb.FailurePenalties"/> (wound or none);
    ///   • whether a witness catches the failed illegal action (effective-Audio proximity);
    ///   • whether a nearby enemy starts a fight (any effective proximity).
    /// Discreteness of the action modus mentis downgrades witness/threat proximity one step
    /// (see <see cref="Cathedral.Game.Scene.ProximityModel"/>). Effective-Visual cases are already
    /// blocked by the coded rules before execution, so only Audio (and, for combat verbs, Visual
    /// threat) reach here.
    /// </summary>
    private (WoundInstance? wound, bool witnessDetected, Cathedral.Game.Npc.NpcEntity? detectedWitness,
             bool fightTriggered, Cathedral.Game.Npc.NpcEntity? fightEnemy)
        ResolveFailureConsequences(ActionEvaluationResult evalResult)
    {
        var action = evalResult.Action;
        bool discrete = evalResult.ActionModusMentis?.ActsDiscretely ?? false;

        // Verb-authored penalty (wound or none), sampled uniformly. Wrapped as an instance stamped
        // with today's date: this happened during the run, so it is a wound that can heal — unlike
        // the historical ones a character was generated with.
        var target = action.PreselectedOutcome?.VerbView.Target;
        var template = action.Verb.SampleFailurePenalty(target, _rng);
        WoundInstance? wound = template != null ? WoundInstance.Inflicted(template) : null;
        Console.WriteLine(wound != null
            ? $"💥 [FAILURE PENALTY] {wound.WoundName} ({WoundLocationLabel(wound)}, {wound.Handicap})"
            : "💥 [FAILURE PENALTY] no injury");

        // Witness (illegal action): effective-Audio + failure ⇒ caught red-handed.
        bool witnessDetected = false;
        Cathedral.Game.Npc.NpcEntity? detectedWitness = null;
        var effWitness = Cathedral.Game.Scene.ProximityModel.Effective(evalResult.WitnessContext.Type, discrete);
        if (effWitness == Cathedral.Game.Scene.WitnessType.Audio)
        {
            witnessDetected = true;
            detectedWitness = evalResult.WitnessContext.Witness;
            Console.WriteLine($"👁 [WITNESS] failed within earshot — caught red-handed by {detectedWitness?.DisplayName ?? "someone"}.");
        }

        // Threat (enemy): any effective proximity + failure ⇒ fight (enemy initiative set by caller).
        bool fightTriggered = false;
        Cathedral.Game.Npc.NpcEntity? fightEnemy = null;
        var effThreat = Cathedral.Game.Scene.ProximityModel.Effective(evalResult.ThreatContext.Level, discrete);
        if (effThreat != Cathedral.Game.Scene.ThreatLevel.None)
        {
            fightTriggered = true;
            fightEnemy = evalResult.ThreatContext.Threat;
            Console.WriteLine($"⚔ [THREAT] failed under threat — fight triggered with {fightEnemy?.DisplayName ?? "the enemy"}.");
        }

        return (wound, witnessDetected, detectedWitness, fightTriggered, fightEnemy);
    }

    /// <summary>Returns a readable location label for a wound, using WildcardZoneHint as fallback.</summary>
    private static string WoundLocationLabel(WoundInstance wound)
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
        CancellationToken cancellationToken,
        ILlmPreviewSink? preview = null)
    {
        // Generate narration explaining why the action is not possible
        var failureOutcome = new HumorOutcome("Melancholia", 1, "inability to act");

        string narration = await _outcomeNarrator.NarratePlausibilityFailureAsync(
            action,
            actionModusMentis,
            plausibilityError,
            ActingMember,
            cancellationToken,
            preview);
        
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

    /// <summary>
    /// Reports produced by LLM-decided outcomes (wound on failure).
    /// Verb-specific reports come from <c>verb.SuccessReports()</c> / <c>verb.FailureReports()</c>
    /// and are built separately in NarrativeController.
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<OutcomeReport> LlmDecidedReports { get; set; }
        = System.Array.Empty<OutcomeReport>();

    /// <summary>
    /// The full, ordered outcome-report list (verb reports + LLM-decided wound) gathered up-front so
    /// their <see cref="OutcomeReport.Verbatim"/> phrases can be woven into the outcome narration.
    /// When non-null, <c>CommitOutcomeResult</c> applies exactly these reports rather than
    /// re-gathering them, so each report — and any item factory it carries — is realised once.
    /// Null for paths that still gather at commit time (e.g. Get-Up).
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<OutcomeReport>? OutcomeReports { get; set; }

    public string Narration { get; set; } = "";
    
    /// <summary>
    /// The wound inflicted on the protagonist if action failed with a physical injury (null otherwise).
    /// </summary>
    public WoundInstance? FailureWound { get; set; }
    
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
    /// When a combined item was used, whether the LLM decided it should be consumed.
    /// Applied lazily at the dice-roll Continue step (so it commits only for the chosen outcome).
    /// </summary>
    public bool ItemConsumed { get; set; }

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
