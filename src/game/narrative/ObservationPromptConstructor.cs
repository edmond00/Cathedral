using Cathedral;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constructs prompts for observation modiMentis to generate environment perceptions.
/// Builds the user message that gets sent to the LLM with the cached persona prompt.
/// </summary>
public class ObservationPromptConstructor
{
    private static string WithArticle(string s) =>
        s.Length > 0 && "aeiouAEIOU".Contains(s[0]) ? $"an {s}" : $"a {s}";

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
        WorldContext worldContext,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        var locationContext = node.BuildLocationContext(worldContext, locationId);
        var outcomeKics = outcome is NarrationNode childNode ? childNode.NodeKeywordsInContext : outcome.OutcomeKeywordsInContext;
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string keywordHint = outcomeKics.Count > 0
            ? $" You may notice things like: {string.Join(", ", outcomeKics.Select(k => k.Context))}."
            : "";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}
Your attention is drawn to {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}what do you feel and observe?{keywordHint}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds the prompt for a general scene description — the opening sentence of an overall observation.
    /// Includes node keywords as atmospheric hints (not clickable, just context).
    /// </summary>
    public string BuildGeneralDescriptionPrompt(NarrationNode node, int locationId, string personaTone, WorldContext worldContext, string? personaReminder = null, string? personaReminder2 = null)
    {
        var locationContext = node.BuildLocationContext(worldContext, locationId);
        var nodeKics = node.NodeKeywordsInContext;

        string keywordHint = nodeKics.Count > 0
            ? $"\nYou may notice things like: {string.Join(", ", nodeKics.Select(k => k.Context))}."
            : "";

        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}{keywordHint}
{reminderClause}what do you feel and observe?
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds a continuation prompt that writes a short transition sentence linking
    /// the previous description to a specific outcome.
    /// </summary>
    public string BuildTransitionSentencePrompt(ConcreteOutcome outcome, string previousDescription, string? personaReminder = null, KeywordInContext? previousKeywordInContext = null, string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string observingClause = previousKeywordInContext != null
            ? $"{previousKeywordInContext.Context} of {WithArticle(previousDescription)}"
            : WithArticle(previousDescription);
        return $@"You were observing {observingClause} but now you notice {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}what catches your attention?
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds a continuation prompt focused on a specific outcome, including its keywords.
    /// </summary>
    public string BuildOutcomeDescriptionSentencePrompt(ConcreteOutcome outcome, string? personaReminder = null, string? personaReminder2 = null)
    {
        var outcomeKics = outcome is NarrationNode childNode ? childNode.NodeKeywordsInContext : outcome.OutcomeKeywordsInContext;
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string keywordHint = outcomeKics.Count > 0
            ? $" You may notice things like: {string.Join(", ", outcomeKics.Select(k => k.Context))}."
            : "";
        return $@"You are now looking at {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}what do you observe?{keywordHint}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

}
