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
    /// <see cref="ActionExecutionController.ExecuteDiceRollAsync"/> can resolve witness detection on
    /// failure without needing the scene again.
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
    /// party member (after a "Speak About"), so skill resolution, organ scores, XP and wounds all
    /// operate on whoever is actually acting.
    /// </summary>
    public PartyMember ActingMember { get; set; }
    private readonly ItemUseCritic _criticEvaluator;
    private readonly WorldContext _worldContext;
    private readonly int _locationId;
    private readonly Random _rng = GameRng.Stream("action-execution");

    /// <summary>Exposes the outcome narrator for item combination failure narration.</summary>
    public OutcomeNarrator OutcomeNarrator => _outcomeNarrator;

    /// <summary>Exposes the item-use critic for the item-appropriateness judgement.</summary>
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
    /// <paramref name="witnessContext"/> and <paramref name="threatContext"/> are carried through
    /// untouched, to be read on the failure path by <see cref="ResolveFailureConsequences"/>.
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
            : Math.Clamp(action.Verb.DifficultyFor(action.PreselectedOutcome?.Target), 1, 10);
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
        INarratable actualOutcome;
        var consequences = default(FailureConsequences);

        if (succeeded)
        {
            actualOutcome = action.PreselectedOutcome;
        }
        else
        {
            consequences  = ResolveFailureConsequences(evalResult);
            actualOutcome = consequences.Wound != null
                ? new WoundInflictionOutcome(consequences.Wound)
                : (INarratable)new InlineNarratable("No wound", "escaped without injury");
        }
        var failureWound = consequences.Wound;

        // Wound-infliction report (failure only). Verb-specific reports are built later in
        // NarrativeController via SuccessReports()/FailureReports().
        var llmDecidedReports = new System.Collections.Generic.List<Outcome>();
        if (!succeeded && failureWound != null)
            llmDecidedReports.Add(new WoundInflictionOutcome(failureWound));

        // +1 XP for every modusMentis in the action chain (observation → thinking → action), as a
        // report rather than a bare award so each one shows the player a chip. The caller applies it.
        if (succeeded)
            foreach (var chainModusMentis in action.GetModusMentisChain())
            {
                var practice = ModusMentisPracticeOutcome.For(ActingMember, chainModusMentis);
                if (practice != null) llmDecidedReports.Add(practice);
            }

        // A combined implement survives being used. Nothing combinable is a consumable — see the
        // note where the consumption tree used to be, in CriticTrees.

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
            CaughtByWitness = consequences.CaughtBy,
            FightWithEnemy  = consequences.FightWith,
            NpcDrawnIn      = consequences.DrawnIn,
        };
    }

    /// <summary>
    /// PHASE 2 (post-dice variant): compute ONLY the actual outcome once the final (possibly
    /// humor-modified) success/failure is known, streaming its narration into <paramref name="preview"/>
    /// like every other text. No game-state side-effects are applied here; the caller commits them on
    /// the preview's CONTINUE. The failure branch also samples the wound and resolves witness/threat
    /// consequences.
    /// </summary>
    public async Task<ActionExecutionResult> PrepareSingleOutcomeAsync(
        ActionEvaluationResult evalResult, bool succeeded,
        IReadOnlyList<Outcome> verbReports,
        ILlmPreviewSink? preview = null,
        CancellationToken cancellationToken = default)
    {
        var action = evalResult.Action;
        var actionModusMentis = evalResult.ActionModusMentis;
        var thinkingModusMentisUsed = evalResult.ThinkingModusMentis;
        double difficultyScore = evalResult.DifficultyScore;
        int difficultyLevel = evalResult.DifficultyLevel;
        var currentNode = evalResult.CurrentNode;

        if (succeeded)
        {
            INarratable successOutcome = action.PreselectedOutcome;

            // The full report list is exactly the verb's success reports; their verbatims become the
            // "Thanks to this success I …" consequence clause.
            var allReports = new List<Outcome>(verbReports);
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
                LlmDecidedReports = System.Array.Empty<Outcome>(),
                OutcomeReports = allReports,
                Narration = narration,
                FailureWound = null,
                IsPlausibilityFailure = false,
            };
        }
        else
        {
            var consequences = ResolveFailureConsequences(evalResult);
            var failureWound = consequences.Wound;
            // One object, both roles: the report IS the narratable. These used to be two classes
            // built side by side from the same wound.
            var woundReport = failureWound != null ? new WoundInflictionOutcome(failureWound) : null;
            INarratable failureOutcome = woundReport
                ?? (INarratable)new InlineNarratable("No wound", "escaped without injury");

            var llmDecidedReports = new List<Outcome>();
            if (woundReport != null) llmDecidedReports.Add(woundReport);

            // Full list = the verb's failure reports followed by the sampled wound. The wound is no
            // longer a separate free-text hint: its Verbatim ("suffered …") lands in the consequence
            // clause like every other report.
            var allReports = new List<Outcome>(verbReports);
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
                CaughtByWitness = consequences.CaughtBy,
                FightWithEnemy  = consequences.FightWith,
                NpcDrawnIn      = consequences.DrawnIn,
            };
        }
    }

    /// <summary>The non-empty <see cref="Outcome.Verbatim"/> phrases, in order.</summary>
    private static IReadOnlyList<string> CollectVerbatims(IEnumerable<Outcome> reports)
        => reports.Select(r => r.Verbatim)
                  .Where(v => !string.IsNullOrWhiteSpace(v))
                  .ToList();

    /// <summary>
    /// PHASE 2 (humor-modifier variant): pre-compute BOTH the success and failure outcomes for an
    /// action during the dice animation, generating both narration texts and the failure wound /
    /// witness / threat data, but applying NO game-state side-effects. The caller commits the
    /// chosen branch's side-effects at the dice-roll Continue step (XP, wound reports,
    /// witness/threat) based on the final (possibly humor-modified) result.
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

        // ── Failure branch data: sampled wound + deterministic witness/threat consequences ──
        var consequences = ResolveFailureConsequences(evalResult);
        var failureWound = consequences.Wound;
        var woundReport = failureWound != null ? new WoundInflictionOutcome(failureWound) : null;
        INarratable failureOutcome = woundReport
            ?? (INarratable)new InlineNarratable("No wound", "escaped without injury");

        var llmDecidedReports = new List<Outcome>();
        if (woundReport != null) llmDecidedReports.Add(woundReport);

        INarratable successOutcome = action.PreselectedOutcome;

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
            LlmDecidedReports = System.Array.Empty<Outcome>(),
            Narration = successNarration,
            FailureWound = null,
            IsPlausibilityFailure = false,
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
            CaughtByWitness = consequences.CaughtBy,
            FightWithEnemy  = consequences.FightWith,
            NpcDrawnIn      = consequences.DrawnIn,
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
        int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel(ActingMember));
        int sixes = 0;
        for (int i = 0; i < numberOfDice; i++)
            if (rng.Next(1, 7) == 6) sixes++;
        bool succeeded = sixes >= evalResult.DifficultyLevel;
        
        // Phase 2: Execute dice roll and get outcome
        return await ExecuteDiceRollAsync(evalResult, succeeded, cancellationToken);
    }

    /// <summary>
    /// What a failed action costs, decided deterministically (no LLM). At most one of the three
    /// social consequences can be set: they are three rungs of one ladder, not a set of flags.
    /// </summary>
    /// <param name="Wound">Sampled from the verb's <see cref="Verb.FailurePenalties"/>; often none.</param>
    /// <param name="CaughtBy">A witness who <b>saw</b> it: the caught-red-handed confrontation.</param>
    /// <param name="FightWith">An enemy who <b>saw</b> it: the fight, on their initiative.</param>
    /// <param name="DrawnIn">
    /// Somebody a room away who <b>heard</b> it and is coming to look — witness or enemy alike. They
    /// move into the area and open the next observation phase; nothing else happens yet, which is
    /// what leaves room to run.
    /// </param>
    private readonly record struct FailureConsequences(
        WoundInstance?                  Wound,
        Cathedral.Game.Npc.NpcEntity?   CaughtBy,
        Cathedral.Game.Npc.NpcEntity?   FightWith,
        Cathedral.Game.Npc.NpcEntity?   DrawnIn);

    /// <summary>
    /// Resolves what a failed action costs. The physical penalty is a verb-authored roll; the social
    /// consequence is read off <b>effective</b> proximity
    /// (<see cref="Cathedral.Game.Scene.ProximityModel"/>), which is where the acting modus mentis's
    /// discreteness applies:
    ///
    /// <list type="bullet">
    /// <item>effective <b>Visual</b> — they are in the room and watched it go wrong. A witness
    ///   confronts you; an enemy simply attacks. Only a discrete modus mentis (or a combat verb) can
    ///   reach this at all, since the coded rules stop anyone else from trying in front of somebody.</item>
    /// <item>effective <b>Audio</b> — they heard it from the next room and come to look. No
    ///   confrontation yet: they arrive, the next observation phase opens on them, and from then on
    ///   they are a Visual presence and the door is no longer a free exit.</item>
    /// <item>effective <b>None</b> — nobody is any the wiser. This is what discreteness buys at a
    ///   distance: a quiet failure a room away is not heard at all.</item>
    /// </list>
    ///
    /// <para>The enemy and witness ladders are deliberately identical in shape and differ only in what
    /// the top rung is — a fight against somebody who already wants one, a conversation against
    /// somebody who has just discovered they might.</para>
    /// </summary>
    private FailureConsequences ResolveFailureConsequences(ActionEvaluationResult evalResult)
    {
        var action = evalResult.Action;
        bool discrete = evalResult.ActionModusMentis?.ActsDiscretely ?? false;

        // Verb-authored penalty (wound or none), sampled uniformly. Wrapped as an instance stamped
        // with today's date: this happened during the run, so it is a wound that can heal — unlike
        // the historical ones a character was generated with.
        var target = action.PreselectedOutcome?.Target;
        var template = action.Verb.SampleFailurePenalty(target, _rng);

        // Verbs author their penalties as HUMAN wounds — a turned ankle, a cut hand, a broken foot —
        // and the acting member need not be human: a beast narrates and acts for itself after a
        // Speak-About hand-off. A wound the body does not own penalises nothing (every Affects*
        // query misses an organ part the anatomy lacks) and is captured into the save verbatim,
        // where PartyState.Rebuild refuses it and loses the run. So the miss costs no injury rather
        // than an injury that is not there. Deliberately not translated to a beast equivalent: the
        // verb named one wound, and inventing a counterpart for it is content, not plumbing.
        if (template != null && !WoundRegistry.CanBeSufferedBy(template, ActingMember))
        {
            Console.WriteLine($"💥 [FAILURE PENALTY] {template.WoundName} is not a wound "
                + $"{ActingMember.DisplayName} ({ActingMember.AnatomyType}) can suffer — no injury.");
            template = null;
        }

        WoundInstance? wound = template != null ? WoundInstance.Inflicted(template) : null;
        Console.WriteLine(wound != null
            ? $"💥 [FAILURE PENALTY] {wound.WoundName} ({WoundLocationLabel(wound)}, {wound.Handicap})"
            : "💥 [FAILURE PENALTY] no injury");

        // An enemy in the room outranks a witness in the room: the quarrel is already declared, and
        // being asked to explain yourself by somebody drawing steel is not a conversation.
        var effThreat  = Cathedral.Game.Scene.ProximityModel.Effective(evalResult.ThreatContext.Level, discrete);
        var effWitness = Cathedral.Game.Scene.ProximityModel.Effective(evalResult.WitnessContext.Type, discrete);

        if (effThreat == Cathedral.Game.Scene.ThreatLevel.Visual)
        {
            var enemy = evalResult.ThreatContext.Threat;
            Console.WriteLine($"⚔ [THREAT] failed in front of the enemy — fight with {enemy?.DisplayName ?? "them"}.");
            return new FailureConsequences(wound, null, enemy, null);
        }

        if (effWitness == Cathedral.Game.Scene.WitnessType.Visual)
        {
            var witness = evalResult.WitnessContext.Witness;
            Console.WriteLine($"👁 [WITNESS] failed in plain sight — caught red-handed by {witness?.DisplayName ?? "someone"}.");
            return new FailureConsequences(wound, witness, null, null);
        }

        // Heard, not seen. Whoever is nearest comes to look; the enemy first, for the same reason.
        var heardBy = effThreat  == Cathedral.Game.Scene.ThreatLevel.Audio  ? evalResult.ThreatContext.Threat
                    : effWitness == Cathedral.Game.Scene.WitnessType.Audio  ? evalResult.WitnessContext.Witness
                    : null;
        if (heardBy != null)
            Console.WriteLine($"👂 [EARSHOT] failed within earshot — {heardBy.DisplayName} comes to look.");

        return new FailureConsequences(wound, null, null, heardBy);
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
        // A marker for "the action was never attempted" — nothing here changes the world, and the
        // narration comes from NarratePlausibilityFailureAsync below rather than from this. It used
        // to claim a Melancholia +1 that no report ever applied.
        var failureOutcome = new InlineNarratable("Not attempted", "thought better of it");

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
    public INarratable ActualOutcome { get; set; } = null!;

    /// <summary>
    /// Reports produced by LLM-decided outcomes (wound on failure).
    /// Verb-specific reports come from <c>verb.SuccessReports()</c> / <c>verb.FailureReports()</c>
    /// and are built separately in NarrativeController.
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Outcome> LlmDecidedReports { get; set; }
        = System.Array.Empty<Outcome>();

    /// <summary>
    /// The full, ordered outcome-report list (verb reports + LLM-decided wound) gathered up-front so
    /// their <see cref="Outcome.Verbatim"/> phrases can be woven into the outcome narration.
    /// When non-null, <c>CommitOutcomeResult</c> applies exactly these reports rather than
    /// re-gathering them, so each report — and any item factory it carries — is realised once.
    /// Null for paths that still gather at commit time (e.g. Get-Up).
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Outcome>? OutcomeReports { get; set; }

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
    /// The witness who <b>saw</b> a failed crime, if any — the caught-red-handed confrontation.
    /// Null on success: nothing is ever confronted for succeeding.
    /// </summary>
    public Cathedral.Game.Npc.NpcEntity? CaughtByWitness { get; set; }

    /// <summary>
    /// The enemy who <b>saw</b> a failed action, if any — the fight, on their initiative.
    /// </summary>
    public Cathedral.Game.Npc.NpcEntity? FightWithEnemy { get; set; }

    /// <summary>
    /// Somebody a room away who <b>heard</b> a failed action and is coming to look. They move into
    /// the area and open the next observation phase; the confrontation, if any, comes later.
    /// </summary>
    public Cathedral.Game.Npc.NpcEntity? NpcDrawnIn { get; set; }
}
