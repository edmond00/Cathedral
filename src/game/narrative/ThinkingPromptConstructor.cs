using System.Collections.Generic;
using System.Linq;
using Cathedral;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Builds the constrained-choice prompts for the thinking pipeline: GOAL (which sub-outcome to
/// pursue), HOW (which action skill to use), and PERSONA-FIT (how drawn the skill is to the action).
/// The reasoning and action *flavor* are produced separately via <see cref="PersonaRewriter"/>; the
/// persona itself is the slot's system prompt, so these prompts only carry a little situational
/// context (<see cref="SituationLine"/>) plus the options.
/// </summary>
public class ThinkingPromptConstructor
{
    /// <summary>
    /// Short "where you are / what you are attending to" preamble prepended to the choice prompts so
    /// the modus mentis has enough situational context to choose well. Names both the overall location
    /// (e.g. "a farm") and the specific area within it (e.g. "courtyard"), plus the observed object.
    /// Any part is omitted when its phrase is blank. Ends with a blank line so the options read cleanly.
    /// </summary>
    public static string SituationLine(string? overallLocation, string? areaLocation, string? observedPhrase)
    {
        var parts = new List<string>();

        bool hasOverall = !string.IsNullOrWhiteSpace(overallLocation);
        bool hasArea    = !string.IsNullOrWhiteSpace(areaLocation);
        if (hasOverall && hasArea)
            parts.Add($"You are in {NeutralNarration.NounPhrase(overallLocation)}, in the {areaLocation!.Trim()}.");
        else if (hasOverall)
            parts.Add($"You are in {NeutralNarration.NounPhrase(overallLocation)}.");
        else if (hasArea)
            parts.Add($"You are in the {areaLocation!.Trim()}.");

        if (!string.IsNullOrWhiteSpace(observedPhrase))
            parts.Add($"Your attention is on {NeutralNarration.NounPhrase(observedPhrase)}.");

        return parts.Count == 0 ? "" : string.Join(" ", parts) + "\n\n";
    }

    /// <summary>
    /// PERSONA-FIT: how strongly the action modus mentis is drawn to carrying out <paramref name="actionPhrase"/>.
    /// Asked on the action skill's own slot (so <see cref="ModusMentis.PersonaReminder"/> here is the
    /// skill's), just before the action-text rewrite. The five-way answer decides both whether the
    /// action happens at all (reluctant/opposed cancel it) and its difficulty modifier
    /// (eager −1 / willing 0 / unsure +1). Replaces the old plausibility + difficulty critic trees.
    /// </summary>
    public static string BuildPersonaFitPrompt(
        string actionPhrase,
        ModusMentis actionModusMentis,
        string? overallLocation = null,
        string? areaLocation = null,
        string? observedPhrase = null)
    {
        string reminderClause = actionModusMentis.PersonaReminder != null
            ? $"As a {actionModusMentis.PersonaReminder}, "
            : "";

        return $@"{SituationLine(overallLocation, areaLocation, observedPhrase)}You are considering whether to {actionPhrase}.

{reminderClause}how strongly are you drawn to this?
- eager: it fits you perfectly — you are keen to do it
- willing: you are willing to do it
- unsure: you are hesitant and unsure about it
- reluctant: you would rather not do it
- opposed: it goes against who you are — you refuse

{Config.Narrative.JsonFormatClause("{\"drawn\": \"...\"}")}";
    }
}
