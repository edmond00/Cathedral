using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Game.Narrative.Sanitizer;

namespace Cathedral.Game.Narrative;

/// <summary>The kind of text being rewritten — selects the persona styling instruction.</summary>
public enum NarrationKind { Observation, Reasoning, Action, Outcome, Speaking }

/// <summary>
/// Turns neutral meaning text (from <see cref="NeutralNarration"/>) into persona-styled prose by
/// asking the speaker's LLM slot to re-express it. The slot already carries the persona as its
/// system prompt, so the rewrite prompt only supplies the neutral line + a kind-specific styling
/// instruction. In playground mode the neutral text is returned unchanged (no LLM call).
///
/// Reuses <see cref="LlamaServerManager.GenerateConstrainedStringAsync"/> (one-shot constrained
/// request that also manages slot history) rather than re-hand-rolling the streaming/event pattern.
/// </summary>
public class PersonaRewriter
{
    private readonly LlamaServerManager _llm;

    public PersonaRewriter(LlamaServerManager llm) => _llm = llm ?? throw new ArgumentNullException(nameof(llm));

    private const int RewriteMaxTokens = 280;

    /// <summary>
    /// Rewrites <paramref name="neutralText"/> in the persona of <paramref name="slotId"/>.
    /// <paramref name="keepHistory"/> preserves the slot's conversation for stylistic continuity
    /// across a multi-sentence batch (the caller resets the slot at the batch boundary).
    /// </summary>
    public async Task<string> RewriteAsync(
        int slotId,
        string neutralText,
        NarrationKind kind,
        string? personaReminder2 = null,
        string? addressee = null,
        bool keepHistory = false,
        CancellationToken ct = default)
    {
        if (PlaygroundMode.IsActive) return neutralText;

        string prompt = BuildPrompt(neutralText, InstructionFor(kind, addressee), FooterFor(kind, personaReminder2));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateRewriteSchema());
        string json = await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory);

        string text = ParseField(json, "text");
        if (string.IsNullOrWhiteSpace(text)) return neutralText;
        return await TextSanitizationPipeline.SanitizeAsync(TextTruncationUtils.TrimToLastSentence(text));
    }

    /// <summary>
    /// Observation rewrite that also returns the single noun the persona chose as the clickable
    /// keyword. The keyword is validated to appear in the styled text; otherwise it falls back to
    /// the last meaningful word of the styled text so highlighting always has an anchor.
    /// </summary>
    public async Task<(string Text, string? Keyword)> RewriteObservationAsync(
        int slotId,
        string neutralText,
        string? personaReminder2,
        bool keepHistory,
        CancellationToken ct = default)
    {
        if (PlaygroundMode.IsActive)
            return (neutralText, NeutralNarration.KeywordFromPhrase(neutralText));

        string instruction = InstructionFor(NarrationKind.Observation, null) +
            " Then choose the single most evocative noun from your sentence as the keyword.";
        string prompt = BuildPrompt(neutralText, instruction, FooterFor(NarrationKind.Observation, personaReminder2));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateObservationRewriteSchema());
        string json = await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory);

        string text = ParseField(json, "text");
        if (string.IsNullOrWhiteSpace(text)) return (neutralText, NeutralNarration.KeywordFromPhrase(neutralText));
        text = await TextSanitizationPipeline.SanitizeAsync(TextTruncationUtils.TrimToLastSentence(text));

        string? keyword = ParseField(json, "keyword");
        keyword = keyword?.ToLowerInvariant().Trim('.', ',', '!', '?', '"', '\'', '(', ')', ' ');
        if (string.IsNullOrWhiteSpace(keyword) || !ContainsWord(text, keyword))
            keyword = NeutralNarration.KeywordFromPhrase(text);
        return (text, keyword);
    }

    // ── Prompt construction ────────────────────────────────────────────────────

    private static string BuildPrompt(string neutralText, string instruction, string footer) => $@"Re-express this in your own voice.

Neutral meaning: ""{neutralText}""

{instruction}
{footer}";

    private static string InstructionFor(NarrationKind kind, string? addressee) => kind switch
    {
        NarrationKind.Observation =>
            "Re-express this perception in your own voice — use a concrete image, metaphor or vivid sensory detail that fits who you are, while keeping the same meaning.",
        NarrationKind.Reasoning =>
            "Re-express this as your own inner thought — your intent, what draws you, and how you mean to proceed — while keeping the same meaning.",
        NarrationKind.Action =>
            "Re-express this intended action in your own voice, concretely and naturally, keeping the same action and its target.",
        NarrationKind.Outcome =>
            "Re-express this result in your own voice — what happens and how it feels to you — while keeping the same meaning and whether it succeeded or failed.",
        NarrationKind.Speaking =>
            $"Say this to {addressee ?? "your companion"} as direct speech in your own voice, keeping the same meaning.",
        _ => "Re-express this in your own voice, keeping the same meaning.",
    };

    private static string FooterFor(NarrationKind kind, string? personaReminder2) =>
        kind == NarrationKind.Speaking
            ? Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2)
            : Config.Narrative.AnswerInstructionFor(personaReminder2);

    // ── Parsing helpers ────────────────────────────────────────────────────────

    private static string ParseField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(field, out var p) ? (p.GetString() ?? string.Empty) : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static bool ContainsWord(string text, string word)
        => Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
}
