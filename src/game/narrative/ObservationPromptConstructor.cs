using Cathedral;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constructs prompts for observation modiMentis to generate environment perceptions.
/// Builds the user message that gets sent to the LLM with the cached persona prompt.
/// Keywords are extracted dynamically from generated text — no keyword hints in prompts.
/// </summary>
public class ObservationPromptConstructor
{
    private static string WithArticle(string s)
    {
        if (s.Length == 0) return s;
        var lower = s.ToLowerInvariant();
        if (lower.StartsWith("a ") || lower.StartsWith("an ") || lower.StartsWith("the "))
            return lower; // already has article — just lowercase for mid-sentence use
        return "aeiouAEIOU".Contains(lower[0]) ? $"an {lower}" : $"a {lower}";
    }

    /// Returns a human-readable label for an outcome: uses GenerateNeutralDescription for
    /// NarrationNode and ObservationObject outcomes instead of the raw id from DisplayName.
    private static string GetOutcomeLabel(ConcreteOutcome outcome) =>
        outcome is NarrationNode n ? n.GenerateNeutralDescription(0) :
        outcome is ObservationObject obs ? obs.GenerateNeutralDescription(0) :
        outcome.DisplayName;

    /// <summary>
    /// Builds the prompt for the FIRST sentence in a per-sentence observation batch.
    /// Provides the full scene context and a description of what the character is focusing on.
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
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}
Your attention is drawn to {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds the prompt for a general scene description — the opening sentence of an overall observation.
    /// </summary>
    public string BuildGeneralDescriptionPrompt(NarrationNode node, int locationId, string personaTone, WorldContext worldContext, string questionText, string? personaReminder = null, string? personaReminder2 = null)
    {
        var locationContext = node.BuildLocationContext(worldContext, locationId);
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}
{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds a continuation prompt that writes a short transition sentence linking
    /// the previous description to a specific outcome.
    /// </summary>
    public string BuildTransitionSentencePrompt(ConcreteOutcome outcome, string previousDescription, string questionText, string? personaReminder = null, string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        return $@"You were observing {WithArticle(previousDescription)} but now you notice {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Chained speaking — request 1 of 3.
    /// Establishes full scene context and asks the speaker to call the companion's attention
    /// to what they are observing.
    /// </summary>
    public string BuildSpeakingAttentionPrompt(
        NarrationNode node,
        int locationId,
        ConcreteOutcome linkedOutcome,
        string keyword,
        string companionName,
        string? personaTone,
        WorldContext worldContext,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        var locationContext = node.BuildLocationContext(worldContext, locationId);
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";

        return $@"You are a {personaTone}.
{WorldContext.EpochContext}
{locationContext}
You are thinking about {keyword}. You want to share this with your companion {companionName}.
{reminderClause}In one sentence, call {companionName}'s attention to {WithArticle(GetOutcomeLabel(linkedOutcome))} you are observing.
{Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Chained speaking — request 2 of 3.
    /// Asks the speaker to describe the observation.
    /// Keywords from this sentence are extracted and made clickable.
    /// </summary>
    public string BuildSpeakingDescriptionPrompt(
        ConcreteOutcome linkedOutcome,
        string companionName,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        return $@"{reminderClause}In one sentence, describe what you observe about {WithArticle(GetOutcomeLabel(linkedOutcome))}.
{Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Chained speaking — request 3 of 3.
    /// Asks the speaker to end with one open question to the companion.
    /// </summary>
    public string BuildSpeakingQuestionPrompt(
        string companionName,
        string? personaReminder = null,
        string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        return $@"{reminderClause}In one sentence, ask {companionName} an open question about what you just shared.
{Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)}";
    }

    /// <summary>
    /// Builds a continuation prompt focused on a specific outcome.
    /// </summary>
    public string BuildOutcomeDescriptionSentencePrompt(ConcreteOutcome outcome, string questionText, string? personaReminder = null, string? personaReminder2 = null)
    {
        string reminderClause = personaReminder != null ? $"As a {personaReminder}, " : "";
        return $@"You are now looking at {WithArticle(GetOutcomeLabel(outcome))}.
{reminderClause}{questionText}
{Config.Narrative.AnswerInstructionFor(personaReminder2)}";
    }

}
