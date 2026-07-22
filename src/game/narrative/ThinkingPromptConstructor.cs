using System.Collections.Generic;
using Cathedral;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Shared situational context for the thinking pipeline's choice prompts (goal, skill, persona-fit,
/// observation focus). The choices themselves run through <see cref="PersonaChoiceSelector"/> —
/// persona reasoning mapped by the neutral <see cref="PersonaMatchCritic"/> — and the persona lives
/// in the slot's system prompt, so all a prompt needs from here is <see cref="SituationLine"/>.
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
}
