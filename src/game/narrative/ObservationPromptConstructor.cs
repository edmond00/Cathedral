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
        var prompt = $@"You are in this environment:

{node.NeutralDescription}";

        if (promptKeywordUsage && node.Keywords.Count > 0)
        {
            prompt += $@"

Notable elements you might observe (try to include 3-5 of these):
{string.Join("\n", node.Keywords.Select(k => $"- {k}"))}";
        }

        prompt += @"

Generate a narration of your observations. Your narration must be between 50 and 300 characters.

Respond in JSON format:
{
  ""narration_text"": ""string (your observation narration)"",
  ""highlighted_keywords"": [""keyword1"", ""keyword2"", ...]
}";

        return prompt;
    }
    
    /// <summary>
    /// Builds an observation request with keyword intro constraints for fallback.
    /// Used when the natural prompt fails to include enough keywords.
    /// </summary>
    public string BuildObservationPromptWithIntros(
        NarrationNode node,
        Avatar avatar)
    {
        var introExamples = string.Join("\n", node.KeywordIntroExamples
            .Select(kvp => $"- {kvp.Key}: \"{kvp.Value}\""));

        var prompt = $@"You are in this environment:

{node.NeutralDescription}

Your narration MUST start with one of these phrases:
{introExamples}

Generate a narration of your observations. Your narration must be between 50 and 300 characters and MUST include at least 3 of the notable elements.

Respond in JSON format:
{{
  ""narration_text"": ""string (your observation narration)"",
  ""highlighted_keywords"": [""keyword1"", ""keyword2"", ...]
}}";

        return prompt;
    }
}
