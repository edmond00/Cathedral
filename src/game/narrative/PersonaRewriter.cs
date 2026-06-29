using System;
using System.Collections.Generic;
using System.Text.Json;
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

    // JSON field-layout hint shown in the "Respond in JSON format (...)" instruction.
    private const string TextHint = "{\"text\": \"...\"}";

    /// <summary>
    /// Rewrites <paramref name="neutralText"/> in the persona of <paramref name="slotId"/>.
    /// <paramref name="keepHistory"/> preserves the slot's conversation for stylistic continuity
    /// across a multi-sentence batch (the caller resets the slot at the batch boundary).
    /// </summary>
    /// <param name="forcedPrefix">When set (e.g. <c>"I "</c>), the rewritten text is constrained by
    /// GBNF to start with this literal — used to force a first-person opening on the observation opener.</param>
    public async Task<string> RewriteAsync(
        int slotId,
        string neutralText,
        NarrationKind kind,
        string? personaReminder2 = null,
        string? addressee = null,
        bool keepHistory = false,
        string? forcedPrefix = null,
        string? styleInstruction = null,
        CancellationToken ct = default)
    {
        if (PlaygroundMode.IsActive) return neutralText;

        string prompt = BuildPrompt(neutralText, InstructionFor(kind, addressee), FooterFor(kind, personaReminder2, styleInstruction, TextHint));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateRewriteSchema(forcedPrefix: forcedPrefix));
        string json = await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory);

        string text = ParseField(json, "text");
        if (string.IsNullOrWhiteSpace(text)) return neutralText;
        return await TextSanitizationPipeline.SanitizeAsync(TextTruncationUtils.TrimToLastSentence(text));
    }

    /// <summary>
    /// Asks the persona slot to pick one of <paramref name="options"/> (constrained choice).
    /// Returns the chosen option string, or empty on failure. Caller handles the playground case
    /// (this returns empty there, since the LLM is not consulted).
    /// </summary>
    public async Task<string> ChooseAsync(
        int slotId,
        string prompt,
        List<string> options,
        string fieldName = "choice",
        bool keepHistory = false,
        CancellationToken ct = default)
    {
        if (PlaygroundMode.IsActive || options.Count == 0) return string.Empty;
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateChoiceSchema(fieldName, options));
        string json = await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, maxTokens: 64, skipReset: keepHistory);
        return ParseField(json, fieldName);
    }

    // ── Prompt construction ────────────────────────────────────────────────────

    private static string BuildPrompt(string neutralText, string instruction, string footer) => $@"Re-express the following sentence in your own voice: ""{neutralText}""

{instruction}
{footer}";

    private static string InstructionFor(NarrationKind kind, string? addressee) => kind switch
    {
        NarrationKind.Observation =>
            "Re-express this perception in your own voice, keeping the same meaning.",
        NarrationKind.Reasoning =>
            "Re-express this as your own inner thought — your intent, what draws you, and how you mean to proceed — while keeping the same meaning.",
        NarrationKind.Action =>
            "Re-express this intended action in your own voice, concretely and naturally. The action you intend and its target are literal facts that must be preserved exactly: state plainly what you will do, never drop, blur, or replace the action itself — restyle only how it is told, not what is done.",
        NarrationKind.Outcome =>
            "Re-express this result in your own voice — what happens and how it feels to you — while keeping the same meaning and whether it succeeded or failed.",
        NarrationKind.Speaking =>
            $"Say this to {addressee ?? "your companion"} as direct speech in your own voice, keeping the same meaning.",
        _ => "Re-express this in your own voice, keeping the same meaning.",
    };

    private static string FooterFor(NarrationKind kind, string? personaReminder2, string? styleInstruction, string? jsonHint) =>
        kind switch
        {
            // Speaking carries its own 2nd-person dialogue reminder (single sentence per line).
            NarrationKind.Speaking =>
                Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2, jsonHint, styleInstruction),
            // Observation (merged attention + detail) and Reasoning (inner thought) both omit the
            // length clause entirely to give the persona freedom over how far it unfolds.
            NarrationKind.Observation or NarrationKind.Reasoning =>
                Config.Narrative.AnswerInstructionFor(personaReminder2, jsonHint, styleInstruction, includeLengthClause: false),
            _ =>
                Config.Narrative.AnswerInstructionFor(personaReminder2, jsonHint, styleInstruction),
        };

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
}
