using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Preview;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Drives the thinking Chain-of-Thought. The two LLM *decisions* are preserved as constrained
/// choices (which goal/sub-outcome to pursue, which action skill to use); the *flavor* — the
/// reasoning block and the concrete action text — is produced by building neutral meaning text
/// (<see cref="NeutralNarration"/>) and re-expressing it in persona voice via
/// <see cref="PersonaRewriter"/>. In playground mode the decisions are made heuristically and the
/// flavor falls back to the neutral text.
/// </summary>
public class ThinkingExecutor
{
    private readonly LlamaServerManager _llmManager;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly PersonaRewriter _rewriter;
    private readonly PersonaChoiceSelector _selector;

    /// <param name="promptConstructor">Retained for API compatibility; the thinking prompts now use
    /// <see cref="ThinkingPromptConstructor"/>'s static helpers directly, so no instance is stored.</param>
    public ThinkingExecutor(
        LlamaServerManager llmManager,
        ThinkingPromptConstructor promptConstructor,
        ModusMentisSlotManager slotManager)
    {
        _llmManager = llmManager;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _rewriter = new PersonaRewriter(llmManager);
        _selector = new PersonaChoiceSelector(llmManager);
    }

    private readonly Random _rng = GameRng.Stream("thinking");

    /// <summary>
    /// GOAL (decision) → optional IGNORE early-exit → HOW (decision) → reasoning rewrite → [doubt] →
    /// willingness → action rewrite. Returns null only if no usable action skill is available.
    ///
    /// <para><b>Iterative thinking.</b> When what the thinking modus mentis proposed comes to nothing —
    /// the action modus mentis refuses it, or the player doubts it — the thinking tries again, up to
    /// <see cref="ThinkingIterationsStat"/> times (the pineal gland's score). A refused goal is tried
    /// again only when another means is able to carry it out (no choice rule refuses it to that means),
    /// and then by one of those, with no refusal left to it: the next try is sure to end on a button.
    /// When no other means could, the goal is given up and the next try chooses another, or the chain
    /// stops when nothing is left. Only what is refused before the click is retried: the action rules
    /// (a witness in sight, an enemy in the room) are the click's, which keeps the final word on them.
    /// The whole chain is one thinking and spends one noetic point. At a score of 0 nothing here runs:
    /// one try, and the refusal is shown as it always was.</para>
    ///
    /// <para><b>Doubt.</b> With <see cref="DoubtStat"/> and a retry left, the reasoning ends on a question
    /// and the preview box asks YES or NO before the action modus mentis is asked or any rule probed. NO
    /// crosses the pair off like a refusal; either answer is then voiced by the thinking modus mentis.</para>
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        ModusMentis thinkingModusMentis,
        NarrativeAnchor targetOutcome,
        string keyword,
        NarrationNode node,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        int locationId,
        PartyMember actingMember,
        Scene.Scene? scene = null,
        PoV? pov = null,
        bool isReminescence = false,
        bool autoSuccess = false,
        LlmPreviewSession? preview = null,
        ActionProposalLedger? proposed = null,
        CancellationToken cancellationToken = default)
    {
        // The reasoning box holds the thinking MM's reasoning rewrite; the action box (a different MM)
        // the action rewrite. The choices behind them are made by the choice judge and show nothing.
        string thinkingTitle = PreviewTitles.For(thinkingModusMentis);
        var thoughtPart = preview?.BeginAccumulatingPart(thinkingTitle);
        PreviewPart? lastPart = thoughtPart;

        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        // Stamp the contextual NPC label for the current narrator's POV before any prompt text is
        // built (this also propagates the label to the target's sub-outcomes / goal phrases).
        (targetOutcome as INpcContextLabelStampable)?.StampContextLabel(actingMember, worldContext, locationId);

        // Sub-outcomes to choose between. "Ignore and move on" is offered by ChooseGoalAsync as the
        // decline option ("walk away and leave it"); the persona choosing it IS the ignore outcome.
        var sourceObs = targetOutcome as ObservationObject;
        var subOutcomes = sourceObs != null
            ? new List<NarrativeAnchor>(sourceObs.SubOutcomes)
            : new List<NarrativeAnchor> { targetOutcome };

        string targetDescription = targetOutcome.ToNaturalLanguageString();

        // Short situational context threaded into every constrained-choice prompt: the overall
        // location (e.g. "a farm"), the specific area within it (e.g. "courtyard"), and what drew
        // the character's attention (the observed object).
        string overallLocation = worldContext.GenerateContextDescription(locationId);
        string areaLocation = node.GenerateNeutralDescription(locationId);
        string? observedPhrase = sourceObs?.NeutralPhrase;

        // The coded choice rules need somewhere to judge from; without a scene (tooling, tests) they
        // are simply not run and every option stands.
        var choiceCtx = scene != null && pov != null
            ? new Rules.Choice.ChoiceRuleContext(scene, pov, actingMember, thinkingModusMentis)
            : null;

        // An auto-success phase (reminescence, get-up) cannot be refused, so it has nothing to retry.
        int retriesLeft = autoSuccess ? 0 : ThinkingIterationsStat.Of(actingMember);
        bool mayDoubt   = !autoSuccess && preview != null && DoubtStat.Of(actingMember);
        var chain       = new ThinkingChain();
        var segments    = new List<ThinkingSegment>();
        if (retriesLeft > 0)
            Console.WriteLine($"ThinkingExecutor: [{thinkingModusMentis.DisplayName}] may try again {retriesLeft} time(s)"
                            + (mayDoubt ? ", and the player may doubt" : "") + ".");

        // What the thinking mind says ahead of its next reasoning: why it is trying again. Null on the
        // first try, which opens on the object instead.
        string? lead = null;

        // A refused goal that another means is able to carry out: the next try takes it up again with
        // one of those (TryAgain).
        VerbAction? again = null;

        // A will is offered no goal but its path's first step (WillPriority), and on an object that
        // does not offer it, or in a place with no way to its goal at all, it proposes nothing and says
        // why. Its goal is not chosen, so there is nothing for a doubt to choose again. Only the act it
        // wants is its own to carry out: a step on the way to it (a walk, a wait, a tool fetched) is
        // carried out by whichever mind the means judge finds fittest, and while a retry remains, a
        // refusal tries the same step with another means.
        Scene.ActionPath? willPath = null;
        VerbAction? willStep = null;
        bool willOnTheWay = false;
        if (scene != null && pov != null && !autoSuccess && Rules.Choice.WillPriority.Steers(thinkingModusMentis, scene))
        {
            var planned = Rules.Choice.WillPriority.PathOf(thinkingModusMentis, scene, pov, actingMember);
            // The path through what this object offers, which need not be the one the plan kept: of
            // two people equally near to strike, either is the way.
            willPath = planned == null ? null
                     : Scene.ActionPathPlanner.PathThrough(scene, pov, actingMember, thinkingModusMentis.Will!, planned,
                                                           subOutcomes.OfType<VerbAction>());
            willStep = willPath == null ? null
                     : subOutcomes.OfType<VerbAction>().FirstOrDefault(v => Scene.ActionPathPlanner.SameAction(v, willPath.First.Action));
            if (willStep == null)
            {
                string refusal = planned == null
                    ? NeutralNarration.WillReasoningNotHere(targetDescription, thinkingModusMentis.Will!.Purpose)
                    : NeutralNarration.WillReasoningNotThisWay(targetDescription, planned, thinkingModusMentis.Will!.Purpose, actingMember);
                Console.WriteLine($"ThinkingExecutor: [{thinkingModusMentis.DisplayName}] wills '{thinkingModusMentis.Will!.Purpose}' — "
                                + (willPath == null ? "no way here." : $"'{targetDescription}' is not on the way."));
                Think(await VoiceAsync(thinkingSlot, thinkingModusMentis, refusal, NarrationKind.Reasoning, thoughtPart, cancellationToken));
                return Finish(new List<ParsedNarrativeAction>());
            }
            willOnTheWay = !thinkingModusMentis.Will!.Matches(willStep);
            Console.WriteLine($"ThinkingExecutor: [{thinkingModusMentis.DisplayName}] wills it — proposing '{willStep.Verbatim}'"
                            + (willOnTheWay ? ", a step on the way: the means is judged." : "."));
            if (!willOnTheWay) retriesLeft = 0;
            mayDoubt = false;
        }

        while (true)
        {
            // ── Decision 1: GOAL ────────────────────────────────────────────────────
            // An auto-success phase (reminescence, get-up) offers no "do something else": its one verb is
            // the only way onward, and a mind that walked away from it left the player where they were.
            bool retryingRefused = again != null;
            var resolved = willStep
                           ?? (NarrativeAnchor?)again
                           ?? await ChooseGoalAsync(subOutcomes, thinkingModusMentis, actionModiMentis, overallLocation, areaLocation, observedPhrase, choiceCtx, proposed, chain, cancellationToken, offerDecline: !autoSuccess);
            // A will's step tried with every means is the end of the chain, as a goal would be crossed off.
            bool isIgnore = resolved is VerbAction vIgnore && vIgnore.Verb is IgnoreVerb
                            || willStep != null && chain.Touched(willStep)
                               && !chain.Untried(willStep, MeansPoolFor(willStep, actionModiMentis)).Any();

            // ── Early exit: IGNORE (reasoning only, no action) ──────────────────────
            // After a retry this is the end of the chain: everything left was tried, or nothing appeals.
            if (isIgnore)
            {
                string ignoreNeutral = lead == null
                    ? NeutralNarration.ReasoningIgnore(targetDescription, isReminescence)
                    : $"{lead} {NeutralNarration.IterationExhausted(targetDescription)}";
                if (lead != null) Console.WriteLine("ThinkingExecutor: nothing left to try — the chain stops.");
                Think(await VoiceAsync(thinkingSlot, thinkingModusMentis, ignoreNeutral, NarrationKind.Reasoning, thoughtPart, cancellationToken));
                return Finish(new List<ParsedNarrativeAction>());
            }

            string goalPhrase = resolved.ToNaturalLanguageString();
            var goalAction = resolved as VerbAction;

            // Within a chain, a goal already tried comes back only with a means not yet tried with it.
            var meansPool = actionModiMentis;
            if (goalAction != null && chain.Touched(goalAction))
            {
                var untried = chain.Untried(goalAction, MeansPoolFor(goalAction, actionModiMentis)).ToList();
                if (untried.Count > 0) meansPool = untried;
            }

            // A second attempt at a goal already refused this phase is not left to be refused again: only
            // the action modi mentis the coded rules let carry it out are offered as the means, and the one
            // chosen is not asked whether it would rather not. ChooseGoalAsync has already withheld the goal
            // when none could, so the narrowed pool is never empty in play; the fallback is for safety.
            // A goal this chain saw refused is the same second attempt, within the thinking.
            again = null;
            bool secondAttempt = choiceCtx != null && goalAction != null
                                 && (retryingRefused || proposed?.WasRefused(goalAction) == true);
            if (secondAttempt)
            {
                var able = meansPool.Where(s => CanCarryOut(s, resolved, choiceCtx!)).ToList();
                if (able.Count > 0) meansPool = able;
                Console.WriteLine($"ThinkingExecutor: '{goalPhrase}' was refused earlier this phase — "
                                + $"offering only the {able.Count} means able to carry it out, with no refusal.");
            }

            // A conversation is carried out by a mind that speaks, when one is held: the means judge is
            // offered only those (SpeakingPriority). Otherwise the speaking action modi mentis, which never
            // refuse a conversation, would be reached only when the judge happened to pick one.
            if (Rules.Choice.SpeakingPriority.IsDialogue(resolved))
            {
                var speakers = meansPool.Where(Rules.Choice.SpeakingPriority.Speaks).ToList();
                if (speakers.Count > 0 && speakers.Count < meansPool.Count)
                {
                    Console.WriteLine($"ThinkingExecutor: '{goalPhrase}' is a conversation — offering only the "
                                    + $"{speakers.Count} speaking means of {meansPool.Count}.");
                    meansPool = speakers;
                }
            }

            // ── Decision 2: HOW (which action skill) ────────────────────────────────
            // A will that bears the Action function carries out the act it wants: the means is not chosen.
            // A step on the way is judged like any goal, the will among the candidates.
            var skill = willStep != null && !willOnTheWay && Rules.Choice.WillPriority.ActsItself(thinkingModusMentis) && actionModiMentis.Contains(thinkingModusMentis)
                ? thinkingModusMentis
                : await ChooseSkillAsync(goalPhrase, meansPool, thinkingModusMentis, overallLocation, areaLocation, observedPhrase, cancellationToken);
            if (skill == null)
            {
                if (actionModiMentis.Count == 0)
                {
                    Console.Error.WriteLine("ThinkingExecutor: no usable action skill for the chosen goal.");
                    preview?.Reset();
                    return null;
                }

                // Skills exist but the thinking Modus Mentis declined every means → "no way to do
                // it": a reasoning-only outcome in the thinking MM's voice, mirroring the ignore branch.
                string noMeansNeutral = NeutralNarration.ReasoningNoMeans(targetDescription, goalPhrase, isReminescence);
                if (lead != null) noMeansNeutral = $"{lead} {noMeansNeutral}";
                Think(await VoiceAsync(thinkingSlot, thinkingModusMentis, noMeansNeutral, NarrationKind.Reasoning, thoughtPart, cancellationToken));
                return Finish(new List<ParsedNarrativeAction>());
            }

            // ── Flavor: reasoning (thinking slot) ───────────────────────────────────
            // Doubt is offered only while a retry remains: a NO with nothing left to try would end the
            // thinking on nothing, which is a refusal the player made for the mind.
            bool doubting = mayDoubt && retriesLeft > 0 && goalAction != null;
            string reasoningNeutral = lead == null
                ? NeutralNarration.ReasoningChain(targetDescription, goalPhrase, skill.SkillMeans, isReminescence)
                : goalAction != null && chain.Touched(goalAction)
                    ? $"{lead} {NeutralNarration.ReasoningRetrySameGoal(goalPhrase, skill.SkillMeans)}"
                    : $"{lead} {NeutralNarration.ReasoningRetry(goalPhrase, skill.SkillMeans)}";
            if (lead == null && willPath is { Steps.Count: > 1 })
                reasoningNeutral = NeutralNarration.WillReasoningChain(targetDescription, willPath, skill.SkillMeans, thinkingModusMentis.Will!.Purpose, actingMember);
            if (doubting) reasoningNeutral += " " + NeutralNarration.DoubtQuestion(goalPhrase);
            Think(await VoiceAsync(thinkingSlot, thinkingModusMentis, reasoningNeutral, NarrationKind.Reasoning, thoughtPart, cancellationToken));
            lead = null;

            // ── Doubt (player) ──────────────────────────────────────────────────────
            if (doubting && thoughtPart != null)
            {
                bool holds = await thoughtPart.AskAsync();
                Console.WriteLine($"ThinkingExecutor: the player answered the doubt — {(holds ? "YES, keep" : "NO, think again")} '{goalPhrase}'.");

                // The answer is voiced in a part of its own: the asking part was committed by the answer.
                thoughtPart = lastPart = preview!.BeginAccumulatingPart(thinkingTitle);
                if (!holds)
                {
                    chain.Drop(goalAction!);
                    retriesLeft--;
                    lead = NeutralNarration.DoubtRejected(goalPhrase);
                    continue;
                }
                Think(await VoiceAsync(thinkingSlot, thinkingModusMentis, NeutralNarration.DoubtConfirmed(goalPhrase), NarrationKind.Reasoning, thoughtPart, cancellationToken));
            }

            // Reasoning is done — make it continue-able now while the action (different MM) streams behind it.
            thoughtPart?.MarkComplete();

            // ── VerbAction skill slot: persona-fit check, then action-text flavor ───────
            int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(skill);
            _llmManager.ResetInstance(actionSlot);

            // The action box (skill MM) holds the action rewrite, or the refusal.
            var actionPart = preview?.BeginAccumulatingPart(PreviewTitles.For(skill));
            lastPart = actionPart ?? lastPart;

            // Persona-fit: how strongly is the skill drawn to this action? Decides possibility + difficulty
            // through the choice judge. Skipped for auto-success phases (reminescence / get-up).
            // The willingness rules judge the action skill against the goal now settled on, so the context
            // is rebuilt around the skill rather than the thinking modus mentis.
            var fitCtx = choiceCtx == null
                ? null
                : choiceCtx with { ModusMentis = skill, Goal = resolved };

            var (fit, imposedReason) = autoSuccess
                ? (PersonaFit.Willing, (string?)null)
                : await AskPersonaFitAsync(skill, goalPhrase, overallLocation, areaLocation, observedPhrase, fitCtx, cancellationToken, mayDecline: !secondAttempt);

            // "unwilling to do it" → the skill refuses; produce a first-person refusal outcome, no action.
            if (fit.Cancels)
            {
                if (goalAction != null)
                    proposed?.Refuse(thinkingModusMentis, goalAction);

                string refusalNeutral = imposedReason != null
                    ? NeutralNarration.ActionRefusal(goalPhrase, imposedReason)
                    : NeutralNarration.ActionRefusal(goalPhrase);
                Refusal(skill, await VoiceAsync(actionSlot, skill, refusalNeutral, NarrationKind.Outcome, actionPart, cancellationToken));
                if (goalAction != null && TryAgain(goalAction, skill, goalPhrase, actionPart)) continue;
                return Finish(new List<ParsedNarrativeAction>());
            }

            // The neutral sentence opens with "I will …" (plus "discretely" for a discrete skill), and the
            // GBNF prefix constraint forces the styled rewrite to open with the same literal. This
            // guarantees the prefix can be stripped cleanly to form the button label (DisplayText);
            // ActionText keeps the canonical "try to …" form the item critic expects.
            string styledAction = await _rewriter.RewriteAsync(
                actionSlot, NeutralNarration.ActionIntent(goalPhrase, skill.ActsDiscretely), NarrationKind.Action, skill.PersonaReminder2, forcedPrefix: ActionPrefix, styleInstruction: skill.StyleInstruction, preview: actionPart?.NextSegment(), ct: cancellationToken);
            if (string.IsNullOrWhiteSpace(styledAction))
                styledAction = ActionPrefix + (skill.ActsDiscretely ? "discretely " : "") + goalPhrase;
            string bareAction = StripPrefix(styledAction, ActionPrefix);

            // Difficulty: verb base ± the persona-fit modifier (eager −1 / willing 0 / reluctant +1),
            // clamped to 1..10. Auto-success phases carry difficulty 0 (rendered with the ○ glyph).
            var verbOutcome = (VerbAction)resolved;
            int difficultyLevel = autoSuccess
                ? 0
                : Math.Clamp(verbOutcome.Verb.DifficultyFor(verbOutcome.Target) + fit.DifficultyModifier, 1, 10);

            var action = new ParsedNarrativeAction
            {
                ActionModusMentisId = skill.ModusMentisId,
                ActionModusMentis   = skill,
                PreselectedOutcome  = verbOutcome,
                ActionText          = $"try to {bareAction}",
                DisplayText         = bareAction,
                NeutralActionText   = goalPhrase,
                ThinkingModusMentis = thinkingModusMentis,
                Keyword             = keyword,
                DifficultyLevel     = difficultyLevel
            };

            return Finish(new List<ParsedNarrativeAction> { action });
        }

        // A refusal crosses the pair off and, while a retry remains, opens the next try: a fresh thinking
        // part, led by the mind's acknowledgement of the refusal. The goal is taken up again when an
        // untried means is able to carry it out; otherwise it is given up with every means, and the
        // next try chooses another goal or finds nothing left.
        bool TryAgain(VerbAction goal, ModusMentis means, string goalPhrase, PreviewPart? refusalPart)
        {
            chain.CrossOff(goal, means);
            if (retriesLeft <= 0) return false;
            retriesLeft--;
            var able = chain.Untried(goal, MeansPoolFor(goal, actionModiMentis))
                            .Where(s => choiceCtx == null || CanCarryOut(s, goal, choiceCtx))
                            .ToList();
            if (able.Count > 0)
            {
                again = goal;
                Console.WriteLine($"ThinkingExecutor: {able.Count} other means could '{goalPhrase}' — trying it again with one of them.");
            }
            else
            {
                chain.Drop(goal);
                Console.WriteLine($"ThinkingExecutor: no other means could '{goalPhrase}' — it is given up.");
            }
            // The refusal is shown and continued past like any part; the last part is the caller's to
            // complete, and this one is no longer the last.
            refusalPart?.MarkComplete();
            lead = NeutralNarration.IterationRetry(goalPhrase);
            thoughtPart = preview?.BeginAccumulatingPart(thinkingTitle);
            lastPart = thoughtPart ?? lastPart;
            return true;
        }

        // A thought joins the thinking block in progress; a refusal closes it and is a block of its own.
        void Think(string text)
        {
            if (segments.Count > 0 && segments[^1].IsThought)
                segments[^1] = segments[^1] with { Text = segments[^1].Text + " " + text };
            else
                segments.Add(new ThinkingSegment(thinkingModusMentis, text, IsThought: true));
        }

        void Refusal(ModusMentis by, string text) => segments.Add(new ThinkingSegment(by, text, IsThought: false));

        ThinkingResponse Finish(List<ParsedNarrativeAction> actions)
        {
            var lastThought = segments.LastOrDefault(s => s.IsThought);
            var last        = segments.LastOrDefault();
            return new ThinkingResponse
            {
                Segments           = segments,
                ReasoningText      = lastThought?.Text ?? "",
                Actions            = actions,
                RefusalText        = actions.Count == 0 && last is { IsThought: false } ? last.Text : null,
                RefusalModusMentis = actions.Count == 0 && last is { IsThought: false } ? last.ModusMentis : null,
                PreviewLastPart    = lastPart,
            };
        }
    }

    /// <summary>
    /// Reconsider: an act already offered as a button, thought over again by <paramref name="thinkingModusMentis"/>,
    /// which may keep it or want no part of it but proposes nothing else. Kept, it is given to another
    /// means than the one that carried it — one able to carry it out both before the click (the rules
    /// of choice, <see cref="CanCarryOut"/>) and on it (<paramref name="clickProbe"/>, the action rules a
    /// witness or an enemy in the room answer to), so a crime in sight of a witness goes to a discreet
    /// means when one is held. The chosen means does not decline. No retry and no doubt: one thought.
    ///
    /// <para>The response carries the new act, or none: when the mind refuses, or no other means could
    /// pass, the reasoning says so and the act offered before still stands.</para>
    /// </summary>
    public async Task<ThinkingResponse> ReconsiderAsync(
        ModusMentis thinkingModusMentis,
        ParsedNarrativeAction previous,
        NarrationNode node,
        List<ModusMentis> actionModiMentis,
        WorldContext worldContext,
        int locationId,
        PartyMember actingMember,
        Scene.Scene? scene,
        PoV? pov,
        Func<ParsedNarrativeAction, string?> clickProbe,
        LlmPreviewSession? preview = null,
        CancellationToken cancellationToken = default)
    {
        var thoughtPart = preview?.BeginAccumulatingPart(PreviewTitles.For(thinkingModusMentis));
        PreviewPart? lastPart = thoughtPart;
        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        var goal = previous.PreselectedOutcome;
        string goalPhrase = !string.IsNullOrWhiteSpace(previous.NeutralActionText) ? previous.NeutralActionText : goal.ToNaturalLanguageString();
        string overallLocation = worldContext.GenerateContextDescription(locationId);
        string areaLocation = node.GenerateNeutralDescription(locationId);
        var choiceCtx = scene != null && pov != null
            ? new Rules.Choice.ChoiceRuleContext(scene, pov, actingMember, thinkingModusMentis)
            : null;
        var segments = new List<ThinkingSegment>();

        ThinkingResponse Finish(ParsedNarrativeAction? action) => new()
        {
            Segments = segments,
            ReasoningText = segments.LastOrDefault(s => s.IsThought)?.Text ?? "",
            Actions = action == null ? new List<ParsedNarrativeAction>() : new List<ParsedNarrativeAction> { action },
            PreviewLastPart = lastPart,
        };
        async Task<ThinkingResponse> Thought(string neutral)
        {
            segments.Add(new ThinkingSegment(thinkingModusMentis,
                await VoiceAsync(thinkingSlot, thinkingModusMentis, neutral, NarrationKind.Reasoning, thoughtPart, cancellationToken), IsThought: true));
            return Finish(null);
        }

        // ── Keep it, or want no part of it ──────────────────────────────────────
        // A will keeps an act of its own course and nothing else; any other mind is asked the goal
        // question with this one goal and the decline, under its rules of choice.
        bool keeps;
        if (scene != null && pov != null && Rules.Choice.WillPriority.Steers(thinkingModusMentis, scene))
        {
            keeps = thinkingModusMentis.Will!.Matches(goal)
                    || Rules.Choice.WillPriority.PathOf(thinkingModusMentis, scene, pov, actingMember) is { } path
                       && Scene.ActionPathPlanner.PathThrough(scene, pov, actingMember, thinkingModusMentis.Will!, path, new[] { goal }) != null;
        }
        else
        {
            var chosen = await ChooseGoalAsync(new List<NarrativeAnchor> { goal }, thinkingModusMentis, actionModiMentis,
                                               overallLocation, areaLocation, null, choiceCtx, proposed: null, new ThinkingChain(), cancellationToken);
            keeps = !(chosen is VerbAction v && v.Verb is IgnoreVerb);
        }
        if (!keeps)
        {
            Console.WriteLine($"ThinkingExecutor: [{thinkingModusMentis.DisplayName}] reconsiders '{goalPhrase}' and wants no part of it.");
            return await Thought(NeutralNarration.ReasoningReconsiderRefused(goalPhrase));
        }

        // ── Another means, able before the click and on it ──────────────────────
        string? firstReason = null;
        var able = new List<ModusMentis>();
        foreach (var m in MeansPoolFor(goal, actionModiMentis).Where(m => m != previous.ActionModusMentis))
        {
            if (choiceCtx != null && !CanCarryOut(m, goal, choiceCtx)) continue;
            var probe = new ParsedNarrativeAction
            {
                ActionModusMentisId = m.ModusMentisId,
                ActionModusMentis   = m,
                PreselectedOutcome  = goal,
                ActionText          = $"try to {goalPhrase}",
                DisplayText         = goalPhrase,
                NeutralActionText   = goalPhrase,
                ThinkingModusMentis = thinkingModusMentis,
                Keyword             = previous.Keyword,
            };
            if (clickProbe(probe) is { } reason) { firstReason ??= reason; continue; }
            able.Add(m);
        }
        Console.WriteLine($"ThinkingExecutor: [{thinkingModusMentis.DisplayName}] reconsiders '{goalPhrase}' — "
                        + $"{able.Count} other means could carry it out" + (able.Count > 0 ? $": {string.Join(", ", able.Select(m => m.DisplayName))}." : "."));
        if (able.Count == 0)
            return await Thought(NeutralNarration.ReasoningReconsiderNoMeans(goalPhrase, firstReason));

        var skill = await ChooseSkillAsync(goalPhrase, able, thinkingModusMentis, overallLocation, areaLocation, null, cancellationToken)
                    ?? able[0];
        segments.Add(new ThinkingSegment(thinkingModusMentis,
            await VoiceAsync(thinkingSlot, thinkingModusMentis, NeutralNarration.ReasoningReconsidered(goalPhrase, skill.SkillMeans),
                             NarrationKind.Reasoning, thoughtPart, cancellationToken), IsThought: true));
        thoughtPart?.MarkComplete();

        // ── The act, in the new means's voice ───────────────────────────────────
        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(skill);
        _llmManager.ResetInstance(actionSlot);
        var actionPart = preview?.BeginAccumulatingPart(PreviewTitles.For(skill));
        lastPart = actionPart ?? lastPart;
        var fitCtx = choiceCtx == null ? null : choiceCtx with { ModusMentis = skill, Goal = goal };
        var (fit, _) = await AskPersonaFitAsync(skill, goalPhrase, overallLocation, areaLocation, null, fitCtx, cancellationToken, mayDecline: false);

        string styledAction = await _rewriter.RewriteAsync(
            actionSlot, NeutralNarration.ActionIntent(goalPhrase, skill.ActsDiscretely), NarrationKind.Action, skill.PersonaReminder2,
            forcedPrefix: ActionPrefix, styleInstruction: skill.StyleInstruction, preview: actionPart?.NextSegment(), ct: cancellationToken);
        if (string.IsNullOrWhiteSpace(styledAction))
            styledAction = ActionPrefix + (skill.ActsDiscretely ? "discretely " : "") + goalPhrase;
        string bareAction = StripPrefix(styledAction, ActionPrefix);

        return Finish(new ParsedNarrativeAction
        {
            ActionModusMentisId = skill.ModusMentisId,
            ActionModusMentis   = skill,
            PreselectedOutcome  = goal,
            ActionText          = $"try to {bareAction}",
            DisplayText         = bareAction,
            NeutralActionText   = goalPhrase,
            ThinkingModusMentis = thinkingModusMentis,
            Keyword             = previous.Keyword,
            DifficultyLevel     = Math.Clamp(goal.Verb.DifficultyFor(goal.Target) + fit.DifficultyModifier, 1, 10),
        });
    }

    /// <summary>Rewrites <paramref name="neutral"/> in <paramref name="mm"/>'s voice; the neutral text when the rewrite comes back empty.</summary>
    private async Task<string> VoiceAsync(int slot, ModusMentis mm, string neutral, NarrationKind kind, PreviewPart? part, CancellationToken ct)
    {
        string text = await _rewriter.RewriteAsync(
            slot, neutral, kind, mm.PersonaReminder2, styleInstruction: mm.StyleInstruction, preview: part?.NextSegment(), ct: ct);
        return string.IsNullOrWhiteSpace(text) ? neutral : text;
    }

    /// <summary>
    /// What one chain of retries has crossed off: the pairs of goal and means already tried, by the
    /// ledger's identity of an action (verb, target, variant), which survives the re-expansion of the
    /// verb lists. Lives for one thinking only — a later thinking starts with nothing crossed off, so a
    /// pineal gland of 0 behaves exactly as it always did.
    /// </summary>
    private sealed class ThinkingChain
    {
        private readonly HashSet<((string, Element?, object?) Goal, ModusMentis Means)> _tried = new();
        private readonly HashSet<(string, Element?, object?)> _goals = new();
        private readonly HashSet<(string, Element?, object?)> _dropped = new();

        public void CrossOff(VerbAction goal, ModusMentis means)
        {
            var key = ActionProposalLedger.IdentityOf(goal);
            _tried.Add((key, means));
            _goals.Add(key);
        }

        /// <summary>
        /// Crosses off <paramref name="goal"/> with every means at once — the player's doubt. A refusal
        /// is the means's, so another means may still carry the goal; a NO is about the goal, and
        /// crossing off only the pair brought the same goal straight back with the next means: three
        /// NOs to "meet him to talk" met three different means and then a fourth.
        /// </summary>
        public void Drop(VerbAction goal)
        {
            var key = ActionProposalLedger.IdentityOf(goal);
            _goals.Add(key);
            _dropped.Add(key);
        }

        /// <summary>Whether this chain has already tried <paramref name="goal"/> with some means.</summary>
        public bool Touched(VerbAction goal) => _goals.Contains(ActionProposalLedger.IdentityOf(goal));

        /// <summary>The means of <paramref name="pool"/> not yet tried with <paramref name="goal"/>.</summary>
        public IEnumerable<ModusMentis> Untried(VerbAction goal, IEnumerable<ModusMentis> pool)
        {
            var key = ActionProposalLedger.IdentityOf(goal);
            if (_dropped.Contains(key)) return Enumerable.Empty<ModusMentis>();
            return pool.Where(m => !_tried.Contains((key, m)));
        }
    }

    /// <summary>
    /// The means that may carry <paramref name="goal"/> out at all: for a conversation, the speaking
    /// action modi mentis when any is held (<c>SpeakingPriority</c>), else every one. The retries read
    /// what is untried against this pool, not the whole list — measured against the whole list, a
    /// conversation whose one speaking means was crossed off came back carried out by Finesse,
    /// Wayfaring and then Hard Labor.
    /// </summary>
    private static List<ModusMentis> MeansPoolFor(NarrativeAnchor goal, List<ModusMentis> all)
    {
        if (!Rules.Choice.SpeakingPriority.IsDialogue(goal)) return all;
        var speakers = all.Where(Rules.Choice.SpeakingPriority.Speaks).ToList();
        return speakers.Count > 0 ? speakers : all;
    }

    // ── Persona-fit (possibility + difficulty) ──────────────────────────────────

    /// <summary>The persona-fit answers and how each maps to difficulty / cancellation.</summary>
    private readonly struct PersonaFit
    {
        public int DifficultyModifier { get; }
        public bool Cancels { get; }
        private PersonaFit(int modifier, bool cancels) { DifficultyModifier = modifier; Cancels = cancels; }

        public static readonly PersonaFit Eager     = new(-1, false);
        public static readonly PersonaFit Willing   = new(0,  false);
        public static readonly PersonaFit Reluctant = new(+1, false);
        public static readonly PersonaFit Refused   = new(0,  true);

        public override string ToString() => Cancels ? "refused" : $"difficulty {DifficultyModifier:+0;-0;0}";
    }

    /// <summary>
    /// The real persona-fit options, written as stances on one willingness axis ("eager to do it");
    /// the refusal rides in as the selector's decline option ("unwilling to do it"), so a <c>null</c>
    /// pick means the skill refuses the action. Phrasing the whole set as parallel stances (rather than
    /// three "do it …" commands plus a lone "refuse") keeps the critic matching on the willingness axis
    /// instead of on which option names the target.
    ///
    /// <para>The default set. <see cref="Rules.Choice.ChoiceRulesChecker.FilterWillingness"/> may narrow
    /// it before it is offered — an unscrupulous skill asked to commit a crime loses the refusal.</para>
    /// </summary>
    private static readonly Rules.Choice.WillingnessOptions DefaultWillingness = new(
        new[] { "eager to do it", "willing to do it", "reluctant to do it" },
        DeclineOption: "unwilling to do it");

    /// <summary>
    /// Asks how strongly the action skill is drawn to the action, through the choice judge's
    /// willingness scale (<see cref="ChoiceJudge.JudgeWillingnessAsync"/>): "eager to do it" (−1
    /// difficulty), "willing to do it" (0), "reluctant to do it" (+1), or "unwilling to do it" (cancels
    /// the action — caller renders the refusal outcome). In playground mode picks Willing.
    /// </summary>
    private async Task<(PersonaFit Fit, string? ImposedReason)> AskPersonaFitAsync(
        ModusMentis skill, string goalPhrase,
        string? overallLocation, string? areaLocation, string? observedPhrase,
        Rules.Choice.ChoiceRuleContext? choiceCtx, CancellationToken ct,
        bool mayDecline = true)
    {
        // Coded rules narrow the answers before they are offered. A skill with no refusal left cannot
        // land on PersonaFit.Refused at all — the decline option is simply not in the prompt.
        var options = choiceCtx == null
            ? DefaultWillingness
            : Rules.Choice.ChoiceRulesChecker.FilterWillingness(DefaultWillingness, choiceCtx);

        // A second attempt at a refused goal: the means was picked from those the rules allow, and it
        // answers only with a degree of assent — one refusal of the same act per phase is enough.
        if (!mayDecline) options = options.WithoutDecline();

        // A rule that has already said no is answered without a question — and ahead of the playground
        // stand-in, since a coded rule is deterministic and has nothing to stand in for.
        if (options.ImposedRefusal != null)
        {
            Console.WriteLine($"ThinkingExecutor: Persona-fit for '{goalPhrase}' ({skill.DisplayName}): "
                            + $"refused by rule — {options.ImposedRefusal}");
            return (PersonaFit.Refused, options.ImposedRefusal);
        }

        if (PlaygroundMode.IsActive)
        {
            // --refuse-means: the playground's stand-in answers "unwilling" while the count lasts, and
            // only where a refusal is on offer at all.
            if (Config.Debug.RefuseMeans > 0 && options.DeclineOption != null)
            {
                Config.Debug.RefuseMeans--;
                Console.WriteLine($"ThinkingExecutor: --refuse-means: {skill.DisplayName} refuses '{goalPhrase}' ({Config.Debug.RefuseMeans} left).");
                return (PersonaFit.Refused, null);
            }
            return (PersonaFit.Willing, null);
        }

        string situation = ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, observedPhrase).TrimEnd();
        string lead = situation.Length == 0 ? "" : situation + " ";
        // The judge always reads the whole scale (its thresholds are set on the whole scale) and the
        // answer is then narrowed to what the rules left on offer: a stance taken away falls to the
        // next weaker one, and a refusal taken away to the weakest assent.
        var judged = await ChoiceJudge.JudgeWillingnessAsync(
            skill, $"{lead}You are considering whether to {goalPhrase}.", ct);
        var fit = Offered(judged);

        Console.WriteLine($"ThinkingExecutor: Persona-fit for '{goalPhrase}' ({skill.DisplayName}): {judged} -> {fit}");
        return (fit, null);

        PersonaFit Offered(Willingness w)
        {
            if (w == Willingness.Unwilling)
                return options.DeclineOption != null ? PersonaFit.Refused : WeakestAssent();
            string stance = w switch
            {
                Willingness.Eager   => "eager to do it",
                Willingness.Willing => "willing to do it",
                _                   => "reluctant to do it",
            };
            int at = DefaultWillingness.Stances.ToList().IndexOf(stance);
            foreach (var s in DefaultWillingness.Stances.Skip(at))
                if (options.Stances.Contains(s)) return FitOf(s);
            return WeakestAssent();
        }
        PersonaFit WeakestAssent() => options.Stances.Count > 0 ? FitOf(options.Stances[^1]) : PersonaFit.Reluctant;
        static PersonaFit FitOf(string stance) => stance switch
        {
            "eager to do it"     => PersonaFit.Eager,
            "willing to do it"   => PersonaFit.Willing,
            _                    => PersonaFit.Reluctant,
        };
    }

    /// <summary>
    /// Whether the coded willingness rules let <paramref name="skill"/> carry out <paramref name="goal"/>
    /// at all — i.e. impose no refusal. Asked as a silent probe: nothing is offered or refused by it.
    /// The persona's own willingness is not consulted; this is only what the rules forbid outright.
    /// </summary>
    private static bool CanCarryOut(ModusMentis skill, NarrativeAnchor goal, Rules.Choice.ChoiceRuleContext ctx)
        => Rules.Choice.ChoiceRulesChecker.FilterWillingness(
               DefaultWillingness, ctx with { ModusMentis = skill, Goal = goal, Probe = true })
           .ImposedRefusal == null;

    // ── Decision: GOAL ─────────────────────────────────────────────────────────

    private async Task<NarrativeAnchor> ChooseGoalAsync(
        List<NarrativeAnchor> subOutcomes,
        ModusMentis thinkingModusMentis,
        List<ModusMentis> actionModiMentis,
        string? overallLocation,
        string? areaLocation,
        string? observedPhrase,
        Rules.Choice.ChoiceRuleContext? choiceCtx,
        ActionProposalLedger? proposed,
        ThinkingChain chain,
        CancellationToken ct,
        bool offerDecline = true)
    {
        // Only real, pursuable goals go in the list; "ignore & move on" rides in as the decline option
        // below. Choosing decline returns IgnoreVerb.MakeOutcome() so the caller's isIgnore exit fires.
        var realOutcomes = subOutcomes
            .Where(o => !(o is VerbAction vo && vo.Verb is IgnoreVerb))
            .ToList();

        // Anything this phase has already put a button on screen for is dropped, so a second thought
        // about the same object reaches for something new instead of re-offering what the player has
        // already been handed. Before the coded rules, for the same reason the ledger is consulted
        // before them everywhere else: what has been offered is not this mind's business to weigh.
        // A goal this same mind already chose this phase and saw refused is not offered to it again —
        // it would converge on it, be refused again, and spend the point on nothing. Another mind
        // asked about the same object is still offered it.
        if (proposed != null)
        {
            realOutcomes = proposed.Remaining(realOutcomes);
            int before = realOutcomes.Count;
            // Within one chain of retries, a goal this mind saw refused comes back while a means for
            // it is untried: the refusal was that means's, and another is what a retry is for.
            realOutcomes = proposed.NotRefusedFor(thinkingModusMentis, realOutcomes,
                reoffer: va => chain.Touched(va) && chain.Untried(va, MeansPoolFor(va, actionModiMentis)).Any());
            if (realOutcomes.Count < before)
                Console.WriteLine($"ThinkingExecutor: [{thinkingModusMentis.DisplayName}] already saw "
                                + $"{before - realOutcomes.Count} goal(s) refused this phase — withholding them.");
        }

        // Coded rules narrow the list to what this mind may be shown at all — a principled modus
        // mentis is not offered crimes, an unscrupulous one is offered nothing else. Applied before
        // the empty check on purpose: filtering everything away IS a decision, and it reads as ignore.
        if (choiceCtx != null)
            realOutcomes = Rules.Choice.ChoiceRulesChecker.FilterGoals(realOutcomes, choiceCtx).ToList();

        // A goal already refused this phase is offered again only if some action modus mentis held
        // could carry it out under the coded rules. When none can, a second choice of it could only be
        // refused a second time, so it is withheld from every mind for the rest of the phase.
        if (choiceCtx != null && proposed != null)
        {
            int before = realOutcomes.Count;
            realOutcomes = realOutcomes
                .Where(g => g is not VerbAction va || !proposed.WasRefused(va)
                            || chain.Untried(va, actionModiMentis).Any(s => CanCarryOut(s, g, choiceCtx)))
                .ToList();
            if (realOutcomes.Count < before)
                Console.WriteLine($"ThinkingExecutor: {before - realOutcomes.Count} goal(s) refused earlier "
                                + "this phase and beyond every action modus mentis held — withholding them.");
        }

        // A goal this chain has tried with every means it could take is crossed off for good.
        {
            int before = realOutcomes.Count;
            realOutcomes = realOutcomes
                .Where(g => g is not VerbAction va || !chain.Touched(va) || chain.Untried(va, MeansPoolFor(va, actionModiMentis)).Any())
                .ToList();
            if (realOutcomes.Count < before)
                Console.WriteLine($"ThinkingExecutor: {before - realOutcomes.Count} goal(s) already tried with every means — crossed off.");
        }

        // Nothing left to offer. The caller's ignore branch narrates the same "nothing here worth
        // doing" line the decline option produces, in this modus mentis's voice — but with no
        // question put to it, because there is nothing to ask about.
        if (realOutcomes.Count == 0) return IgnoreVerb.MakeOutcome();

        if (PlaygroundMode.IsActive)
        {
            var pool = GoalOnlyFilter(realOutcomes);
            return pool[_rng.Next(pool.Count)];
        }

        // The judge picks one goal for this Modus Mentis — or the decline option, which is the ignore
        // outcome. Each goal phrase ("grab a beechnut") is already the action; the selector lists them
        // and collapses duplicates. The decline is a real answer: with the goal list shrinking request
        // by request (see the proposal ledger above), a mind left with two leftovers it cares nothing
        // for should be able to say so rather than be forced to commit to one of them. Landing on it is
        // a refusal — the caller's isIgnore exit fires, and the noetic point is spent all the same.
        var chosen = await _selector.SelectAsync(
            ChoiceKind.Goal, thinkingModusMentis, realOutcomes,
            o => o.ToNaturalLanguageString(),
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, observedPhrase),
            declineOption: offerDecline ? "do something else entirely" : null, ct: ct);

        if (chosen == null)
            Console.WriteLine("ThinkingExecutor: the judge chose none of the offered goals — treating as a refusal.");
        return chosen ?? IgnoreVerb.MakeOutcome();
    }

    /// <summary>
    /// Narrows a playground goal draw to the verb <c>--goal-only</c> (or the CLI's <c>goal</c>
    /// command) names. Returns the pool untouched at the flag's default, and again when no goal in it
    /// matches — a phase where the named verb does not apply then draws as usual rather than being
    /// unable to choose anything.
    /// </summary>
    private static List<NarrativeAnchor> GoalOnlyFilter(List<NarrativeAnchor> outcomes)
    {
        var wanted = Config.Debug.GoalOnly;
        if (string.IsNullOrWhiteSpace(wanted)) return outcomes;

        var matched = outcomes
            .Where(o => o is VerbAction vo &&
                        string.Equals(vo.Verb.VerbId, wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count > 0) return matched;

        Cathedral.Game.DebugFlagAudit.Miss("--goal-only", wanted, $"all {outcomes.Count} offered goal(s)");
        return outcomes;
    }

    /// <summary>The <see cref="GoalOnlyFilter"/> of the means: <c>--means-only</c> / the CLI's <c>means</c>.</summary>
    private static List<ModusMentis> MeansOnlyFilter(List<ModusMentis> skills)
    {
        var wanted = Config.Debug.MeansOnly;
        if (string.IsNullOrWhiteSpace(wanted)) return skills;

        var matched = skills
            .Where(s => string.Equals(s.ModusMentisId, wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count > 0) return matched;

        Cathedral.Game.DebugFlagAudit.Miss("--means-only", wanted, $"all {skills.Count} action modi mentis");
        return skills;
    }

    // ── Decision: HOW (skill) ──────────────────────────────────────────────────

    private async Task<ModusMentis?> ChooseSkillAsync(
        string goalPhrase,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis,
        string? overallLocation,
        string? areaLocation,
        string? observedPhrase,
        CancellationToken ct)
    {
        if (actionModiMentis.Count == 0) return null;
        if (PlaygroundMode.IsActive)
        {
            var pool = MeansOnlyFilter(actionModiMentis);
            return pool[_rng.Next(pool.Count)];
        }

        // The goal is fixed; the judge picks which action skill this thinking Modus Mentis would carry
        // it out with. Each option names the skill and what it does ("Sneak Art: stealth and silent
        // movement"); the goal lives in the situation so it need not repeat per option. No decline —
        // a means is always settled on.
        string situation = ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, observedPhrase).TrimEnd();
        string lead = situation.Length == 0 ? "" : situation + " ";
        return await _selector.SelectAsync(
            ChoiceKind.Means, thinkingModusMentis, actionModiMentis,
            s => $"{s.DisplayName}: {s.SkillMeans}",
            $"{lead}Your goal is to {goalPhrase}.", ct: ct);
    }

    // ── Item combination (reasoning + reformulated action) ──────────────────────

    /// <summary>
    /// Reasons (in the action Modus Mentis's voice) about how a combined item helps the action.
    /// </summary>
    public async Task<string?> ExecuteItemReasoningAsync(
        ParsedNarrativeAction originalAction,
        Item item,
        NarrationNode node,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken = default,
        ILlmPreviewSink? preview = null)
    {
        var mm = originalAction.ActionModusMentis;
        if (mm == null) return null;

        int slot = await _slotManager.GetOrCreateSlotForModusMentisAsync(mm);
        _llmManager.ResetInstance(slot);
        string neutral = $"I could use {item.WithArticle()} to help me {ActionDisplay(originalAction)}.";
        return await _rewriter.RewriteAsync(slot, neutral, NarrationKind.Reasoning, mm.PersonaReminder2, styleInstruction: mm.StyleInstruction, preview: preview, ct: cancellationToken);
    }

    /// <summary>
    /// Reformulates an action to incorporate a combined item, in the action Modus Mentis's voice.
    /// Returns the styled display text (the "I will " opening stripped, so it reads as a button label).
    /// <para>
    /// Generated exactly like the plain action above: the neutral sentence is the same
    /// <see cref="NeutralNarration.ActionIntent"/> "I will …" statement, the rewrite is GBNF-forced to
    /// open with the same literal, and the same literal is stripped off the answer. Without the forced
    /// prefix this path fell back to the kind's default "I ", and the persona wrote the deed as
    /// something already under way — "I slice into the pig's belly … using an arming sword" — where
    /// every other action button reads as an intention.
    /// </para>
    /// </summary>
    public async Task<string?> ExecuteItemReformulationAsync(
        ParsedNarrativeAction originalAction,
        Item item,
        NarrationNode node,
        Protagonist protagonist,
        WorldContext worldContext,
        CancellationToken cancellationToken = default,
        ILlmPreviewSink? preview = null)
    {
        var mm = originalAction.ActionModusMentis;
        if (mm == null) return null;

        int slot = await _slotManager.GetOrCreateSlotForModusMentisAsync(mm);
        _llmManager.ResetInstance(slot);
        string neutral = NeutralNarration.ActionIntent($"{ActionDisplay(originalAction)} using {item.WithArticle()}");
        string styled = await _rewriter.RewriteAsync(slot, neutral, NarrationKind.Action, mm.PersonaReminder2, forcedPrefix: ActionPrefix, styleInstruction: mm.StyleInstruction, preview: preview, ct: cancellationToken);
        if (string.IsNullOrWhiteSpace(styled)) return null;
        return StripPrefix(styled, ActionPrefix);
    }

    /// <summary>
    /// The opening every action rewrite is generated behind, and stripped of afterwards to form the
    /// button label. It is <see cref="NeutralNarration.ActionIntent"/>'s opening, the GBNF forced
    /// prefix, and the fallback's opening — one literal, so the three cannot drift apart.
    /// </summary>
    private const string ActionPrefix = "I will ";

    private static string ActionDisplay(ParsedNarrativeAction action)
        => !string.IsNullOrWhiteSpace(action.DisplayText)
            ? action.DisplayText
            : StripTryToPrefix(action.ActionText ?? "");

    /// <summary>Drops a leading "try to " so an attempt phrase becomes the bare action used as a label.</summary>
    private static string StripTryToPrefix(string text) => StripPrefix(text, "try to ");

    /// <summary>Drops <paramref name="prefix"/> (case-insensitive) and surrounding whitespace if present.</summary>
    private static string StripPrefix(string text, string prefix)
        => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? text.Substring(prefix.Length).Trim() : text.Trim();
}

/// <summary>
/// Represents the response from a thinking modusMentis LLM request.
/// </summary>
public class ThinkingResponse
{
    /// <summary>
    /// Everything the thinking produced, in order: the thinking modus mentis's thoughts
    /// (<see cref="ThinkingSegment.IsThought"/>) and the refusals between them, one per try of
    /// iterative thinking (<see cref="ThinkingIterationsStat"/>). Without a retry this is one thought,
    /// or one thought and its refusal. The last thought is the one the <see cref="Actions"/> belong to.
    /// </summary>
    public List<ThinkingSegment> Segments { get; set; } = new();

    /// <summary>The last thought's text (<see cref="Segments"/>).</summary>
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();

    /// <summary>
    /// Set when the action modus mentis refused the action (persona-fit reluctant/opposed): the
    /// first-person "I don't want to …" narration, shown as an outcome block. <see cref="Actions"/>
    /// is empty in this case.
    /// </summary>
    public string? RefusalText { get; set; }

    /// <summary>The skill that refused, whose voice the <see cref="RefusalText"/> is in.</summary>
    public ModusMentis? RefusalModusMentis { get; set; }

    /// <summary>
    /// The last preview part of this thinking generation (reasoning-only paths: the reasoning part;
    /// the full path: the action/refusal part). The caller attaches the block-commit closure to it
    /// and marks it complete once the block is built (see NarrativeController.ExecuteThinkingPhaseAsync).
    /// Null when previewing is off.
    /// </summary>
    internal PreviewPart? PreviewLastPart { get; set; }
}

/// <summary>
/// One block a thinking produced: a thought in the thinking modus mentis's voice, or a refusal in the
/// voice of the action modus mentis that refused (or on whose behalf a coded rule refused).
/// </summary>
public sealed record ThinkingSegment(ModusMentis ModusMentis, string Text, bool IsThought);
