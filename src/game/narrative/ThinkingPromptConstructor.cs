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
    /// Call 0a (REFLECT): asks the thinking modusMentis what it thinks about the observation as a whole.
    /// This is the FIRST call in the GOAL batch — resets the slot and provides full context.
    /// The response text is prepended to the final reasoning block shown to the player.
    /// </summary>
    public static string BuildReflectPrompt(
        ObservationObject observation,
        NarrationNode node,
        ModusMentis thinkingModusMentis,
        Protagonist protagonist,
        WorldContext worldContext)
    {
        return BuildReflectPromptCore(
            observation.GenerateNeutralDescription(protagonist.CurrentLocationId),
            node, thinkingModusMentis, protagonist, worldContext);
    }

    /// <summary>
    /// Call 0a (REFLECT) overload for a plain ConcreteOutcome (single-outcome targets).
    /// </summary>
    public static string BuildReflectPrompt(
        ConcreteOutcome outcome,
        NarrationNode node,
        ModusMentis thinkingModusMentis,
        Protagonist protagonist,
        WorldContext worldContext)
    {
        string description = outcome is NarrationNode nn ? nn.GenerateNeutralDescription(0)
            : outcome is ObservationObject ob ? ob.GenerateNeutralDescription(0)
            : outcome.DisplayName;
        return BuildReflectPromptCore(description, node, thinkingModusMentis, protagonist, worldContext);
    }

    private static string BuildReflectPromptCore(
        string description,
        NarrationNode node,
        ModusMentis thinkingModusMentis,
        Protagonist protagonist,
        WorldContext worldContext)
    {
        string personaToneLine = thinkingModusMentis.PersonaTone != null
            ? $"You are a {thinkingModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You are observing {WithArticle(description)}.
{reminderClause}what do you think?
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Call 0b (GOAL): follow-up in the same slot after REFLECT — asks which sub-outcome to pursue.
    /// <paramref name="goalOptions"/> must include the "ignore and move on" sentinel string.
    /// </summary>
    public static string BuildGoalPrompt(
        IEnumerable<string> goalOptions,
        ModusMentis thinkingModusMentis)
    {
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";
        string optionsList = string.Join("\n", goalOptions.Select(o => $"- {o}"));

        return $@"You could:
{optionsList}

{reminderClause}what do you want to do?
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
    }

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
        ConcreteOutcome targetOutcome,
        string questionText)
    {
        string personaToneLine = thinkingModusMentis.PersonaTone != null
            ? $"You are a {thinkingModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";
        string outcomeLabel = targetOutcome is NarrationNode n
            ? n.GenerateNeutralDescription(protagonist.CurrentLocationId)
            : targetOutcome is ObservationObject obs ? obs.GenerateNeutralDescription(0)
            : targetOutcome.DisplayName;

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

Your attention is drawn to {WithArticle(outcomeLabel)}. Now you want to {outcomeDescription}.

{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Call 2 (HOW): asks the thinking modusMentis which skill could help reach the outcome.
    /// </summary>
    public string BuildHowPrompt(
        string outcomeDescription,
        List<ModusMentis> actionModiMentis,
        ModusMentis thinkingModusMentis,
        string questionText)
    {
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";

        return $@"Your goal is to {outcomeDescription}.

You could proceed:
{string.Join("\n", actionModiMentis.Select(s => $"- with {s.SkillMeans}"))}

{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
    }

    private static string WithArticle(string s) =>
        s.Length > 0 && "aeiouAEIOU".Contains(s[0]) ? $"an {s}" : $"a {s}";

    /// <summary>
    /// Builds the prompt asking the action modusMentis to reason about how the item can help.
    /// </summary>
    public string BuildItemReasoningPrompt(
        string originalActionText,
        Item combinedItem,
        ModusMentis actionModusMentis,
        NarrationNode node,
        Protagonist protagonist,
        WorldContext worldContext)
    {
        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You are about to: {originalActionText}.
You are in possession of {combinedItem.WithArticle()} ({combinedItem.DescriptionLower()}).

{reminderClause}in two or three sentences, explain how you could use {combinedItem.WithArticle()} to help with this action.
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Builds the prompt asking the action modusMentis to reformulate an action incorporating an item.
    /// </summary>
    public string BuildItemReformulationPrompt(
        string originalActionText,
        Item combinedItem,
        ModusMentis actionModusMentis,
        NarrationNode node,
        Protagonist protagonist,
        WorldContext worldContext)
    {
        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You are about to: {originalActionText}.
You are in possession of {combinedItem.WithArticle()} ({combinedItem.DescriptionLower()}).

{reminderClause}expert in {actionModusMentis.ShortDescription}, describe simply what you will do, incorporating the use of {combinedItem.WithArticle()} in your action.
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

    public string BuildWhatPrompt(
        string keyword,
        string outcomeDescription,
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis actionModusMentis,
        WorldContext worldContext,
        string questionText)
    {
        string personaToneLine = actionModusMentis.PersonaTone != null
            ? $"You are a {actionModusMentis.PersonaTone}.\n"
            : "";
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";
        string formattedQuestion = string.Format(questionText, actionModusMentis.ShortDescription).TrimEnd('.', '?', '!')
            + $" in order to {outcomeDescription}? Write a simple action naturally leading to the goal.";

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You noticed {keyword}. Now you want to {outcomeDescription}.

{reminderClause}{formattedQuestion}
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

}
