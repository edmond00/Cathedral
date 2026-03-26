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
        string personaTone,
        string biomeType)
    {
        var locationContext = node.BuildLocationContext(biomeType, locationId);
        var outcomeKeywords = outcome is NarrationNode childNode ? childNode.NodeKeywords : outcome.OutcomeKeywords;

        return $@"You are a {personaTone}.
{locationContext}
Your attention is drawn to {GetOutcomeLabel(outcome)}.
What do you feel and observe? (include one of: {string.Join(", ", outcomeKeywords)})";
    }

    /// <summary>
    /// Builds the prompt for a general scene description — the opening sentence of an overall observation.
    /// Includes node keywords as atmospheric hints (not clickable, just context).
    /// </summary>
    public string BuildGeneralDescriptionPrompt(NarrationNode node, int locationId, string personaTone, string biomeType)
    {
        var locationContext = node.BuildLocationContext(biomeType, locationId);
        var nodeKeywords = node.NodeKeywords;

        string keywordHint = nodeKeywords.Count > 0
            ? $"\nYou may notice things like: {string.Join(", ", nodeKeywords)}."
            : "";

        return $@"You are a {personaTone}.
{locationContext}{keywordHint}
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

}
