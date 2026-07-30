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

    // Lower bound (in characters) of the free-text body the GBNF grammar generates after the prefix.
    private const int RewriteMinChars = 15;

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
                                  FooterFor(kind, personaReminder2, styleInstruction))
            : BuildPrompt(neutralText, InstructionFor(kind, addressee),
                          FooterFor(kind, personaReminder2, styleInstruction),
                          innerThought);
        // Every first-person narration kind opens with "I " so a small model cannot drift into a
        // detached, non-first-person opening (e.g. "Data flows through my eyes..."). An explicit
        // forcedPrefix (the action's "I will ", say) still wins; spoken kinds (Speaking,
        // DialogueReplica) are exempt, since a reply or a call to a companion needn't begin with "I".
        forcedPrefix ??= DefaultForcedPrefix(kind);

        // The rewrite is emitted as raw text (no JSON envelope), so a nested quotation no longer
        // terminates generation mid-sentence — double-quotes are allowed in the body charset. A dialogue
        // reply is structured instead: a double-quoted spoken line plus an optional parenthetical aside
        // (the unspoken inner thought), which keeps narration out of the spoken words and the aside out
        // of the quotes — see GenerateDialogueReplyGrammar.
        string gbnf = kind == NarrationKind.DialogueReplica
            ? JsonConstraintGenerator.GenerateDialogueReplyGrammar(
                  spokenMinLen: RewriteMinChars,
                  spokenMaxLen: Config.Narrative.MaxNarrativeTextLength,
                  asideMaxLen: Config.Narrative.DialogueAsideMaxLength)
            : JsonConstraintGenerator.GenerateRawTextGrammar(
                  forcedPrefix: forcedPrefix,
                  minLen: RewriteMinChars,
                  maxLen: Config.Narrative.MaxNarrativeTextLength,
                  allowDoubleQuote: true);

        // When a preview sink is supplied, stream the tokens through it; otherwise keep the one-shot
        // path so the Critic / non-preview callers are byte-for-byte unchanged. The grammar produces the
        // rewritten sentence directly, so the returned string is the text itself — no field to parse.
        // A dialogue reply arrives behind the "I say : " frame, which is scaffolding for the model and
        // must not be seen: the token stream is filtered so the preview box never shows it either.
        Action<string, int>? onToken = preview == null ? null : MakeTokenForwarder(preview, kind);
        string text = preview != null
            ? await _llm.GenerateConstrainedStringStreamingAsync(
                  slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory,
                  onTokenStreamed: onToken)
            : await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory);

        if (kind == NarrationKind.DialogueReplica)
            text = StripReplyFrame(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            string fallback = NameFaking.Real(neutralText);
            preview?.OnComplete(fallback);
            return fallback;
        }
        // A dialogue reply is a complete structured string ("spoken" (aside)?) — appending "..." when it
        // does not end in sentence punctuation (it ends in " or ")") would corrupt that shape, so the
        // truncation guard is skipped for it. Every other kind keeps the mid-sentence "…" cleanup.
        string trimmed = kind == NarrationKind.DialogueReplica ? text : TextTruncationUtils.TrimToLastSentence(text);
        string sanitized = await TextSanitizationPipeline.SanitizeAsync(trimmed);
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

    // ── The dialogue reply frame ───────────────────────────────────────────────

    /// <summary>
    /// Removes the generated reply's <see cref="JsonConstraintGenerator.DialogueReplyFrame"/> opening,
    /// leaving the <c>"spoken" (aside)?</c> shape the dialogue layer parses. Tolerant of a missing frame
    /// (a truncated generation, or a caller that changed the grammar) so a reply is never lost to it.
    /// </summary>
    private static string StripReplyFrame(string text)
    {
        string s = (text ?? string.Empty).TrimStart();
        return s.StartsWith(JsonConstraintGenerator.DialogueReplyFrame, StringComparison.OrdinalIgnoreCase)
            ? s.Substring(JsonConstraintGenerator.DialogueReplyFrame.Length).TrimStart()
            : s;
    }

    /// <summary>
    /// The per-token preview callback. For every kind but a dialogue reply it forwards tokens straight
    /// through; for a dialogue reply it withholds the leading frame, then forwards everything after it,
    /// so the player watches the spoken line appear rather than <c>I say : "…</c>.
    /// </summary>
    private static Action<string, int> MakeTokenForwarder(Preview.ILlmPreviewSink preview, NarrationKind kind)
    {
        if (kind != NarrationKind.DialogueReplica)
            return (token, _) => preview.OnToken(token);

        string frame = JsonConstraintGenerator.DialogueReplyFrame;
        var head = new System.Text.StringBuilder();
        bool framePassed = false;

        return (token, _) =>
        {
            if (framePassed)
            {
                preview.OnToken(token);
                return;
            }

            head.Append(token);
            if (head.Length < frame.Length) return;   // still inside the frame — nothing to show yet

            framePassed = true;
            string seen = head.ToString();
            // Defensive: if the frame is absent (grammar changed, or a stray leading space), show what
            // arrived rather than silently eating the first characters of the line.
            string rest = seen.StartsWith(frame, StringComparison.OrdinalIgnoreCase)
                ? seen.Substring(frame.Length)
                : seen;
            if (rest.Length > 0) preview.OnToken(rest);
        };
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
    /// <para>
    /// Two things about the layout are deliberate. <b>The line to speak comes last</b>, after every
    /// rule: it is the one piece of the prompt the answer must actually carry, and on a 3B model the
    /// closing lines outweigh the opening ones — stated first, it sat some 300 tokens from the answer
    /// and came back distorted. <b>Every rule is stated once.</b> The shape, the pronouns and the
    /// no-narration rule each used to appear both here and in the footer, in different words; a small
    /// model reads a restatement as a second, subtly different requirement, and the prompt was over
    /// twice this length for no added instruction.
    /// </para>
    /// <para>
    /// Only the spoken part is shown as a <c>&lt;slot&gt;</c>. The optional aside was drawn the same way
    /// once — <c>(&lt;one brief thought … — optional&gt;)</c> — and the model answered a slot it had no
    /// thought for by naming it: the reply came back as <c>… fireside?" (thought)</c>, and that literal
    /// word then travelled into the next turn's history as what the NPC had said. Described in prose,
    /// there is no slot to fill.
    /// </para>
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
            : $" You speak of {dialogueContext.Trim().TrimEnd('.')}.";
        string history = string.IsNullOrWhiteSpace(previousReplica)
            ? " No one has spoken yet: this opens the conversation."
            : $" {who} just said: \"{previousReplica.Trim().Trim('"')}\"";
        string frame = JsonConstraintGenerator.DialogueReplyFrame.Trim();

        return $@"{speaker}, talking with {who}.{context}{history}

Answer in this shape, and hold nothing else:
{JsonConstraintGenerator.DialogueReplyFrame}""<the words {who} hears you say>""

After the closing quote you may add one brief thought {who} does not hear, wrapped in parentheses and beginning with ""I"" — or leave it out entirely.

That opening {frame} is the whole report of your speaking, so the quotes hold sound and nothing else: no speech verb, no quotation marks, nothing of your voice, tone or manner — that belongs in the thought, or nowhere. Call yourself ""I"" and {who} ""you""; never call {who} by your own name.

{footer}

Now say this line as your own — same meaning, same intent, nothing added to or taken from what is said, asked or offered — but put it in your own words, do not give it back word for word:
""{neutralText}""";
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

    /// <summary>
    /// The default GBNF opening prefix for a rewrite kind when the caller doesn't force one. Every
    /// first-person narration kind (perception, thought, action, outcome) opens with "I " so a small
    /// model cannot start on a detached, non-first-person phrase. Spoken kinds return null: a spoken
    /// reply or a line addressed to a companion needn't begin with "I".
    /// </summary>
    private static string? DefaultForcedPrefix(NarrationKind kind) => kind switch
    {
        NarrationKind.Observation or NarrationKind.Reasoning
            or NarrationKind.Action or NarrationKind.Outcome => "I ",
        _ => null,
    };

    private static string FooterFor(NarrationKind kind, string? personaReminder2, string? styleInstruction) =>
        kind switch
        {
            // A dialogue turn. The shape, the pronouns and the interlocutor's name are stated by
            // BuildDialoguePrompt, which owns the reply shape; the footer carries only the clauses every
            // kind shares (length, grounding, style, character, setting).
            NarrationKind.DialogueReplica =>
                Config.Narrative.DialogueAnswerInstructionFor(personaReminder2, styleInstruction),
            // Speaking carries its own 2nd-person dialogue reminder (single sentence per line).
            NarrationKind.Speaking =>
                Config.Narrative.SpeakingAnswerInstructionFor(personaReminder2, styleInstruction),
            // Observation (merged attention + detail), Reasoning (inner thought), and Outcome
            // (success/failure of a tried action) all omit the length clause entirely to give the
            // persona freedom over how far it unfolds.
            NarrationKind.Observation or NarrationKind.Reasoning or NarrationKind.Outcome =>
                Config.Narrative.AnswerInstructionFor(personaReminder2, styleInstruction, includeLengthClause: false),
            _ =>
                Config.Narrative.AnswerInstructionFor(personaReminder2, styleInstruction),
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
