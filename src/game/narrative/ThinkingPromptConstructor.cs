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
    /// "ignore and move on" sentinel. <paramref name="observedPhrase"/> names the object being
    /// observed (e.g. "a beech tree"), shown as context before the options when supplied.
    /// Only the JSON-format clause is appended — this is a constrained choice, not styled prose.
    /// </summary>
    public static string BuildGoalPrompt(
        IEnumerable<string> goalOptions,
        ModusMentis thinkingModusMentis,
        string? observedPhrase = null)
    {
        string reminderClause = thinkingModusMentis.PersonaReminder != null
            ? $"As a {thinkingModusMentis.PersonaReminder}, "
            : "";
        string contextClause = string.IsNullOrWhiteSpace(observedPhrase)
            ? ""
            : $"You are observing {observedPhrase}. ";
        string optionsList = string.Join("\n", goalOptions.Select(o => $"- {o}"));

        return $@"{contextClause}You could:
{optionsList}

{reminderClause}what do you want to do?
{Config.Narrative.JsonFormatClause("{\"goal\": \"...\"}")}";
    }

    /// <summary>
    /// PERSONA-FIT: how strongly the action modus mentis is drawn to carrying out <paramref name="actionPhrase"/>.
    /// Asked on the action skill's own slot (so <see cref="ModusMentis.PersonaReminder"/> here is the
    /// skill's), just before the action-text rewrite. The five-way answer decides both whether the
    /// action happens at all (reluctant/opposed cancel it) and its difficulty modifier
    /// (eager −1 / willing 0 / unsure +1). Replaces the old plausibility + difficulty critic trees.
    /// </summary>
    public static string BuildPersonaFitPrompt(string actionPhrase, ModusMentis actionModusMentis)
    {
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        return $@"You are considering whether to {actionPhrase}.

{reminderClause}how strongly are you drawn to this?
- eager: it fits you perfectly — you are keen to do it
- willing: you are willing to do it
- unsure: you are hesitant and unsure about it
- reluctant: you would rather not do it
- opposed: it goes against who you are — you refuse

{Config.Narrative.JsonFormatClause("{\"drawn\": \"...\"}")}";
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
{Config.Narrative.JsonFormatClause("{\"how\": \"...\"}")}";
    }
}
