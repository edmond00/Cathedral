using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
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
    private readonly ThinkingPromptConstructor _promptConstructor;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly PersonaRewriter _rewriter;

    public ThinkingExecutor(
        LlamaServerManager llmManager,
        ThinkingPromptConstructor promptConstructor,
        ModusMentisSlotManager slotManager)
    {
        _llmManager = llmManager;
        _promptConstructor = promptConstructor;
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _rewriter = new PersonaRewriter(llmManager);
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
        CancellationToken cancellationToken = default)
    {
        int thinkingSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(thinkingModusMentis);
        _llmManager.ResetInstance(thinkingSlot);

        // Sub-outcomes to choose between, always including an "ignore and move on" option.
        var sourceObs = targetOutcome as ObservationObject;
        var subOutcomes = sourceObs != null
            ? new List<ConcreteOutcome>(sourceObs.SubOutcomes)
            : new List<ConcreteOutcome> { targetOutcome };
        if (!subOutcomes.Any(o => o is VerbOutcome vo && vo.VerbView.Verb is IgnoreVerb))
            subOutcomes.Add(IgnoreVerb.MakeOutcome());

        string targetDescription = targetOutcome.ToNaturalLanguageString();

        // ── Decision 1: GOAL ────────────────────────────────────────────────────
        ConcreteOutcome resolved = await ChooseGoalAsync(thinkingSlot, subOutcomes, thinkingModusMentis, cancellationToken);
        bool isIgnore = resolved is VerbOutcome vIgnore && vIgnore.VerbView.Verb is IgnoreVerb;

        // ── Early exit: IGNORE (reasoning only, no action) ──────────────────────
        if (isIgnore)
        {
            string ignoreNeutral = NeutralNarration.ReasoningIgnore(targetDescription);
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
        ModusMentis? skill = await ChooseSkillAsync(thinkingSlot, goalPhrase, actionModiMentis, thinkingModusMentis, cancellationToken);
        if (skill == null)
        {
            Console.Error.WriteLine("ThinkingExecutor: no usable action skill for the chosen goal.");
            return null;
        }

        // ── Flavor: reasoning (thinking slot) ───────────────────────────────────
        string reasoningNeutral = NeutralNarration.ReasoningChain(targetDescription, goalPhrase, skill.SkillMeans);
        string reasoningText = await _rewriter.RewriteAsync(
            thinkingSlot, reasoningNeutral, NarrationKind.Reasoning, thinkingModusMentis.PersonaReminder2, styleInstruction: thinkingModusMentis.StyleInstruction, ct: cancellationToken);

        // ── Flavor: action text (action skill slot) ─────────────────────────────
        int actionSlot = await _slotManager.GetOrCreateSlotForModusMentisAsync(skill);
        _llmManager.ResetInstance(actionSlot);
        string styledAction = await _rewriter.RewriteAsync(
            actionSlot, NeutralNarration.ActionIntent(goalPhrase), NarrationKind.Action, skill.PersonaReminder2, styleInstruction: skill.StyleInstruction, ct: cancellationToken);
        if (string.IsNullOrWhiteSpace(styledAction)) styledAction = goalPhrase;

        var action = new ParsedNarrativeAction
        {
            ActionModusMentisId = skill.ModusMentisId,
            ActionModusMentis   = skill,
            PreselectedOutcome  = (VerbOutcome)resolved,
            ActionText          = $"try to {styledAction}",
            DisplayText         = styledAction,
            ThinkingModusMentis = thinkingModusMentis,
            Keyword             = keyword
        };

        return new ThinkingResponse
        {
            ReasoningText = reasoningText,
            Actions = new List<ParsedNarrativeAction> { action }
        };
    }

    // ── Decision: GOAL ─────────────────────────────────────────────────────────

    private async Task<ConcreteOutcome> ChooseGoalAsync(
        int thinkingSlot,
        List<ConcreteOutcome> subOutcomes,
        ModusMentis thinkingModusMentis,
        CancellationToken ct)
    {
        if (PlaygroundMode.IsActive)
        {
            var pick = subOutcomes.OfType<VerbOutcome>().FirstOrDefault(v => v.VerbView.Verb is not IgnoreVerb)
                       ?? subOutcomes.OfType<VerbOutcome>().FirstOrDefault();
            return (ConcreteOutcome?)pick ?? subOutcomes[0];
        }

        var options = subOutcomes.Select(o => o.ToNaturalLanguageString()).ToList();
        string prompt = ThinkingPromptConstructor.BuildGoalPrompt(options, thinkingModusMentis);
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateChoiceSchema("goal", options));
        string json = await _llmManager.GenerateConstrainedStringAsync(thinkingSlot, prompt, gbnf, maxTokens: 64, skipReset: true);

        string chosen = ParseChoice(json, "goal");
        return subOutcomes.FirstOrDefault(o =>
                   o.ToNaturalLanguageString().Equals(chosen, StringComparison.OrdinalIgnoreCase))
               ?? IgnoreVerb.MakeOutcome();
    }

    // ── Decision: HOW (skill) ──────────────────────────────────────────────────

    private async Task<ModusMentis?> ChooseSkillAsync(
        int thinkingSlot,
        string goalPhrase,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis,
        CancellationToken ct)
    {
        if (actionModiMentis.Count == 0) return null;
        if (PlaygroundMode.IsActive)
            return actionModiMentis[_rng.Next(actionModiMentis.Count)];

        var means = actionModiMentis.Select(s => $"with {s.SkillMeans}").ToList();
        string prompt = _promptConstructor.BuildHowPrompt(goalPhrase, actionModiMentis, thinkingModusMentis);
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateChoiceSchema("how", means));
        string json = await _llmManager.GenerateConstrainedStringAsync(thinkingSlot, prompt, gbnf, maxTokens: 48, skipReset: true);

        string chosen = ParseChoice(json, "how");
        return MapMeansToModusMentis(chosen, actionModiMentis)
               ?? actionModiMentis[0];
    }

    private static ModusMentis? MapMeansToModusMentis(string means, List<ModusMentis> actionModiMentis)
        => actionModiMentis.FirstOrDefault(s => $"with {s.SkillMeans}" == means);

    private static string ParseChoice(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(field, out var p) ? (p.GetString() ?? string.Empty) : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
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
        return styled.StartsWith("try to ", StringComparison.OrdinalIgnoreCase) ? styled.Substring(7) : styled;
    }

    private static string ActionDisplay(ParsedNarrativeAction action)
    {
        if (!string.IsNullOrWhiteSpace(action.DisplayText)) return action.DisplayText;
        var text = action.ActionText ?? "";
        return text.StartsWith("try to ", StringComparison.OrdinalIgnoreCase) ? text.Substring(7) : text;
    }
}

/// <summary>
/// Represents the response from a thinking modusMentis LLM request.
/// </summary>
public class ThinkingResponse
{
    public string ReasoningText { get; set; } = "";
    public List<ParsedNarrativeAction> Actions { get; set; } = new();
}
