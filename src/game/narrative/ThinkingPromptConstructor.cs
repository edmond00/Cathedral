using System.Collections.Generic;
using System.Linq;
using Cathedral;

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
        string outcomeLabel = targetOutcome is NarrationNode n
            ? n.GenerateNeutralDescription(protagonist.CurrentLocationId)
            : targetOutcome.DisplayName;
        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

Your attention is drawn to {outcomeLabel}. Now you want to {outcomeDescription}.

{reminderClause}why do you want this?
{Config.Narrative.AnswerInstruction}";
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

You could proceed:
{string.Join("\n", actionModiMentis.Select(s => $"- with {s.SkillMeans}"))}

{reminderClause}what approach will you take and why?
{Config.Narrative.AnswerInstruction}";
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

{reminderClause}using your {actionModusMentis.DisplayName} skill ({actionModusMentis.ShortDescription}), what exactly are you going to try to do?
{Config.Narrative.AnswerInstruction}";
    }

}
