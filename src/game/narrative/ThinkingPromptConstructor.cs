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
        var allOutcomes = outcomesWithMetadata
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();

        string keywordContext = string.IsNullOrEmpty(keywordSourceOutcome)
            ? $"\"{keyword}\""
            : $"\"{keyword}\" (noticed while observing \"{keywordSourceOutcome}\")";

        return $@"You are a {thinkingModusMentis.PersonaTone}.
You are now in {node.GenerateNeutralDescription(protagonist.CurrentLocationId)}.

You notice {keywordContext} and start wondering what it could lead to.

Among your skills:
{string.Join("\n", actionModiMentis.Select(s => $"- {s.DisplayName}: {s.ShortDescription}"))}

What could happen:
{string.Join("\n", allOutcomes.Select(o => $"- {o}"))}

What do you think and feel about {keywordContext}? Which of your skills could help you here? Propose 2-5 things you could try.";
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
        var allOutcomes = outcomesWithMetadata
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();

        string keywordContext = string.IsNullOrEmpty(keywordSourceOutcome)
            ? $"\"{keyword}\""
            : $"\"{keyword}\" (noticed while observing \"{keywordSourceOutcome}\")";

        return $@"You are a {thinkingModusMentis.PersonaTone}.
You are now in {node.GenerateNeutralDescription(protagonist.CurrentLocationId)}.

You are wondering how you could handle {keywordContext}.

Among your skills:
{string.Join("\n", actionModiMentis.Select(s => $"- {s.DisplayName}: {s.ShortDescription}"))}

What could happen:
{string.Join("\n", allOutcomes.Select(o => $"- {o}"))}

Which of your skills could help? What do you think and feel?";
    }

    /// <summary>
    /// Builds the intermediate skill-selection prompt (step 3a).
    /// The outcome is fixed; the LLM reasons about skills then picks one.
    /// When <paramref name="alreadyChosenSkills"/> is non-empty, the LLM is asked to
    /// prefer a different skill from those already selected.
    /// </summary>
    public string BuildSkillSelectionPrompt(
        string hardcodedOutcome,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis,
        List<string> alreadyChosenSkills)
    {
        string avoidanceLine = alreadyChosenSkills.Count > 0
            ? $"Other than the skills you already considered ({string.Join(", ", alreadyChosenSkills)}), which "
            : "Which ";

        string outcomeQuoted = $"\"{hardcodedOutcome}\"";
        return $@"You are wondering how you could achieve: {outcomeQuoted}.

Among your skills:
{string.Join("\n", actionModiMentis.Select(s => $"- {s.DisplayName}: {s.ShortDescription}"))}

Question 1: How could my skills help me achieve {outcomeQuoted}?
Question 2: {avoidanceLine}skill is most appropriate and why?";
    }

    /// <summary>
    /// Builds a single-action prompt for the batched thinking pipeline (step 3b).
    /// Both outcome and skill are fixed; the LLM only writes the action description.
    /// </summary>
    public string BuildSingleActionPrompt(
        int actionIndex,
        int totalActions,
        ModusMentis thinkingModusMentis,
        string hardcodedOutcome,
        string hardcodedSkill)
    {
        string outcomeQuoted = $"\"{hardcodedOutcome}\"";
        return $@"You have decided to use {hardcodedSkill} to achieve {outcomeQuoted}.

What exactly will you try?";
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
