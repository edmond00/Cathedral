using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constructs prompts for observation modiMentis to generate environment perceptions.
/// Builds the user message that gets sent to the LLM with the cached persona prompt.
/// </summary>
public class ObservationPromptConstructor
{
    /// <summary>
    /// Builds an observation request for the LLM.
    /// Includes neutral description, available keywords grouped by outcome, and protagonist state.
    /// </summary>
    public string BuildObservationPrompt(
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis observationModusMentis,
        List<string> targetKeywords,
        bool promptKeywordUsage = true)
    {
        var prompt = $@"You are observing this scene:

{node.GenerateNeutralDescription(protagonist.CurrentLocationId)}";

        if (promptKeywordUsage && targetKeywords.Count > 0)
        {
            // Get keywords grouped by their source outcome
            var keywordsByOutcome = node.GetKeywordsByOutcome();
            
            // Filter to only include keywords that are in our target list
            var filteredKeywordsByOutcome = FilterKeywordsByTarget(keywordsByOutcome, targetKeywords);
            
            prompt += $@"

Notable elements you should describe in your narration using specific keywords (include 3-5 of these):
{FormatKeywordsByOutcome(filteredKeywordsByOutcome)}";
        }

        prompt += $@"

Generate a brief narration (50-300 characters) from your perspective that describes what you observe. Include specific details about the notable elements.

Write like {observationModusMentis.PersonaTone}.

Respond in JSON format:
{{
  ""narration_text"": ""your observation narration""
}}";

        return prompt;
    }
    
    /// <summary>
    /// Builds an observation request with keyword intro constraints for fallback.
    /// Used when the natural prompt fails to include enough keywords.
    /// Forces the LLM to start with a keyword intro to guarantee keyword inclusion.
    /// </summary>
    public string BuildObservationPromptWithIntros(
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis observationModusMentis,
        List<string> targetKeywords)
    {
        // Get a few intro examples
        // Generate intro examples dynamically from the first 3 keywords
        var keywords = targetKeywords.Take(3).ToList();
        var introExamples = keywords.Select(k => $"You notice {k}").ToList();
        
        var examplesText = string.Join("\n", introExamples.Select(intro => 
            $"Example: \"{intro} [continue observation...]\""));

        var prompt = $@"You are observing this scene:

{node.GenerateNeutralDescription(protagonist.CurrentLocationId)}

Your narration MUST begin with one of these keyword introductions:
{string.Join("\n", introExamples.Select(intro => $"- Start with: \"{intro}\""))}

{examplesText}

IMPORTANT: Your narration MUST:
1. Start with one of the exact phrases above
2. Include at least 3 of these keywords naturally: {string.Join(", ", targetKeywords)}
3. Be 50-300 characters total

Write like {observationModusMentis.PersonaTone}.

Respond in JSON format:
{{
  ""narration_text"": ""your observation starting with one of the required phrases""
}}";

        return prompt;
    }
    
    /// <summary>
    /// Formats keywords grouped by outcome for display in the prompt.
    /// Example output:
    ///   stream keywords: gurgling, cool, flowing
    ///   berry bush keywords: ripe, bush, branches
    /// </summary>
    private string FormatKeywordsByOutcome(Dictionary<string, List<string>> keywordsByOutcome)
    {
        var sb = new StringBuilder();
        
        foreach (var kvp in keywordsByOutcome)
        {
            if (kvp.Value.Count > 0)
            {
                sb.AppendLine($"{kvp.Key} keywords: {string.Join(", ", kvp.Value)}");
            }
        }
        
        return sb.ToString().TrimEnd();
    }
    
    /// <summary>
    /// Filters the keywords by outcome to only include keywords that are in the target list.
    /// </summary>
    private Dictionary<string, List<string>> FilterKeywordsByTarget(
        Dictionary<string, List<string>> keywordsByOutcome, 
        List<string> targetKeywords)
    {
        var targetSet = new HashSet<string>(targetKeywords, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, List<string>>();
        
        foreach (var kvp in keywordsByOutcome)
        {
            var filteredKeywords = kvp.Value
                .Where(k => targetSet.Contains(k))
                .ToList();
            
            if (filteredKeywords.Count > 0)
            {
                result[kvp.Key] = filteredKeywords;
            }
        }
        
        return result;
    }
}
