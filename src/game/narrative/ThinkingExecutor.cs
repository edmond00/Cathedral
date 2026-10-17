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
    /// GOAL (decision) → optional IGNORE early-exit → HOW (decision) → reasoning rewrite →
    /// action rewrite. Returns null only if no usable action skill is available.
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
        var reasoningPart = preview?.BeginAccumulatingPart(PreviewTitles.For(thinkingModusMentis));

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

        // ── Decision 1: GOAL ────────────────────────────────────────────────────
        // An auto-success phase (reminescence, get-up) offers no "do something else": its one verb is
        // the only way onward, and a mind that walked away from it left the player where they were.
        var resolved = await ChooseGoalAsync(subOutcomes, thinkingModusMentis, actionModiMentis, overallLocation, areaLocation, observedPhrase, choiceCtx, proposed, cancellationToken, offerDecline: !autoSuccess);
        bool isIgnore = resolved is VerbAction vIgnore && vIgnore.Verb is IgnoreVerb;

        // ── Early exit: IGNORE (reasoning only, no action) ──────────────────────
        if (isIgnore)
        {
            string ignoreNeutral = NeutralNarration.ReasoningIgnore(targetDescription, isReminescence);
            string ignoreReasoning = await _rewriter.RewriteAsync(
                thinkingSlot, ignoreNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, preview: reasoningPart?.NextSegment(), ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText = ignoreReasoning,
                Actions = new List<ParsedNarrativeAction>(),
                PreviewLastPart = reasoningPart
            };
        }

        string goalPhrase = resolved.ToNaturalLanguageString();

        // A second attempt at a goal already refused this phase is not left to be refused again: only
        // the action modi mentis the coded rules let carry it out are offered as the means, and the one
        // chosen is not asked whether it would rather not. ChooseGoalAsync has already withheld the goal
        // when none could, so the narrowed pool is never empty in play; the fallback is for safety.
        bool secondAttempt = choiceCtx != null && resolved is VerbAction retried && proposed?.WasRefused(retried) == true;
        var meansPool = actionModiMentis;
        if (secondAttempt)
        {
            var able = actionModiMentis.Where(s => CanCarryOut(s, resolved, choiceCtx!)).ToList();
            if (able.Count > 0) meansPool = able;
            Console.WriteLine($"ThinkingExecutor: '{goalPhrase}' was refused earlier this phase — "
                            + $"offering only the {able.Count} means able to carry it out, with no refusal.");
        }

        // ── Decision 2: HOW (which action skill) ────────────────────────────────
        var skill = await ChooseSkillAsync(goalPhrase, meansPool, thinkingModusMentis, overallLocation, areaLocation, observedPhrase, cancellationToken);
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
            string noMeansText = await _rewriter.RewriteAsync(
                thinkingSlot, noMeansNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, preview: reasoningPart?.NextSegment(), ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText = string.IsNullOrWhiteSpace(noMeansText) ? noMeansNeutral : noMeansText,
                Actions = new List<ParsedNarrativeAction>(),
                PreviewLastPart = reasoningPart
            };
        }

        // ── Flavor: reasoning (thinking slot) ───────────────────────────────────
        string reasoningNeutral = NeutralNarration.ReasoningChain(targetDescription, goalPhrase, skill.SkillMeans, isReminescence);
        string reasoningText = await _rewriter.RewriteAsync(
            thinkingSlot, reasoningNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, preview: reasoningPart?.NextSegment(), ct: cancellationToken);
        // Reasoning is done — make it continue-able now while the action (different MM) streams behind it.
        reasoningPart?.MarkComplete();

        // ── VerbAction skill slot: persona-fit check, then action-text flavor ───────
        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(skill);
        _llmManager.ResetInstance(actionSlot);

        // The action box (skill MM) holds the action rewrite, or the refusal.
        var actionPart = preview?.BeginAccumulatingPart(PreviewTitles.For(skill));

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
            if (resolved is VerbAction refusedGoal)
                proposed?.Refuse(thinkingModusMentis, refusedGoal);

            string refusalNeutral = imposedReason != null
                ? NeutralNarration.ActionRefusal(goalPhrase, imposedReason)
                : NeutralNarration.ActionRefusal(goalPhrase);
            string refusalText = await _rewriter.RewriteAsync(
                actionSlot, refusalNeutral, NarrationKind.Outcome, skill.PersonaReminder2,
                styleInstruction: skill.StyleInstruction, preview: actionPart?.NextSegment(), ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText   = reasoningText,
                Actions         = new List<ParsedNarrativeAction>(),
                RefusalText     = string.IsNullOrWhiteSpace(refusalText) ? refusalNeutral : refusalText,
                RefusalModusMentis = skill,
                PreviewLastPart = actionPart
            };
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

        return new ThinkingResponse
        {
            ReasoningText = reasoningText,
            Actions = new List<ParsedNarrativeAction> { action },
            PreviewLastPart = actionPart
        };
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

        if (PlaygroundMode.IsActive) return (PersonaFit.Willing, null);

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
            realOutcomes = proposed.NotRefusedFor(thinkingModusMentis, realOutcomes);
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
                            || actionModiMentis.Any(s => CanCarryOut(s, g, choiceCtx)))
                .ToList();
            if (realOutcomes.Count < before)
                Console.WriteLine($"ThinkingExecutor: {before - realOutcomes.Count} goal(s) refused earlier "
                                + "this phase and beyond every action modus mentis held — withholding them.");
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
