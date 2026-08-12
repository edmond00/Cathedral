using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Rules;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Headless replay of a recorded <see cref="Routine"/> — no UI, no LLM, no critic, no dice.
///
/// <para><b>Virtual replay</b> validates that every step COULD execute against a freshly built scene
/// without mutating real game state (used to grey out unreplayable routines).</para>
///
/// <para><b>Full replay</b> consumes the recorded constraints and applies each verb's success reports
/// for real, collecting outcomes and deriving the final <see cref="PhaseTransition"/>.</para>
///
/// The caller supplies a <paramref name="buildScene"/> delegate so scene construction (factory choice,
/// existing location state, default-enemy flags) stays owned by the game controller and is identical
/// to what a normal narration start would produce.
/// </summary>
public class RoutineReplayEngine
{
    public RoutineReplayResult VirtualReplay(Routine routine, Protagonist protagonist, Func<Scene.Scene> buildScene)
        => Replay(routine, protagonist, buildScene, dryRun: true);

    public RoutineReplayResult FullReplay(Routine routine, Protagonist protagonist, Func<Scene.Scene> buildScene)
        => Replay(routine, protagonist, buildScene, dryRun: false);

    private RoutineReplayResult Replay(Routine routine, Protagonist protagonist, Func<Scene.Scene> buildScene, bool dryRun)
    {
        var result = new RoutineReplayResult();

        Scene.Scene scene;
        try { scene = buildScene(); }
        catch (Exception ex)
        {
            result.Replayable = false;
            result.FailReason = $"Scene could not be built: {ex.Message}";
            return result;
        }

        // In virtual replay, picking verbs must validate without mutating real state (inventory,
        // depletion timestamps). Full replay leaves this false so picks consume for real.
        scene.IsVirtualReplay = dryRun;

        var firstArea = scene.AllAreas.FirstOrDefault();
        if (firstArea == null)
        {
            result.Replayable = false;
            result.FailReason = "Scene has no areas.";
            return result;
        }

        var pov = new PoV(firstArea, routine.StartTime);
        var ctx = new RoutineReplayContext(scene, pov, protagonist, dryRun);

        for (int i = 0; i < routine.Steps.Count; i++)
        {
            var step = routine.Steps[i];

            // 1. Resolve the live target in the fresh scene.
            var target = RoutineTargetResolver.Resolve(scene, pov, step.Target);
            if (target == null)
            {
                Fail(result, i, $"'{step.Target.DisplayName}' is no longer present.");
                return result;
            }

            // 2. Resolve the verb instance for this scene.
            var verb = scene.Verbs.FirstOrDefault(v => v.VerbId == step.VerbId);
            if (verb == null)
            {
                Fail(result, i, $"Verb '{step.VerbId}' is unavailable here.");
                return result;
            }

            // 3. Bind acting member first (other constraints reference ctx.ActingMember), then check all.
            ctx.ActingMember = protagonist;
            foreach (var c in step.Constraints.Where(c => c.Kind == "acting_member"))
            {
                if (!c.IsSatisfied(ctx)) { Fail(result, i, "The recorded party member is missing."); return result; }
            }
            foreach (var c in step.Constraints.Where(c => c.Kind != "acting_member"))
            {
                if (!c.IsSatisfied(ctx)) { Fail(result, i, ConstraintFailReason(c)); return result; }
            }

            // 4. Verb-level possibility against the live scene/pov.
            // The acting member itself, not a downcast of it: a companion is a PartyMember and used
            // to arrive here as null, which skipped every actor-dependent gate — including, now, the
            // anatomy one that decides whether this body can perform the step at all.
            if (!verb.IsPossible(scene, pov, target, ctx.ActingMember))
            {
                Fail(result, i, $"\"{step.DisplayLabel}\" is not possible here.");
                return result;
            }

            // 4b. Coded rules (legality, witnesses, inventory capacity, …) — the same deterministic
            //     gate narration applies. A failure here marks the routine unreplayable (greyed out).
            var ruleResult = CheckCodedRules(scene, pov, ctx, verb, target, step);
            if (!ruleResult.Passed)
            {
                Fail(result, i, ruleResult.ErrorMessage ?? "VerbAction is not allowed here.");
                return result;
            }

            // 5. Commit the step. Constraints consume (real or ledger-only); verb reports advance the
            //    disposable scene/pov — including where and when it is, so a movement or time-shift
            //    step re-gates every later step's IsPossible through the same GetNpcsAt(Where, When)
            //    query narration uses. NOTE: gather mutates the acting member's inventory, and picking
            //    is only made safe by scene.IsVirtualReplay above; a recordable verb whose reports
            //    touch the member some other way will need per-report dry-run isolation here.
            foreach (var c in step.Constraints) c.Consume(ctx);

            // Rebuild the exact view the player chose (e.g. which job) so variant-aware verbs like
            // RequestJobVerb re-run their side effects (PendingJobOffer) during replay.
            object? variant = string.IsNullOrEmpty(step.VariantKey)
                ? null
                : verb.ResolveRoutineVariant(step.VariantKey);
            var replayView = new VerbAction(verb, step.Verbatim, target, variant);

            var reports = verb.SuccessReports(scene, pov, ctx.ActingMember, target, replayView);
            foreach (var report in reports)
            {
                report.ApplyTo(OutcomeContext.For(ctx.ActingMember, scene, pov));
                if (!dryRun && report.ShowInUI) result.Outcomes.Add(report);
            }

            if (!dryRun)
            {
                foreach (var c in step.Constraints.Where(c => c.ShowInOutcome))
                    result.ExtraLines.Add(c.OutcomeText);
            }
        }

        result.Replayable = true;
        if (!dryRun)
            result.FinalTransition = DeriveFinalTransition(routine, scene, pov, protagonist, result);
        return result;
    }

    private static void Fail(RoutineReplayResult result, int stepIndex, string reason)
    {
        result.Replayable      = false;
        result.FailedStepIndex = stepIndex;
        result.FailReason      = reason;
    }

    /// <summary>
    /// Runs the same coded action rules narration applies, by reconstructing a minimal action context
    /// from the routine step (verb + target + recorded modus mentis) and the live scene/pov.
    /// </summary>
    private static ActionRuleResult CheckCodedRules(Scene.Scene scene, PoV pov, RoutineReplayContext ctx,
        Verb verb, Element target, RoutineStep step)
    {
        var verbView = new VerbAction(verb, verb.Verbatim(scene, pov, target), target);
        var action = new ParsedNarrativeAction
        {
            PreselectedOutcome  = verbView,
            // Recorded on the step rather than held as a constraint: the rules read morality off it
            // when the actor still has it, and simply see null when they do not.
            ActionModusMentisId = step.ActionModusMentisId,
        };

        bool illegal = verb.IsIllegal(scene, pov, target, ctx.ActingMember);
        var witness  = illegal ? WitnessSelector.ComputeContext(scene, pov) : WitnessContext.None;
        var threat   = ThreatSelector.ComputeContext(scene, pov, ctx.Protagonist);

        var ruleCtx = new ActionRuleContext(action, ctx.ActingMember, scene, pov, witness, threat);
        return ActionRulesChecker.Check(ruleCtx);
    }

    private static string ConstraintFailReason(RoutineConstraint c) => c switch
    {
        ItemConstraint ic        => $"Missing required item: {ic.ItemName}.",
        _                        => "A required condition is no longer met.",
    };

    /// <summary>
    /// Derives the post-replay phase from the last step's recorded trigger plus any pending request
    /// the verb left on the scene (fight/dialogue).
    ///
    /// <para>The handed-on time period is <c>pov.When</c>, not <c>routine.StartTime</c>: replay begins
    /// at the recorded arrival time but a time-shifting step (waiting out the morning) moves it on,
    /// exactly as a movement step moves <c>pov.Where</c>. Both are read off the PoV the replay
    /// actually ended at, so whatever opens next opens where and when the routine left off.</para>
    ///
    /// A dialogue trigger is resolved by the tree's
    /// <see cref="DialogueRoutineBehavior"/>: <b>IncludeTrigger</b> reopens the dialogue live, while
    /// <b>IncludeSuccess</b> bakes the dialogue's success in (applying its outcomes here) and opens the
    /// follow-on trade/work phase directly.
    /// </summary>
    private static PhaseTransition DeriveFinalTransition(Routine routine, Scene.Scene scene, PoV pov,
        Protagonist protagonist, RoutineReplayResult result)
    {
        if (scene.PendingFightRequest != null)
            return new StartFightTransition(scene.PendingFightRequest.Npc,
                $"attack on {scene.PendingFightRequest.Npc.DisplayName}");

        if (scene.PendingDialogueRequest != null)
        {
            var req      = scene.PendingDialogueRequest;
            var treeId   = req.TreeId ?? "";
            var tree     = DialogueTreeRegistry.Instance.TryGet(treeId);
            var behavior = tree?.RoutineBehavior ?? DialogueRoutineBehavior.Interrupt;

            if (behavior == DialogueRoutineBehavior.IncludeSuccess && tree != null)
            {
                // Apply the dialogue's success outcome set (which sets TradeRequest / JobRequest),
                // surface its chips, then open the follow-on phase directly.
                string partyMemberId = protagonist.AffinityKey;
                foreach (var oc in tree.SuccessOutcomes)
                {
                    oc.ApplyTo(OutcomeContext.ForDialogue(req.Npc, partyMemberId, protagonist));
                    result.Outcomes.Add(oc);
                }

                if (req.Npc.TradeRequest != TradeMode.None)
                {
                    var mode = req.Npc.TradeRequest;
                    req.Npc.TradeRequest = TradeMode.None;   // consume the transient flag
                    return new StartRoutineTradeTransition(routine.LocationId, req.Npc.DisplayName, mode, pov.When);
                }
                if (req.Npc.JobRequest is { } job)
                {
                    req.Npc.JobRequest      = null;          // consume the transient flags
                    req.Npc.PendingJobOffer = null;
                    return new StartRoutineWorkTransition(routine.LocationId, req.Npc.DisplayName, job.Id, pov.When);
                }

                // Success set neither flag — treat as inert and return to travel rather than hang.
                return ReturnToTravelTransition.Instance;
            }

            // IncludeTrigger (the only other behavior that records the step): reopen the dialogue live.
            return new StartRoutineDialogueTransition(routine.LocationId, req.Npc.DisplayName, treeId, pov.When);
        }

        var last = routine.Steps.LastOrDefault();
        if (last != null && last.TriggeredPhase == RoutinePhaseKind.Narration)
            return new StartNarrationTransition(routine.LocationId, pov.Where, pov.When);

        return ReturnToTravelTransition.Instance;
    }
}
