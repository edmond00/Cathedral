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
    /// Call 1 (WHY): asks the thinking modusMentis why observing the keyword makes it want the outcome.
    /// Includes the full persona tone at the head (first call in the batch).
    /// </summary>
    public string BuildWhyPrompt(
        string keyword,
        string outcomeDescription,
        NarrationNode node,
        ModusMentis thinkingModusMentis,
        Protagonist protagonist,
        WorldContext worldContext,
        ConcreteOutcome targetOutcome)
    {
        string personaToneLine = thinkingModusMentis.PersonaTone != null
            ? $"You are a {thinkingModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";
        string transition = targetOutcome.GetKeywordToOutcomeTransition(keyword);

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You noticed {keyword}. {transition} Now you want to {outcomeDescription}.

{reminderClause}why does what you observed make you want this?";
    }

    /// <summary>
    /// Call 2 (HOW): asks the thinking modusMentis which skill could help reach the outcome.
    /// Sent as a follow-up in the same slot context (the WHY reasoning is already in context).
    /// </summary>
    public string BuildHowPrompt(
        string outcomeDescription,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis)
    {
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";

        return $@"Your goal is to {outcomeDescription}.

How do you want to proceed?
{string.Join("\n", actionModiMentis.Select(s => $"- with {s.SkillMeans}"))}

{reminderClause}which approach could best help you achieve this goal, and how would you use it?";
    }

    /// <summary>
    /// Call 3 (WHAT): asks the selected action modusMentis what it will concretely try to do.
    /// Sent to the action modusMentis's own slot (fresh context) — full context is provided
    /// since this instance has no prior conversation history.
    /// Includes the action modusMentis's persona tone at the head (first call in this slot).
    /// </summary>
    public string BuildWhatPrompt(
        string keyword,
        string outcomeDescription,
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis actionModusMentis,
        WorldContext worldContext,
        ConcreteOutcome targetOutcome)
    {
        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";
        string transition = targetOutcome.GetKeywordToOutcomeTransition(keyword);

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You noticed {keyword}. {transition} Now you want to {outcomeDescription}.

{reminderClause}using your {actionModusMentis.DisplayName} skill ({actionModusMentis.ShortDescription}), what exactly are you going to try to do?";
    }

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

        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";

        return $@"You are a {thinkingModusMentis.PersonaTone}.
You are now in {node.GenerateNeutralDescription(protagonist.CurrentLocationId)}.

You notice {keywordContext} and start wondering what it could lead to.

How do you want to proceed?
{string.Join("\n", actionModiMentis.Select(s => $"- with {s.SkillMeans}"))}

What could happen:
{string.Join("\n", allOutcomes.Select(o => $"- {o}"))}

{reminderClause}what do you think and feel about {keywordContext}? Which approach could help you here? Propose 2-5 things you could try.";
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

        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";

        return $@"You are a {thinkingModusMentis.PersonaTone}.
You are now in {node.GenerateNeutralDescription(protagonist.CurrentLocationId)}.

You are wondering how you could handle {keywordContext}.

How do you want to proceed?
{string.Join("\n", actionModiMentis.Select(s => $"- with {s.SkillMeans}"))}

What could happen:
{string.Join("\n", allOutcomes.Select(o => $"- {o}"))}

{reminderClause}which approach could help? What do you think and feel?";
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
            ? $"Other than the approaches you already considered ({string.Join(", ", alreadyChosenSkills)}), which "
            : "Which ";

        string outcomeQuoted = $"\"{hardcodedOutcome}\"";
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, answer:\n"
            : "";

        return $@"You are wondering how you could achieve: {outcomeQuoted}.

How do you want to proceed?
{string.Join("\n", actionModiMentis.Select(s => $"- with {s.SkillMeans}"))}

{reminderClause}Question 1: How could I proceed to achieve {outcomeQuoted}?
Question 2: {avoidanceLine}approach is most appropriate and why?";
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
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";

        return $@"You have decided to proceed {hardcodedSkill} to achieve {outcomeQuoted}.

{reminderClause}what exactly will you try?";
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
