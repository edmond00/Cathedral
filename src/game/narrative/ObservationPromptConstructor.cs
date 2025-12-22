using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constructs prompts for observation skills to generate environment perceptions.
/// Builds the user message that gets sent to the LLM with the cached persona prompt.
/// </summary>
public class ObservationPromptConstructor
{
    /// <summary>
    /// Builds an observation request for the LLM.
    /// Includes neutral description, available keywords, and avatar state.
    /// </summary>
    public string BuildObservationPrompt(
        NarrationNode node,
        Avatar avatar,
        bool promptKeywordUsage = true)
    {
        var prompt = $@"You are observing this scene:

{node.GenerateNeutralDescription(avatar.CurrentLocationId)}";

        if (promptKeywordUsage && node.Keywords.Count > 0)
        {
            prompt += $@"

Notable elements you should describe in your narration (include 3-5 of these):
{string.Join(", ", node.Keywords)}";
        }

        prompt += @"

Generate a brief narration (50-300 characters) from your perspective that describes what you observe. Include specific details about the notable elements.

Respond in JSON format:
{
  ""narration_text"": ""your observation narration""
}";

        return prompt;
    }
    
    /// <summary>
    /// Builds an observation request with keyword intro constraints for fallback.
    /// Used when the natural prompt fails to include enough keywords.
    /// Forces the LLM to start with a keyword intro to guarantee keyword inclusion.
    /// </summary>
    public string BuildObservationPromptWithIntros(
        NarrationNode node,
        Avatar avatar)
    {
        // Get a few intro examples
        // Generate intro examples dynamically from the first 3 keywords
        var keywords = node.Keywords.Take(3).ToList();
        var introExamples = keywords.Select(k => $"You notice {k}").ToList();
        
        var examplesText = string.Join("\n", introExamples.Select(intro => 
            $"Example: \"{intro} [continue observation...]\""));

        var prompt = $@"You are observing this scene:

{node.GenerateNeutralDescription(avatar.CurrentLocationId)}

Your narration MUST begin with one of these keyword introductions:
{string.Join("\n", introExamples.Select(intro => $"- Start with: \"{intro}\""))}

{examplesText}

IMPORTANT: Your narration MUST:
1. Start with one of the exact phrases above
2. Include at least 3 of these keywords naturally: {string.Join(", ", node.Keywords)}
3. Be 50-300 characters total

Respond in JSON format:
{{
  ""narration_text"": ""your observation starting with one of the required phrases""
}}";

        return prompt;
    }
}
