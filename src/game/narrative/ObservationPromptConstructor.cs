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
    /// Returns a human-readable label for an outcome: uses GenerateNeutralDescription for
    /// NarrationNode outcomes instead of the raw NodeId from DisplayName.
    private static string GetOutcomeLabel(ConcreteOutcome outcome) =>
        outcome is NarrationNode n ? n.GenerateNeutralDescription(0) : outcome.DisplayName;

    /// <summary>
    /// Builds the prompt for the FIRST sentence in a per-sentence observation batch.
    /// Provides the full scene context, persona reminder, and outcome keywords as hints.
    /// </summary>
    public string BuildFirstSentencePrompt(
        NarrationNode node,
        int locationId,
        ConcreteOutcome outcome,
        string personaTone)
    {
        var locationDescription = node.GenerateNeutralDescription(locationId);
        var outcomeKeywords = outcome is NarrationNode childNode ? childNode.NodeKeywords : outcome.OutcomeKeywords;
        
        return $@"You are observing: {locationDescription}

Write one sentence (20-120 characters) from the perspective of {personaTone}.
Focus specifically on: {GetOutcomeLabel(outcome)}
Include one of these keywords naturally: {string.Join(", ", outcomeKeywords)}

Respond in JSON format:
{{
  ""narration_text"": ""your single observation sentence""
}}";
    }

    /// <summary>
    /// Builds the prompt for a general scene description — the opening sentence of an overall observation.
    /// No specific outcome or keyword hints; describes the scene broadly from the persona's perspective.
    /// </summary>
    public string BuildGeneralDescriptionPrompt(NarrationNode node, int locationId, string personaTone)
    {
        var locationDescription = node.GenerateNeutralDescription(locationId);
        return $@"You are observing: {locationDescription}

Write one sentence (20-120 characters) as a general description of this scene from the perspective of {personaTone}.
Describe the overall atmosphere or most prominent feature. Do not mention specific interactable objects or named items — keep it broad.

Respond in JSON format:
{{
  ""narration_text"": ""your single general observation sentence""
}}";
    }

    /// <summary>
    /// Builds a continuation prompt that writes a short transition sentence linking
    /// the previous description to a specific outcome.
    /// </summary>
    public string BuildTransitionSentencePrompt(ConcreteOutcome outcome, string previousDescription)
    {
        return $@"The previous description was of: {previousDescription}.
Write one short transition sentence (15-80 characters) that bridges from there toward: {GetOutcomeLabel(outcome)}
The sentence should hint at or lead toward the subject without describing it in detail.

Respond in JSON format:
{{
  ""narration_text"": ""your single transition sentence""
}}";
    }

    /// <summary>
    /// Builds a continuation prompt focused on a specific outcome, including its keywords.
    /// </summary>
    public string BuildOutcomeDescriptionSentencePrompt(ConcreteOutcome outcome)
    {
        var outcomeKeywords = outcome is NarrationNode childNode ? childNode.NodeKeywords : outcome.OutcomeKeywords;
        return $@"Write one sentence (20-120 characters) focused on: {GetOutcomeLabel(outcome)}
Include one of these keywords naturally: {string.Join(", ", outcomeKeywords)}

Respond in JSON format:
{{
  ""narration_text"": ""your single outcome description sentence""
}}";
    }

    /// <summary>
    /// Builds a continuation prompt for subsequent sentences in the same observation batch.
    /// The slot already holds the prior sentences as conversation context.
    /// </summary>
    public string BuildContinuationSentencePrompt(ConcreteOutcome outcome)
    {
        var outcomeKeywords = outcome is NarrationNode childNode ? childNode.NodeKeywords : outcome.OutcomeKeywords;

        return $@"Write the next sentence in this observation.
Focus on: {GetOutcomeLabel(outcome)}
Include one of these keywords naturally: {string.Join(", ", outcomeKeywords)}

Respond in JSON format:
{{
  ""narration_text"": ""your single observation sentence""
}}";
    }


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
