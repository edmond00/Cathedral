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
        CancellationToken cancellationToken = default)
    {
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
        ConcreteOutcome resolved = await ChooseGoalAsync(thinkingSlot, subOutcomes, thinkingModusMentis, overallLocation, areaLocation, observedPhrase, cancellationToken);
        bool isIgnore = resolved is VerbOutcome vIgnore && vIgnore.VerbView.Verb is IgnoreVerb;

        // ── Early exit: IGNORE (reasoning only, no action) ──────────────────────
        if (isIgnore)
        {
            string ignoreNeutral = NeutralNarration.ReasoningIgnore(targetDescription, isReminescence);
            string ignoreReasoning = await _rewriter.RewriteAsync(
                thinkingSlot, ignoreNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText = ignoreReasoning,
                Actions = new List<ParsedNarrativeAction>()
            };
        }

        string goalPhrase = resolved.ToNaturalLanguageString();

        // ── Decision 2: HOW (which action skill) ────────────────────────────────
        ModusMentis? skill = await ChooseSkillAsync(thinkingSlot, goalPhrase, actionModiMentis, thinkingModusMentis, overallLocation, areaLocation, observedPhrase, cancellationToken);
        if (skill == null)
        {
            if (actionModiMentis.Count == 0)
            {
                Console.Error.WriteLine("ThinkingExecutor: no usable action skill for the chosen goal.");
                return null;
            }

            // Skills exist but the thinking Modus Mentis declined every means → "no way to do
            // it": a reasoning-only outcome in the thinking MM's voice, mirroring the ignore branch.
            string noMeansNeutral = NeutralNarration.ReasoningNoMeans(targetDescription, goalPhrase, isReminescence);
            string noMeansText = await _rewriter.RewriteAsync(
                thinkingSlot, noMeansNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText = string.IsNullOrWhiteSpace(noMeansText) ? noMeansNeutral : noMeansText,
                Actions = new List<ParsedNarrativeAction>()
            };
        }

        // ── Flavor: reasoning (thinking slot) ───────────────────────────────────
        string reasoningNeutral = NeutralNarration.ReasoningChain(targetDescription, goalPhrase, skill.SkillMeans, isReminescence);
        string reasoningText = await _rewriter.RewriteAsync(
            thinkingSlot, reasoningNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, ct: cancellationToken);

        // ── Action skill slot: persona-fit check, then action-text flavor ───────
        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(skill);
        _llmManager.ResetInstance(actionSlot);

        // Persona-fit: how strongly is the skill drawn to this action? Decides possibility + difficulty.
        // Asked with keepHistory:true so it shares context with the action-text rewrite below (both
        // concern the same action). Skipped for auto-success phases (reminescence / get-up).
        // Replaces the former plausibility + difficulty critic trees.
        PersonaFit fit = autoSuccess
            ? PersonaFit.Willing
            : await AskPersonaFitAsync(actionSlot, skill, goalPhrase, overallLocation, areaLocation, observedPhrase, cancellationToken);

        // Reluctant / opposed → the skill refuses; produce a first-person refusal outcome, no action.
        if (fit.Cancels)
        {
            string refusalNeutral = NeutralNarration.ActionRefusal(goalPhrase);
            string refusalText = await _rewriter.RewriteAsync(
                actionSlot, refusalNeutral, NarrationKind.Outcome, skill.PersonaReminder2,
                styleInstruction: skill.StyleInstruction, ct: cancellationToken);
            return new ThinkingResponse
            {
                ReasoningText   = reasoningText,
                Actions         = new List<ParsedNarrativeAction>(),
                RefusalText     = string.IsNullOrWhiteSpace(refusalText) ? refusalNeutral : refusalText,
                RefusalModusMentis = skill
            };
        }

        // The neutral sentence opens with "I will …" (plus "discretely" for a discrete skill), and the
        // GBNF prefix constraint forces the styled rewrite to open with the same literal. This
        // guarantees the prefix can be stripped cleanly to form the button label (DisplayText);
        // ActionText keeps the canonical "try to …" form the item critic expects.
        const string actionPrefix = "I will ";
        string styledAction = await _rewriter.RewriteAsync(
            actionSlot, NeutralNarration.ActionIntent(goalPhrase, skill.ActsDiscretely), NarrationKind.Action, skill.PersonaReminder2, forcedPrefix: actionPrefix, styleInstruction: skill.StyleInstruction, ct: cancellationToken);
        if (string.IsNullOrWhiteSpace(styledAction))
            styledAction = actionPrefix + (skill.ActsDiscretely ? "discretely " : "") + goalPhrase;
        string bareAction = StripPrefix(styledAction, actionPrefix);

        // Difficulty: verb base ± the persona-fit modifier (eager −1 / willing 0 / unsure +1),
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
            Actions = new List<ParsedNarrativeAction> { action }
        };
    }

    // ── Persona-fit (possibility + difficulty) ──────────────────────────────────

    /// <summary>The five persona-fit answers and how each maps to difficulty / cancellation.</summary>
    private readonly struct PersonaFit
    {
        public int DifficultyModifier { get; }
        public bool Cancels { get; }
        private PersonaFit(int modifier, bool cancels) { DifficultyModifier = modifier; Cancels = cancels; }

        public static readonly PersonaFit Eager     = new(-1, false);
        public static readonly PersonaFit Willing   = new(0,  false);
        public static readonly PersonaFit Unsure    = new(+1, false);
        public static readonly PersonaFit Reluctant = new(0,  true);
        public static readonly PersonaFit Opposed   = new(0,  true);

        public static PersonaFit FromId(string id) => id switch
        {
            "eager"     => Eager,
            "willing"   => Willing,
            "unsure"    => Unsure,
            "reluctant" => Reluctant,
            "opposed"   => Opposed,
            _           => Willing, // unrecognised → proceed at base difficulty
        };
    }

    private static readonly List<string> PersonaFitOptions =
        new() { "eager", "willing", "unsure", "reluctant", "opposed" };

    /// <summary>
    /// Asks the action skill how strongly it is drawn to the action (constrained enum on its slot,
    /// keepHistory:true so the following rewrite shares context). In playground mode picks "willing".
    /// </summary>
    private async Task<PersonaFit> AskPersonaFitAsync(
        int actionSlot, ModusMentis skill, string goalPhrase,
        string? overallLocation, string? areaLocation, string? observedPhrase, CancellationToken ct)
    {
        if (PlaygroundMode.IsActive) return PersonaFit.Willing;

        string prompt = ThinkingPromptConstructor.BuildPersonaFitPrompt(goalPhrase, skill, overallLocation, areaLocation, observedPhrase);
        string chosen = await _rewriter.ChooseAsync(actionSlot, prompt, PersonaFitOptions, fieldName: "drawn", keepHistory: true, ct: ct);
        Console.WriteLine($"ThinkingExecutor: Persona-fit for '{goalPhrase}' ({skill.DisplayName}): {(string.IsNullOrWhiteSpace(chosen) ? "(none)" : chosen)}");
        return PersonaFit.FromId(chosen.Trim());
    }

    // ── Decision: GOAL ─────────────────────────────────────────────────────────

    private async Task<ConcreteOutcome> ChooseGoalAsync(
        int thinkingSlot,
        List<ConcreteOutcome> subOutcomes,
        ModusMentis thinkingModusMentis,
        string? overallLocation,
        string? areaLocation,
        string? observedPhrase,
        CancellationToken ct)
    {
        // Only real, pursuable goals go in the list; "ignore & move on" rides in as the decline option
        // below. Choosing decline returns IgnoreVerb.MakeOutcome() so the caller's isIgnore exit fires.
        var realOutcomes = subOutcomes
            .Where(o => !(o is VerbOutcome vo && vo.VerbView.Verb is IgnoreVerb))
            .ToList();
        if (realOutcomes.Count == 0) return IgnoreVerb.MakeOutcome();

        if (PlaygroundMode.IsActive)
            return realOutcomes[_rng.Next(realOutcomes.Count)];

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
            prompt, ct: ct);

        return chosen ?? IgnoreVerb.MakeOutcome();  // null only if the list was empty
    }

    // ── Decision: HOW (skill) ──────────────────────────────────────────────────

    private async Task<ModusMentis?> ChooseSkillAsync(
        int thinkingSlot,
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
            return actionModiMentis[_rng.Next(actionModiMentis.Count)];

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
            prompt, ct: ct);

        // null only if the list was empty; the caller still handles it as "no way to do it".
        return chosen;
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
        CancellationToken cancellationToken = default)
    {
        var mm = originalAction.ActionModusMentis;
        if (mm == null) return null;

        int slot = await _slotManager.GetOrCreateSlotForModusMentisAsync(mm);
        _llmManager.ResetInstance(slot);
        string neutral = $"I could use {item.WithArticle()} to help me {ActionDisplay(originalAction)}.";
        return await _rewriter.RewriteAsync(slot, neutral, NarrationKind.Reasoning, mm.PersonaReminder2, styleInstruction: mm.StyleInstruction, ct: cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var mm = originalAction.ActionModusMentis;
        if (mm == null) return null;

        int slot = await _slotManager.GetOrCreateSlotForModusMentisAsync(mm);
        _llmManager.ResetInstance(slot);
        string neutral = $"{ActionDisplay(originalAction)} using {item.WithArticle()}";
        string styled = await _rewriter.RewriteAsync(slot, neutral, NarrationKind.Action, mm.PersonaReminder2, styleInstruction: mm.StyleInstruction, ct: cancellationToken);
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
}
