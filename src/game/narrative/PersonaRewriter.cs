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
/// <remarks>
/// <c>Speaking</c> is self-narration spoken aloud to a companion ("come look at this"); it uses the
/// first-person narration framing. <c>DialogueReplica</c> is a turn in a two-person conversation —
/// framed as direct speech where "I" is the speaker and "you" is the interlocutor being addressed.
/// </remarks>
public enum NarrationKind { Observation, Reasoning, Action, Outcome, Speaking, DialogueReplica }

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
    /// <param name="previousReplica">
    /// For <see cref="NarrationKind.DialogueReplica"/> only: the addressee's most recent spoken line
    /// in this conversation, so the rewrite responds in the flow of the exchange instead of in a
    /// vacuum. Pass null/empty for the opening line of a conversation — the prompt then says so
    /// explicitly rather than silently omitting any mention of prior context.
    /// </param>
    /// <param name="innerThought">
    /// The persona's own free-text reasoning behind the choice this sentence narrates (see
    /// <see cref="PersonaChoice{T}.Reasoning"/>), quoted in the prompt as the inner thought behind
    /// the neutral line — a flavour hint the rewrite may echo, never new facts to state. Ignored for
    /// <see cref="NarrationKind.DialogueReplica"/>.
    /// </param>
    /// <param name="speakerName">
    /// For <see cref="NarrationKind.DialogueReplica"/> only: the speaker's (placeholder) name, so the
    /// prompt can name the speaker ("You are Bob, the speaker"). Pass the placeholder, not the
    /// real name — the caller restores real names afterwards, like everywhere else in dialogue.
    /// </param>
    public async Task<string> RewriteAsync(
        int slotId,
        string neutralText,
        NarrationKind kind,
        string? personaReminder2 = null,
        string? addressee = null,
        bool keepHistory = false,
        string? forcedPrefix = null,
        string? styleInstruction = null,
        string? dialogueContext = null,
        string? previousReplica = null,
        string? innerThought = null,
        string? speakerName = null,
        Preview.ILlmPreviewSink? preview = null,
        CancellationToken ct = default)
    {
        // Single name boundary: everything that goes INTO the prompt is switched to the scene's simple,
        // sanitizer-safe false names; everything RETURNED is switched back to the real in-world names.
        // NameFaking is null-safe and idempotent (already-false text round-trips), so this is safe even
        // when upstream already faked (NpcLabelResolver) or when no scene table is active.
        neutralText     = NameFaking.Fake(neutralText);
        addressee       = addressee       == null ? null : NameFaking.Fake(addressee);
        dialogueContext = dialogueContext == null ? null : NameFaking.Fake(dialogueContext);
        previousReplica = previousReplica == null ? null : NameFaking.Fake(previousReplica);
        speakerName     = speakerName     == null ? null : NameFaking.Fake(speakerName);
        innerThought    = innerThought    == null ? null : NameFaking.Fake(innerThought);

        if (PlaygroundMode.IsActive)
        {
            // No LLM call in playground: hand the neutral placeholder straight to the preview (real
            // names restored) and return it.
            string playgroundText = NameFaking.Real(neutralText);
            preview?.OnComplete(playgroundText);
            return playgroundText;
        }

        string prompt = kind == NarrationKind.DialogueReplica
            ? BuildDialoguePrompt(neutralText, addressee, dialogueContext, previousReplica, speakerName,
                                  FooterFor(kind, personaReminder2, styleInstruction, TextHint, addressee))
            : BuildPrompt(neutralText, InstructionFor(kind, addressee),
                          FooterFor(kind, personaReminder2, styleInstruction, TextHint, addressee),
                          innerThought);
        // Dialogue replies may carry a parenthetical aside (an inner thought the interlocutor does not
        // hear), so the body charset is widened to include round brackets for that kind only.
        string gbnf = JsonConstraintGenerator.GenerateGBNF(
            LLMSchemaConfig.CreateRewriteSchema(forcedPrefix: forcedPrefix,
                                                allowParentheses: kind == NarrationKind.DialogueReplica));

        // When a preview sink is supplied, stream the tokens through it; otherwise keep the one-shot
        // path so the Critic / non-preview callers are byte-for-byte unchanged.
        string json = preview != null
            ? await _llm.GenerateConstrainedStringStreamingAsync(
                  slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory,
                  onTokenStreamed: (token, _) => preview.OnToken(token))
            : await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory);

        string text = ParseField(json, "text");
        if (string.IsNullOrWhiteSpace(text))
        {
            string fallback = NameFaking.Real(neutralText);
            preview?.OnComplete(fallback);
            return fallback;
        }
        string sanitized = await TextSanitizationPipeline.SanitizeAsync(TextTruncationUtils.TrimToLastSentence(text));
        string restored  = NameFaking.Real(sanitized);
        preview?.OnComplete(restored);
        return restored;
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

    private static string BuildPrompt(string neutralText, string instruction, string footer, string? innerThought = null)
    {
        // The persona's own words from the choice this sentence narrates, offered back as the
        // thought behind the line — flavour to echo, not content to add. The precedence clause is
        // load-bearing: a reluctant choice's thought can argue AGAINST the action, and without it
        // the model follows the mood and swaps the deed for what the thought would rather do.
        string thought = string.IsNullOrWhiteSpace(innerThought)
            ? ""
            : $"\nA passing thought accompanies it: \"{innerThought.Trim().Trim('"')}\" — use it to colour the mood and wording only. It changes nothing about what is done: the sentence above states what actually happens, and wherever the thought pulls elsewhere, the sentence wins.\n";

        return $@"Re-express the following sentence in your own voice: ""{neutralText}""
{thought}
This sentence is written in the first person, and that ""I"" is you — it describes your own perception, thought or action, not anyone else's.

{instruction}
{footer}";
    }

    /// <summary>
    /// Prompt for a single line of dialogue in a two-person conversation. Frames the neutral line as
    /// direct speech: "I" is the speaker (this persona), "you" is <paramref name="addressee"/>. The
    /// neutral line is a plain, short reply; the persona keeps its meaning and adds flavour.
    /// <paramref name="previousReplica"/> — what <paramref name="addressee"/> just said — is included
    /// so the rewrite lands as a reply to something rather than a line spoken into a void; when null
    /// the prompt says outright that this opens the conversation.
    /// <paramref name="speakerName"/> names the "I" of the line (the speaker's placeholder name, e.g.
    /// Bob/Alice for the party member) so the model knows who it is speaking as, not just to.
    /// </summary>
    private static string BuildDialoguePrompt(string neutralText, string? addressee,
                                              string? dialogueContext, string? previousReplica,
                                              string? speakerName, string footer)
    {
        string who     = string.IsNullOrWhiteSpace(addressee) ? "the person you are speaking with" : addressee!;
        // Role statement, not a pronoun equation: phrasing like «"I" is you, Bob» reads to a small
        // model as a substitution rule (I → you) and gets applied to the text, turning "I'm Bob"
        // into "you're Bob". State who the speaker is, then say which pronoun to use for whom.
        string speaker = string.IsNullOrWhiteSpace(speakerName) ? "You are the speaker" : $"You are {speakerName.Trim()}, the speaker";
        string context = string.IsNullOrWhiteSpace(dialogueContext)
            ? ""
            : $" The conversation is about {dialogueContext.Trim().TrimEnd('.')}.";
        string history = string.IsNullOrWhiteSpace(previousReplica)
            ? " This is the opening line of the conversation — no one has spoken yet."
            : $" {who} just said: \"{previousReplica.Trim().Trim('"')}\"";

        return $@"You are in conversation with {who}.{context}{history}

Re-express the following spoken line in your own voice, keeping the same meaning and intent: ""{neutralText}""

This is a line of direct dialogue that you say out loud. {speaker}; {who} is the person you are talking to. Speak in the first person: call yourself ""I"", and call {who} ""you"". Keep it a short, natural spoken reply — add your own flavour, wording and personality, but do not change what is being said, asked or offered.

You may enclose an aside in parentheses (like this) to voice a private inner thought — something you think but do not say aloud, which {who} does not hear. Everything outside the parentheses is spoken to {who}; keep any parenthetical aside brief and optional.

{footer}";
    }

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

    private static string FooterFor(NarrationKind kind, string? personaReminder2, string? styleInstruction, string? jsonHint, string? addressee) =>
        kind switch
        {
            // A dialogue turn: 2nd-person reminder that names the interlocutor being addressed.
            NarrationKind.DialogueReplica =>
                Config.Narrative.DialogueAnswerInstructionFor(personaReminder2, addressee, jsonHint, styleInstruction),
            // Speaking carries its own 2nd-person dialogue reminder (single sentence per line).
            NarrationKind.Speaking =>
                Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2, jsonHint, styleInstruction),
            // Observation (merged attention + detail), Reasoning (inner thought), and Outcome
            // (success/failure of a tried action) all omit the length clause entirely to give the
            // persona freedom over how far it unfolds.
            NarrationKind.Observation or NarrationKind.Reasoning or NarrationKind.Outcome =>
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
