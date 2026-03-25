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
        
        return $@"You are a {personaTone}.
You are now in {locationDescription}.
Your attention is drawn to {GetOutcomeLabel(outcome)}.
What do you feel and observe? (include one of: {string.Join(", ", outcomeKeywords)})";
    }

    /// <summary>
    /// Builds the prompt for a general scene description — the opening sentence of an overall observation.
    /// No specific outcome or keyword hints; describes the scene broadly from the persona's perspective.
    /// </summary>
    public string BuildGeneralDescriptionPrompt(NarrationNode node, int locationId, string personaTone)
    {
        var locationDescription = node.GenerateNeutralDescription(locationId);
        return $@"You are a {personaTone}.
You are now in {locationDescription}.
What do you feel and observe?";
    }

    /// <summary>
    /// Builds a continuation prompt that writes a short transition sentence linking
    /// the previous description to a specific outcome.
    /// </summary>
    public string BuildTransitionSentencePrompt(ConcreteOutcome outcome, string previousDescription)
    {
        return $@"You were observing {previousDescription} but now you notice {GetOutcomeLabel(outcome)}.
What catches your attention?";
    }

    /// <summary>
    /// Builds a continuation prompt focused on a specific outcome, including its keywords.
    /// </summary>
    public string BuildOutcomeDescriptionSentencePrompt(ConcreteOutcome outcome)
    {
        var outcomeKeywords = outcome is NarrationNode childNode ? childNode.NodeKeywords : outcome.OutcomeKeywords;
        return $@"You are now looking at {GetOutcomeLabel(outcome)}.
What do you observe? (include one of: {string.Join(", ", outcomeKeywords)})";
    }

    /// <summary>
    /// Builds a continuation prompt for subsequent sentences in the same observation batch.
    /// The slot already holds the prior sentences as conversation context.
    /// </summary>
    public string BuildContinuationSentencePrompt(ConcreteOutcome outcome)
    {
        var outcomeKeywords = outcome is NarrationNode childNode ? childNode.NodeKeywords : outcome.OutcomeKeywords;

        return $@"What else do you notice? (include one of: {string.Join(", ", outcomeKeywords)})";
    }


    public string BuildObservationPrompt(
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis observationModusMentis,
        List<string> targetKeywords,
        bool promptKeywordUsage = true)
    {
        var locationDescription = node.GenerateNeutralDescription(protagonist.CurrentLocationId);
        var prompt = $"You are a {observationModusMentis.PersonaTone}.\nYou are now in {locationDescription}.";

        if (promptKeywordUsage && targetKeywords.Count > 0)
        {
            var keywordsByOutcome = node.GetKeywordsByOutcome();
            var filteredKeywordsByOutcome = FilterKeywordsByTarget(keywordsByOutcome, targetKeywords);
            
            prompt += $"\nYou notice some elements around you:\n{FormatKeywordsByOutcome(filteredKeywordsByOutcome)}";
        }

        prompt += "\nWhat do you feel and observe? Try to mention 3-5 of those elements naturally.";

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
        var locationDescription = node.GenerateNeutralDescription(protagonist.CurrentLocationId);
        var firstKeyword = targetKeywords.FirstOrDefault() ?? "something";

        return $@"You are a {observationModusMentis.PersonaTone}.
You are now in {locationDescription}.
What do you notice first? Start your response with ""I notice {firstKeyword}"".
Try to include at least 3 of: {string.Join(", ", targetKeywords)}";
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
