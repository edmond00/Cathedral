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

    private readonly Random _rng = new();

    /// <summary>
    /// GOAL (decision) → optional IGNORE early-exit → HOW (decision) → reasoning rewrite →
    /// action rewrite. Returns null only if no usable action skill is available.
    /// </summary>
    public async Task<ThinkingResponse?> GenerateThinkingAsync(
        ModusMentis thinkingModusMentis,
        ConcreteOutcome targetOutcome,
        string keyword,
        NarrationNode node,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        int locationId,
        PartyMember actingMember,
        bool isReminescence = false,
        bool autoSuccess = false,
        LlmPreviewSession? preview = null,
        CancellationToken cancellationToken = default)
    {
        // The reasoning box accumulates the thinking MM's free "wants" (goal, then means) as dimmer
        // parenthesized inner thoughts, followed by the reasoning rewrite. The action box (a different
        // MM) gets the persona-fit want, then the action rewrite.
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
            ? new List<ConcreteOutcome>(sourceObs.SubOutcomes)
            : new List<ConcreteOutcome> { targetOutcome };

        string targetDescription = targetOutcome.ToNaturalLanguageString();

        // Short situational context threaded into every constrained-choice prompt: the overall
        // location (e.g. "a farm"), the specific area within it (e.g. "courtyard"), and what drew
        // the character's attention (the observed object).
        string overallLocation = worldContext.GenerateContextDescription(locationId);
        string areaLocation = node.GenerateNeutralDescription(locationId);
        string? observedPhrase = sourceObs?.NeutralPhrase;

        // ── Decision 1: GOAL ────────────────────────────────────────────────────
        var (resolved, goalThought) = await ChooseGoalAsync(thinkingSlot, subOutcomes, thinkingModusMentis, overallLocation, areaLocation, observedPhrase, cancellationToken, reasoningPart);
        bool isIgnore = resolved is VerbOutcome vIgnore && vIgnore.VerbView.Verb is IgnoreVerb;

        // ── Early exit: IGNORE (reasoning only, no action) ──────────────────────
        if (isIgnore)
        {
            string ignoreNeutral = NeutralNarration.ReasoningIgnore(targetDescription, isReminescence);
            string ignoreReasoning = await _rewriter.RewriteAsync(
                thinkingSlot, ignoreNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, innerThought: goalThought, preview: reasoningPart?.NextSegment(), ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText = ignoreReasoning,
                Actions = new List<ParsedNarrativeAction>(),
                PreviewLastPart = reasoningPart
            };
        }

        string goalPhrase = resolved.ToNaturalLanguageString();

        // ── Decision 2: HOW (which action skill) ────────────────────────────────
        var (skill, skillThought) = await ChooseSkillAsync(thinkingSlot, goalPhrase, actionModiMentis, thinkingModusMentis, overallLocation, areaLocation, observedPhrase, cancellationToken, reasoningPart);
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
                thinkingSlot, noMeansNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, innerThought: JoinThoughts(goalThought, skillThought), preview: reasoningPart?.NextSegment(), ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText = string.IsNullOrWhiteSpace(noMeansText) ? noMeansNeutral : noMeansText,
                Actions = new List<ParsedNarrativeAction>(),
                PreviewLastPart = reasoningPart
            };
        }

        // ── Flavor: reasoning (thinking slot) ───────────────────────────────────
        // The goal and means wants are handed back as the inner thought behind the chain, so the
        // styled reasoning echoes why this goal and this way were chosen.
        string reasoningNeutral = NeutralNarration.ReasoningChain(targetDescription, goalPhrase, skill.SkillMeans, isReminescence);
        string reasoningText = await _rewriter.RewriteAsync(
            thinkingSlot, reasoningNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, innerThought: JoinThoughts(goalThought, skillThought), preview: reasoningPart?.NextSegment(), ct: cancellationToken);
        // Reasoning is done — make it continue-able now while the action (different MM) streams behind it.
        reasoningPart?.MarkComplete();

        // ── Action skill slot: persona-fit check, then action-text flavor ───────
        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(skill);
        _llmManager.ResetInstance(actionSlot);

        // The action box (skill MM) accumulates the persona-fit want, then the action rewrite.
        var actionPart = preview?.BeginAccumulatingPart(PreviewTitles.For(skill));

        // Persona-fit: how strongly is the skill drawn to this action? Decides possibility + difficulty
        // via the persona-reasoning → neutral-critic pass (the selector resets the slot in and out, so
        // the action rewrite below starts fresh). Skipped for auto-success phases (reminescence /
        // get-up). Replaces the former plausibility + difficulty critic trees.
        var (fit, fitThought) = autoSuccess
            ? (PersonaFit.Willing, (string?)null)
            : await AskPersonaFitAsync(actionSlot, skill, goalPhrase, overallLocation, areaLocation, observedPhrase, cancellationToken, actionPart);

        // "refuse to do it" → the skill refuses; produce a first-person refusal outcome, no action.
        // The fit want explains the refusal, so it rides into the rewrite as the inner thought.
        if (fit.Cancels)
        {
            string refusalNeutral = NeutralNarration.ActionRefusal(goalPhrase);
            string refusalText = await _rewriter.RewriteAsync(
                actionSlot, refusalNeutral, NarrationKind.Outcome, skill.PersonaReminder2,
                styleInstruction: skill.StyleInstruction, innerThought: fitThought, preview: actionPart?.NextSegment(), ct: cancellationToken);
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
        const string actionPrefix = "I will ";
        string styledAction = await _rewriter.RewriteAsync(
            actionSlot, NeutralNarration.ActionIntent(goalPhrase, skill.ActsDiscretely), NarrationKind.Action, skill.PersonaReminder2, forcedPrefix: actionPrefix, styleInstruction: skill.StyleInstruction, innerThought: fitThought, preview: actionPart?.NextSegment(), ct: cancellationToken);
        if (string.IsNullOrWhiteSpace(styledAction))
            styledAction = actionPrefix + (skill.ActsDiscretely ? "discretely " : "") + goalPhrase;
        string bareAction = StripPrefix(styledAction, actionPrefix);

        // Difficulty: verb base ± the persona-fit modifier (eager −1 / willing 0 / reluctant +1),
        // clamped to 1..10. Auto-success phases carry difficulty 0 (rendered with the ○ glyph).
        var verbOutcome = (VerbOutcome)resolved;
        int difficultyLevel = autoSuccess
            ? 0
            : Math.Clamp(verbOutcome.VerbView.Verb.DifficultyFor(verbOutcome.VerbView.Target) + fit.DifficultyModifier, 1, 10);

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
    }

    /// <summary>
    /// The real persona-fit options, written as actions ("do it eagerly"); the refusal rides in as
    /// the selector's decline option, so a <c>null</c> pick means the skill refuses the action.
    /// </summary>
    private static readonly string[] PersonaFitActions = { "do it eagerly", "do it willingly", "do it reluctantly" };

    /// <summary>
    /// Asks the action skill how strongly it is drawn to the action, through the same
    /// persona-reasoning → neutral-critic pass as every other choice
    /// (<see cref="PersonaChoiceSelector"/>): the skill answers "Do you want to do it?" in its own
    /// voice, and the critic maps that onto "do it eagerly" (−1 difficulty), "do it willingly" (0),
    /// "do it reluctantly" (+1), or the decline option "refuse to do it" (cancels the action —
    /// caller renders the refusal outcome). The selector resets the slot in and out, so the
    /// following action rewrite starts from the system prompt. In playground mode picks Willing.
    /// </summary>
    private async Task<(PersonaFit Fit, string? Reasoning)> AskPersonaFitAsync(
        int actionSlot, ModusMentis skill, string goalPhrase,
        string? overallLocation, string? areaLocation, string? observedPhrase, CancellationToken ct,
        PreviewPart? part = null)
    {
        if (PlaygroundMode.IsActive) return (PersonaFit.Willing, null);

        string situation = ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, observedPhrase).TrimEnd();
        string lead = situation.Length == 0 ? "" : situation + " ";
        var prompt = new PersonaChoicePrompt(
            $"{lead}You are considering whether to {goalPhrase}.\n\n",
            "Do you want to do it?", "whether they want to do it");

        var chosen = await _selector.SelectAsync(
            actionSlot, skill, PersonaFitActions,
            a => a,
            prompt, declineOption: "refuse to do it", preview: part?.NextSegment(isFree: true), ct: ct);

        Console.WriteLine($"ThinkingExecutor: Persona-fit for '{goalPhrase}' ({skill.DisplayName}): {chosen.Item ?? "refuse to do it"}");
        var fit = chosen.Item switch
        {
            "do it eagerly"     => PersonaFit.Eager,
            "do it willingly"   => PersonaFit.Willing,
            "do it reluctantly" => PersonaFit.Reluctant,
            null                => PersonaFit.Refused,
            _                   => PersonaFit.Willing, // unrecognised → proceed at base difficulty
        };
        return (fit, chosen.Reasoning);
    }

    // ── Decision: GOAL ─────────────────────────────────────────────────────────

    private async Task<(ConcreteOutcome Outcome, string? Reasoning)> ChooseGoalAsync(
        int thinkingSlot,
        List<ConcreteOutcome> subOutcomes,
        ModusMentis thinkingModusMentis,
        string? overallLocation,
        string? areaLocation,
        string? observedPhrase,
        CancellationToken ct,
        PreviewPart? part = null)
    {
        // Only real, pursuable goals go in the list; "ignore & move on" rides in as the decline option
        // below. Choosing decline returns IgnoreVerb.MakeOutcome() so the caller's isIgnore exit fires.
        var realOutcomes = subOutcomes
            .Where(o => !(o is VerbOutcome vo && vo.VerbView.Verb is IgnoreVerb))
            .ToList();
        if (realOutcomes.Count == 0) return (IgnoreVerb.MakeOutcome(), null);

        if (PlaygroundMode.IsActive)
            return (realOutcomes[_rng.Next(realOutcomes.Count)], null);

        // The Modus Mentis reasons over the goals ("What do you want to do?") and the neutral critic
        // maps that to one — or to the decline option, which is the ignore outcome. Each goal phrase
        // ("grab a beechnut") is already the action; the selector lists them and collapses duplicates.
        var prompt = new PersonaChoicePrompt(
            ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, observedPhrase),
            "What do you want to do?", "what they want to do");
        // No decline option for now — the persona always commits to a real goal.
        var chosen = await _selector.SelectAsync(
            thinkingSlot, thinkingModusMentis, realOutcomes,
            o => o.ToNaturalLanguageString(),
            prompt, preview: part?.NextSegment(isFree: true), ct: ct);

        // Null item only if the list was empty; the reasoning still explains the (non-)choice.
        return (chosen.Item ?? IgnoreVerb.MakeOutcome(), chosen.Reasoning);
    }

    // ── Decision: HOW (skill) ──────────────────────────────────────────────────

    private async Task<(ModusMentis? Skill, string? Reasoning)> ChooseSkillAsync(
        int thinkingSlot,
        string goalPhrase,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis,
        string? overallLocation,
        string? areaLocation,
        string? observedPhrase,
        CancellationToken ct,
        PreviewPart? part = null)
    {
        if (actionModiMentis.Count == 0) return (null, null);
        if (PlaygroundMode.IsActive)
            return (actionModiMentis[_rng.Next(actionModiMentis.Count)], null);

        // The goal is fixed; the Modus Mentis reasons over the available means ("How do you want to do
        // it?") and the neutral critic maps that to one skill — or to the decline option, which is the
        // "no way to do it" outcome. Each option is the means ("with the unfussy keeping-of-oneself-
        // alive"); the goal lives in the context so it need not repeat per option.
        string situation = ThinkingPromptConstructor.SituationLine(overallLocation, areaLocation, observedPhrase).TrimEnd();
        string lead = situation.Length == 0 ? "" : situation + " ";
        var prompt = new PersonaChoicePrompt(
            $"{lead}Your goal is to {goalPhrase}.\n\n",
            "How do you want to do it?", "how they want to go about it");
        // No decline option for now — the persona always settles on one means.
        var chosen = await _selector.SelectAsync(
            thinkingSlot, thinkingModusMentis, actionModiMentis,
            s => $"go about it with {s.SkillMeans}",
            prompt, preview: part?.NextSegment(isFree: true), ct: ct);

        // Null item only if the list was empty; the caller still handles it as "no way to do it".
        return (chosen.Item, chosen.Reasoning);
    }

    /// <summary>Joins choice reasonings into one inner-thought hint; null when there is none.</summary>
    private static string? JoinThoughts(params string?[] thoughts)
    {
        var parts = thoughts.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()).ToList();
        return parts.Count == 0 ? null : string.Join(" ", parts);
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
    /// Returns the styled display text (no "try to " prefix).
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
        string neutral = $"{ActionDisplay(originalAction)} using {item.WithArticle()}";
        string styled = await _rewriter.RewriteAsync(slot, neutral, NarrationKind.Action, mm.PersonaReminder2, styleInstruction: mm.StyleInstruction, preview: preview, ct: cancellationToken);
        if (string.IsNullOrWhiteSpace(styled)) return null;
        return StripTryToPrefix(styled);
    }

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
