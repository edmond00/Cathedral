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
    /// NarrationNode and ObservationObject outcomes instead of the raw id from DisplayName.
    private static string GetOutcomeLabel(ConcreteOutcome outcome) =>
        outcome is NarrationNode n ? n.GenerateNeutralDescription(0) :
        outcome is ObservationObject obs ? obs.GenerateNeutralDescription(0) :
        outcome.DisplayName;

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
        string questionText,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        var locationContext = node.BuildLocationContext(worldContext, locationId);
        var outcomeKics = outcome is NarrationNode childNode ? childNode.NodeKeywordsInContext
            : outcome is ObservationObject obs ? obs.ObservationKeywordsInContext
            : outcome.OutcomeKeywordsInContext;
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string keywordHint = outcomeKics.Count > 0
            ? $" You may notice things like: {string.Join(", ", outcomeKics.Select(k => k.Context))}."
            : "";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}
Your attention is drawn to {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}{questionText}{keywordHint}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds the prompt for a general scene description — the opening sentence of an overall observation.
    /// Includes node keywords as atmospheric hints (not clickable, just context).
    /// </summary>
    public string BuildGeneralDescriptionPrompt(NarrationNode node, int locationId, string personaTone, WorldContext worldContext, string questionText, string? personaReminder = null, string? personaReminder2 = null)
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
{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds a continuation prompt that writes a short transition sentence linking
    /// the previous description to a specific outcome.
    /// </summary>
    public string BuildTransitionSentencePrompt(ConcreteOutcome outcome, string previousDescription, string questionText, string? personaReminder = null, KeywordInContext? previousKeywordInContext = null, string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string observingClause = previousKeywordInContext != null
            ? $"{previousKeywordInContext.Context} of {WithArticle(previousDescription)}"
            : WithArticle(previousDescription);
        return $@"You were observing {observingClause} but now you notice {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Chained speaking — request 1 of 3.
    /// Establishes full scene context and asks the speaker to call the companion's attention
    /// to what they are observing. No keyword hints — sets up context for the chain.
    /// </summary>
    public string BuildSpeakingAttentionPrompt(
        NarrationNode node,
        int locationId,
        ConcreteOutcome linkedOutcome,
        string keyword,
        KeywordInContext? keywordInContext,
        string companionName,
        string? personaTone,
        WorldContext worldContext,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        var locationContext = node.BuildLocationContext(worldContext, locationId);
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string contextClause = keywordInContext != null
            ? $"You are thinking about {keywordInContext.Context}."
            : $"You are thinking about {keyword}.";
        string personaReminder2Clause = personaReminder2 != null
            ? $" Stay in the character of {personaReminder2}."
            : " Stay in character.";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}
{contextClause} You want to share this with your companion {companionName}.
{reminderClause}In one sentence, call {companionName}'s attention to {WithArticle(GetOutcomeLabel(linkedOutcome))} you are observing.
{Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Chained speaking — request 2 of 3.
    /// Continuation prompt (no full context needed — LLM already has the scene).
    /// Asks the speaker to describe the observation using the keyword list.
    /// Keywords from this sentence are extracted and made clickable.
    /// </summary>
    public string BuildSpeakingDescriptionPrompt(
        ConcreteOutcome linkedOutcome,
        string companionName,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        var outcomeKics = linkedOutcome is NarrationNode nn ? nn.NodeKeywordsInContext
            : linkedOutcome is ObservationObject obs ? obs.ObservationKeywordsInContext
            : linkedOutcome.OutcomeKeywordsInContext;
        string keywordHint = outcomeKics.Count > 0
            ? $" You notice things like: {string.Join(", ", outcomeKics.Select(k => k.Context))}."
            : "";
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string personaReminder2Clause = personaReminder2 != null
            ? $" Stay in the character of {personaReminder2}."
            : " Stay in character.";

        return $@"{reminderClause}In one sentence, describe what you observe about {WithArticle(GetOutcomeLabel(linkedOutcome))}.{keywordHint}
{Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Chained speaking — request 3 of 3.
    /// Continuation prompt — asks the speaker to end with one open question to the companion.
    /// No description or keyword hints to keep it focused on the question.
    /// </summary>
    public string BuildSpeakingQuestionPrompt(
        string companionName,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string personaReminder2Clause = personaReminder2 != null
            ? $" Stay in the character of {personaReminder2}."
            : " Stay in character.";

        return $@"{reminderClause}In one sentence, ask {companionName} an open question about what you just shared.
{Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds a continuation prompt focused on a specific outcome, including its keywords.
    /// </summary>
    public string BuildOutcomeDescriptionSentencePrompt(ConcreteOutcome outcome, string questionText, string? personaReminder = null, string? personaReminder2 = null)
    {
        var outcomeKics = outcome is NarrationNode childNode ? childNode.NodeKeywordsInContext
            : outcome is ObservationObject obs ? obs.ObservationKeywordsInContext
            : outcome.OutcomeKeywordsInContext;
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        string keywordHint = outcomeKics.Count > 0
            ? $" You may notice things like: {string.Join(", ", outcomeKics.Select(k => k.Context))}."
            : "";
        return $@"You are now looking at {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}{questionText}{keywordHint}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

}
