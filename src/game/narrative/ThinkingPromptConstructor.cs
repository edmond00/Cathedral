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
        var kics = observation.ObservationKeywordsInContext;
        string keywordHint = kics.Count > 0
            ? $" You notice things like: {string.Join(", ", kics.Select(k => k.Context))}."
            : "";
        return BuildReflectPromptCore(
            observation.GenerateNeutralDescription(protagonist.CurrentLocationId),
            keywordHint, node, thinkingModusMentis, protagonist, worldContext);
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
        var kics = outcome.OutcomeKeywordsInContext;
        string keywordHint = kics.Count > 0
            ? $" You notice things like: {string.Join(", ", kics.Select(k => k.Context))}."
            : "";
        // Use GenerateNeutralDescription for types that have it (NarrationNode, ObservationObject),
        // so we get e.g. "the alder grove" rather than the transition verb "follow the draining".
        string description = outcome is NarrationNode nn ? nn.GenerateNeutralDescription(0)
            : outcome is ObservationObject ob ? ob.GenerateNeutralDescription(0)
            : outcome.DisplayName;
        return BuildReflectPromptCore(
            description,
            keywordHint, node, thinkingModusMentis, protagonist, worldContext);
    }

    private static string BuildReflectPromptCore(
        string description,
        string keywordHint,
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

You are observing {WithArticle(description)}.{keywordHint}
{reminderClause}what do you think?
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Call 0b (GOAL): follow-up in the same slot after REFLECT — asks which sub-outcome to pursue.
    /// Short continuation: no full context repeat since the slot already has it from REFLECT.
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
        KeywordInContext? keywordInContext = null)
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
        string attentionLine = keywordInContext != null
            ? $"Your attention is drawn to {keywordInContext.Context} of {WithArticle(outcomeLabel)}."
            : $"Your attention is drawn to {WithArticle(outcomeLabel)}.";
        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

{attentionLine} Now you want to {outcomeDescription}.

{reminderClause}why do you want this?
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
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
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2)}";
    }

    /// <summary>
    /// Call 3 (WHAT): asks the selected action modusMentis what it will concretely try to do.
    /// Sent to the action modusMentis's own slot (fresh context) — full context is provided
    /// since this instance has no prior conversation history.
    /// Includes the action modusMentis's persona tone at the head (first call in this slot).
    /// </summary>
    private static string WithArticle(string s) =>
        s.Length > 0 && "aeiouAEIOU".Contains(s[0]) ? $"an {s}" : $"a {s}";

    /// <summary>
    /// Builds the prompt asking the action modusMentis to reformulate an existing action text
    /// to incorporate a combined item.  Sent to the action modusMentis's own slot (fresh context).
    /// Style mirrors <see cref="BuildWhatPrompt"/>: direct, immersive, first-person.
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
You are holding: {combinedItem.ItemId} ({combinedItem.Description}).

{reminderClause}using your {actionModusMentis.DisplayName} skill ({actionModusMentis.ShortDescription}), describe simply what you will do, incorporating the use of {combinedItem.ItemId} in your action.
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

    public string BuildWhatPrompt(
        string keyword,
        KeywordInContext? keywordInContext,
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
        string noticedClause = keywordInContext != null ? keywordInContext.Context : keyword;
        string transition = targetOutcome.GetKeywordToOutcomeTransition(keyword, keywordInContext);

        return $@"{personaToneLine}{WorldContext.EpochContext}
{node.BuildLocationContext(worldContext, protagonist.CurrentLocationId)}

You noticed {noticedClause}. {transition} Now you want to {outcomeDescription}.

{reminderClause}using your {actionModusMentis.DisplayName} skill ({actionModusMentis.ShortDescription}), explain simply what you are going to try to do.
{Config.Narrative.AnswerInstructionFor(actionModusMentis.PersonaReminder2)}";
    }

}
