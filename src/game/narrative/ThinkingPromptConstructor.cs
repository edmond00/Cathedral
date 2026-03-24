using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constructs prompts for thinking modiMentis to generate Chain-of-Thought reasoning and actions.
/// Builds the user message that gets sent to the LLM with the cached persona prompt.
/// </summary>
public class ThinkingPromptConstructor
{
    /// <summary>
    /// Builds a thinking request for the LLM.
    /// Includes keyword context, possible outcomes, available action modiMentis, and protagonist state.
    /// Separates straightforward outcomes from circuitous outcomes in the prompt.
    /// </summary>
    /// <param name="keyword">The keyword that was clicked</param>
    /// <param name="keywordSourceOutcome">The outcome/element that the keyword relates to (e.g., "berry bush")</param>
    /// <param name="node">The current narration node</param>
    /// <param name="outcomesWithMetadata">Possible outcomes with circuitous metadata</param>
    /// <param name="actionModiMentis">Available action modiMentis</param>
    /// <param name="protagonist">The player protagonist</param>
    /// <param name="thinkingModusMentis">The thinking modusMentis being used</param>
    public string BuildThinkingPrompt(
        string keyword,
        string? keywordSourceOutcome,
        NarrationNode node,
        List<OutcomeWithMetadata> outcomesWithMetadata,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        ModusMentis thinkingModusMentis)
    {
        // Separate straightforward and circuitous outcomes
        var straightforwardOutcomes = outcomesWithMetadata
            .Where(o => !o.IsCircuitous)
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();
            
        var circuitousOutcomes = outcomesWithMetadata
            .Where(o => o.IsCircuitous)
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();
        
        // Get action modusMentis IDs
        var actionModusMentisIds = actionModiMentis.Select(s => s.ModusMentisId).ToList();
        
        // Build outcomes section with separation
        var outcomesSection = new System.Text.StringBuilder();
        outcomesSection.AppendLine("What could happen:");
        foreach (var outcome in straightforwardOutcomes)
        {
            outcomesSection.AppendLine($"- {outcome}");
        }
        
        if (circuitousOutcomes.Count > 0)
        {
            outcomesSection.AppendLine();
            outcomesSection.AppendLine("Other possibilities (harder to achieve):");
            foreach (var outcome in circuitousOutcomes)
            {
                outcomesSection.AppendLine($"- {outcome}");
            }
        }
        
        // Build the attention line with optional outcome context
        string attentionLine = string.IsNullOrEmpty(keywordSourceOutcome)
            ? $@"Your attention is drawn to: ""{keyword}"""
            : $@"Your attention is drawn to the ""{keyword}"" aspect of ""{keywordSourceOutcome}""";
        
        // Build the think line with optional outcome context
        string thinkLine = string.IsNullOrEmpty(keywordSourceOutcome)
            ? $@"Think about what ""{keyword}"" suggests to you. What possibilities does it open?"
            : $@"Think about what ""{keyword}"" suggests to you in the context of observing ""{keywordSourceOutcome}"". What possibilities does it open?";
        
        var prompt = $@"{attentionLine}

Current situation:
{node.GenerateNeutralDescription(protagonist.CurrentLocationId)}

ModiMentis you can apply:
{string.Join("\n", actionModiMentis.Select(s => $"- {s.DisplayName}: {s.ShortDescription}"))}

{outcomesSection.ToString().TrimEnd()}

{thinkLine}

First, express your internal thoughts about this element—why it catches your interest,
what connections you see, how it relates to your capabilities.

Then propose 2–5 specific things you could try.

Guidelines:
- Think and speak as {thinkingModusMentis.PersonaTone}.
- Your perspective and instincts drive everything.
- Connect ""{keyword}"" naturally to what you can do.
- Each action begins with 'try to'.
- Let your reasoning flow into your proposed actions.

Output fields:
- reasoning_text
- actions[] with: action_modusMentis, outcome, action_description
";

        return prompt;
    }
    
    /// <summary>
    /// Builds the reasoning-only first prompt for the batched thinking pipeline.
    /// Asks the LLM for its internal reasoning about the keyword — no actions yet.
    /// The slot retains this context for all subsequent action calls in the same batch.
    /// </summary>
    public string BuildReasoningPrompt(
        string keyword,
        string? keywordSourceOutcome,
        NarrationNode node,
        List<OutcomeWithMetadata> outcomesWithMetadata,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        ModusMentis thinkingModusMentis)
    {
        var straightforwardOutcomes = outcomesWithMetadata
            .Where(o => !o.IsCircuitous)
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();

        var circuitousOutcomes = outcomesWithMetadata
            .Where(o => o.IsCircuitous)
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();

        var outcomesSection = new System.Text.StringBuilder();
        outcomesSection.AppendLine("What could happen:");
        foreach (var outcome in straightforwardOutcomes)
            outcomesSection.AppendLine($"- {outcome}");

        if (circuitousOutcomes.Count > 0)
        {
            outcomesSection.AppendLine();
            outcomesSection.AppendLine("Other possibilities (harder to achieve):");
            foreach (var outcome in circuitousOutcomes)
                outcomesSection.AppendLine($"- {outcome}");
        }

        string attentionLine = string.IsNullOrEmpty(keywordSourceOutcome)
            ? $@"Your attention is drawn to: ""{keyword}"""
            : $@"Your attention is drawn to the ""{keyword}"" aspect of ""{keywordSourceOutcome}""";

        string thinkLine = string.IsNullOrEmpty(keywordSourceOutcome)
            ? $@"Think about what ""{keyword}"" suggests to you. What possibilities does it open?"
            : $@"Think about what ""{keyword}"" suggests to you in the context of observing ""{keywordSourceOutcome}"". What possibilities does it open?";

        return $@"{attentionLine}

Current situation:
{node.GenerateNeutralDescription(protagonist.CurrentLocationId)}

Skills you can apply:
{string.Join("\n", actionModiMentis.Select(s => $"- {s.DisplayName}: {s.ShortDescription}"))}

{outcomesSection.ToString().TrimEnd()}

{thinkLine}

Express your internal thoughts about this element — why it catches your interest,
what connections you see, how it relates to your capabilities.
For each possible outcome, consider which skill would be most effective to achieve it.
Speak as {thinkingModusMentis.PersonaTone}.

Output field:
- reasoning_text
";
    }

    /// <summary>
    /// Builds a single-action prompt for the batched thinking pipeline.
    /// Called once per action after the reasoning call; the slot keeps prior context as CoT.
    /// The outcome is pre-assigned — the LLM must choose the appropriate skill and write the description.
    /// </summary>
    public string BuildSingleActionPrompt(
        int actionIndex,
        int totalActions,
        ModusMentis thinkingModusMentis,
        string hardcodedOutcome)
    {
        return $@"As {thinkingModusMentis.PersonaTone}, propose action {actionIndex} of {totalActions}.

Your outcome is fixed: ""{hardcodedOutcome}"".
Choose the skill best suited to achieve this outcome, then describe what you will try to do.

Output fields:
- outcome (must be ""{hardcodedOutcome}"")
- skill
- action_description (must start with ""try to "")
";
    }

    /// <summary>
    /// Legacy overload that accepts plain outcomes (treats all as straightforward).
    /// Does not include outcome context for the keyword.
    /// </summary>
    public string BuildThinkingPrompt(
        string keyword,
        NarrationNode node,
        List<OutcomeBase> possibleOutcomes,
        List<ModusMentis> actionModiMentis,
        Protagonist protagonist,
        ModusMentis thinkingModusMentis)
    {
        var outcomesWithMetadata = possibleOutcomes
            .Select(o => OutcomeWithMetadata.Straightforward(o))
            .ToList();
            
        return BuildThinkingPrompt(keyword, null, node, outcomesWithMetadata, actionModiMentis, protagonist, thinkingModusMentis);
    }
}
