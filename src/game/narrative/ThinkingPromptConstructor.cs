using System.Collections.Generic;
using System.Linq;
using Cathedral;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Builds the two surviving constrained-choice prompts for the thinking pipeline: GOAL (which
/// sub-outcome to pursue) and HOW (which action skill to use). The reasoning and action *flavor*
/// are produced separately via <see cref="PersonaRewriter"/>; the persona itself is the slot's
/// system prompt, so these prompts only carry the options.
/// </summary>
public class ThinkingPromptConstructor
{
    /// <summary>
    /// GOAL: pick which sub-outcome to pursue. <paramref name="goalOptions"/> must include the
    /// "ignore and move on" sentinel.
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
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2, "{\"goal\": \"...\"}")}";
    }

    /// <summary>
    /// HOW: pick which action skill to use to reach the goal.
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

{reminderClause}which approach do you take?
{Config.Narrative.AnswerInstructionFor(thinkingModusMentis.PersonaReminder2, "{\"how\": \"...\"}")}";
    }
}
