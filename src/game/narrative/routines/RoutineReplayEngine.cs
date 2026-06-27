using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;
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
            if (!verb.IsPossible(scene, pov, target, ctx.ActingMember as Protagonist))
            {
                Fail(result, i, $"\"{step.Verbatim}\" is not possible here.");
                return result;
            }

            // 5. Commit the step. Constraints consume (real or ledger-only); verb reports advance the
            //    disposable scene/pov. NOTE: today's only recordable verb (move) confines its reports to
            //    the PoV. When recordable verbs that mutate the acting member are added, virtual replay
            //    will need per-report dry-run isolation here.
            foreach (var c in step.Constraints) c.Consume(ctx);

            var reports = verb.SuccessReports(scene, pov, ctx.ActingMember, target);
            foreach (var report in reports)
            {
                report.Apply(ctx.ActingMember, scene, pov);
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
            result.FinalTransition = DeriveFinalTransition(routine, scene, pov);
        return result;
    }

    private static void Fail(RoutineReplayResult result, int stepIndex, string reason)
    {
        result.Replayable      = false;
        result.FailedStepIndex = stepIndex;
        result.FailReason      = reason;
    }

    private static string ConstraintFailReason(RoutineConstraint c) => c switch
    {
        ItemConstraint ic        => $"Missing required item: {ic.ItemName}.",
        ModusMentisConstraint    => "A required modus mentis has been forgotten.",
        _                        => "A required condition is no longer met.",
    };

    /// <summary>
    /// Derives the post-replay phase from the last step's recorded trigger plus any pending request
    /// the verb left on the scene (fight/dialogue).
    /// </summary>
    private static PhaseTransition DeriveFinalTransition(Routine routine, Scene.Scene scene, PoV pov)
    {
        if (scene.PendingFightRequest != null)
            return new StartFightTransition(scene.PendingFightRequest.Npc,
                $"attack on {scene.PendingFightRequest.Npc.DisplayName}");

        if (scene.PendingDialogueRequest != null)
            return new StartDialogueTransition(scene.PendingDialogueRequest.Npc,
                scene.PendingDialogueRequest.TreeId);

        var last = routine.Steps.LastOrDefault();
        if (last != null && last.TriggeredPhase == RoutinePhaseKind.Narration)
            return new StartNarrationTransition(routine.LocationId, pov.Where, routine.StartTime);

        return ReturnToTravelTransition.Instance;
    }
}
