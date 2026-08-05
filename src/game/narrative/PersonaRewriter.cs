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
/// <c>Speaking</c> (an address to a companion during narration — "come look at this") and
/// <c>DialogueReplica</c> (a turn in a two-person conversation) are both <i>spoken</i> kinds: one
/// person says words to another, so both are generated the same way, behind the
/// <see cref="JsonConstraintGenerator.DialogueReplyFrame"/> and inside quotes. What differs is only
/// the situation line at the top of the prompt and what the caller gets back — a dialogue turn keeps
/// the quoted shape its parser expects, a spoken address is unwrapped to the bare words.
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

        string footer = FooterFor(kind, personaReminder2, styleInstruction);
        string prompt = kind switch
        {
            NarrationKind.DialogueReplica =>
                BuildDialoguePrompt(neutralText, addressee, dialogueContext, speakerName, footer),
            NarrationKind.Speaking =>
                BuildSpeakingPrompt(neutralText, addressee, footer),
            _ =>
                BuildPrompt(neutralText, InstructionFor(kind), footer, innerThought),
        };
        // Every first-person narration kind opens with "I " so a small model cannot drift into a
        // detached, non-first-person opening (e.g. "Data flows through my eyes..."). An explicit
        // forcedPrefix (the action's "I will ", say) still wins; spoken kinds (Speaking,
        // DialogueReplica) are exempt, since a reply or a call to a companion needn't begin with "I".
        forcedPrefix ??= DefaultForcedPrefix(kind);

        // The rewrite is emitted as raw text (no JSON envelope) whose charset excludes the double-quote:
        // the prompt shows the neutral line and the inner thought inside quotes, and a small model copies
        // that framing back into its answer ("...with warmth, \"I focus on this lane\""). Quoting itself
        // reads badly in narration and then breaks the sanitizer downstream, whose JSON envelope ends its
        // string at the first quote and truncates the passage. A spoken kind is structured instead: a
        // frame plus a double-quoted spoken line and nothing after it, so there the quotes are the shape
        // and are produced by the grammar itself — see GenerateDialogueReplyGrammar.
        string gbnf = IsSpokenKind(kind)
            ? JsonConstraintGenerator.GenerateDialogueReplyGrammar(
                  addressee: addressee,
                  spokenMinLen: RewriteMinChars,
                  spokenMaxLen: Config.Narrative.MaxNarrativeTextLength)
            : JsonConstraintGenerator.GenerateRawTextGrammar(
                  forcedPrefix: forcedPrefix,
                  minLen: RewriteMinChars,
                  maxLen: Config.Narrative.MaxNarrativeTextLength);

        // Everything game-authored that went INTO the prompt: the answer may echo any of it verbatim, and
        // when it does, the sanitizer must not treat it as the model's invention. innerThought is
        // deliberately absent — it is the persona's own words, so a name hallucinated there would
        // otherwise license itself here. See SourceVocabulary.
        var sourceVocabulary = SourceVocabulary.From(neutralText, addressee, dialogueContext, speakerName);
        preview?.OnSourceVocabulary(sourceVocabulary);

        // When a preview sink is supplied, stream the tokens through it; otherwise keep the one-shot
        // path so the Critic / non-preview callers are byte-for-byte unchanged. The grammar produces the
        // rewritten sentence directly, so the returned string is the text itself — no field to parse.
        // A spoken kind arrives behind the "I say to X : " frame, which is scaffolding for the model and
        // must not be seen: the token stream is filtered so the preview box never shows it either.
        Action<string, int>? onToken = preview == null ? null : MakeTokenForwarder(preview, kind, addressee);
        string text = preview != null
            ? await _llm.GenerateConstrainedStringStreamingAsync(
                  slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory,
                  onTokenStreamed: onToken)
            : await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, RewriteMaxTokens, skipReset: keepHistory);

        if (IsSpokenKind(kind))
            text = StripReplyFrame(text, addressee);

        if (string.IsNullOrWhiteSpace(text))
        {
            string fallback = NameFaking.Real(neutralText);
            preview?.OnComplete(fallback);
            return fallback;
        }
        // A spoken kind is a complete structured string ("spoken") — appending "..." when it does not
        // end in sentence punctuation (it ends in the closing quote) would corrupt that shape, so the
        // truncation guard is skipped for it. Every other kind keeps the mid-sentence "…" cleanup.
        string trimmed = IsSpokenKind(kind) ? text : TextTruncationUtils.TrimToLastSentence(text);
        string sanitized = IsSpokenKind(kind)
            ? await SanitizeSpokenAsync(trimmed, sourceVocabulary)
            : await TextSanitizationPipeline.SanitizeAsync(trimmed, sourceVocabulary);
        string restored  = NameFaking.Real(sanitized);

        // A dialogue turn keeps the quoted shape — DialogueReplicaWriter.NormalizeReply is the one place
        // that unwraps it, on both sides of a conversation. A spoken address has no such parser waiting,
        // so the words between the quotes are extracted here and the caller gets the line itself.
        if (kind == NarrationKind.Speaking)
            restored = UnwrapSpoken(restored);

        preview?.OnComplete(restored);
        return restored;
    }

    /// <summary>
    /// The kinds that are words one character says to another: generated behind the
    /// <see cref="JsonConstraintGenerator.DialogueReplyFrame"/> and inside quotes, rather than as a run
    /// of first-person narration.
    /// </summary>
    private static bool IsSpokenKind(NarrationKind kind)
        => kind is NarrationKind.Speaking or NarrationKind.DialogueReplica;

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
    /// Removes the <see cref="JsonConstraintGenerator.DialogueReplyFrame"/> opening, leaving the
    /// <c>"spoken"</c> shape the dialogue layer parses. Tolerant of a missing frame (a truncated
    /// generation, or a caller that changed the grammar) so a reply is never lost to it.
    /// </summary>
    private static string StripReplyFrame(string text, string? addressee)
    {
        string s     = (text ?? string.Empty).TrimStart();
        string frame = JsonConstraintGenerator.DialogueReplyFrame(addressee);
        return s.StartsWith(frame, StringComparison.OrdinalIgnoreCase)
            ? s.Substring(frame.Length).TrimStart()
            : s;
    }

    /// <summary>
    /// Extracts the spoken words from the <c>"spoken"</c> shape the grammar produces (frame already
    /// stripped), for the callers that want the line rather than the shape. Mirrors
    /// <c>DialogueReplicaWriter.NormalizeReply</c> — same rules, so both spoken kinds are unwrapped
    /// identically: first quote to next quote, and a graceful fall back to the trimmed text when the
    /// quotes are not there (playground placeholder text, or a generation cut off by the token limit).
    /// </summary>
    private static string UnwrapSpoken(string raw)
    {
        string s = (raw ?? string.Empty).Trim();
        if (s.Length < 2 || s[0] != '"') return s.Trim('"');

        int close = s.IndexOf('"', 1);
        return close < 0 ? s.Trim('"') : s.Substring(1, close - 1).Trim();
    }

    /// <summary>
    /// Sanitizes a dialogue reply without handing the sanitizer the reply's own delimiters.
    /// <para>
    /// A reply arrives as <c>"spoken words"</c> — the quotes are the shape the dialogue layer parses
    /// (<c>DialogueReplicaWriter.NormalizeReply</c>), not part of what was said. Passing them through
    /// would break the sanitizer's rewrite, whose JSON envelope ends its string at the first quote: a
    /// reply that tripped a detector came back cut off at its opening delimiter, losing the whole line.
    /// So the quotes are peeled off, only the spoken words are sanitized, and the shape is restored —
    /// exactly as it arrived, closing quote included or not, since a reply cut short by the token limit
    /// must keep degrading the way the parser already expects.
    /// </para>
    /// <para>
    /// The spoken part is delimited the way the parser will delimit it (first quote to next quote), so
    /// what gets sanitized is precisely what will be kept. Anything after the closing quote — which the
    /// grammar cannot produce, but a changed grammar or a stray token could — is carried through
    /// untouched rather than silently dropped here; the parser drops it either way.
    /// </para>
    /// </summary>
    private static async Task<string> SanitizeSpokenAsync(string reply, IReadOnlySet<string> sourceVocabulary)
    {
        string s = reply.Trim();

        // Not the quoted shape at all (playground text, or a grammar that changed): sanitize as it is.
        if (s.Length < 2 || s[0] != '"')
            return await TextSanitizationPipeline.SanitizeAsync(s, sourceVocabulary);

        int close = s.IndexOf('"', 1);
        string spoken = close < 0 ? s.Substring(1) : s.Substring(1, close - 1);
        string tail   = close < 0 ? ""             : s.Substring(close);   // the closing quote and beyond

        string sanitized = await TextSanitizationPipeline.SanitizeAsync(spoken, sourceVocabulary);
        return "\"" + sanitized + tail;
    }

    /// <summary>
    /// The per-token preview callback. For a narration kind it forwards tokens straight through; for a
    /// spoken kind it withholds the leading frame, then forwards everything after it, so the player
    /// watches the spoken line appear rather than <c>I say to Emily : "…</c>. The frame's length depends
    /// on the addressee's name, so it is measured here rather than assumed.
    /// <para>
    /// A <see cref="NarrationKind.Speaking"/> address also drops the delimiting quotes as they stream:
    /// its caller is handed the bare words (see <see cref="UnwrapSpoken"/>), so leaving them in would
    /// show the player a quoted line that loses its quotes the moment generation finishes and
    /// <c>OnComplete</c> supersedes it. The grammar's body excludes the double-quote, so the only ones
    /// in the stream are the delimiters and dropping every occurrence is exact. A dialogue reply keeps
    /// them: there the quoted shape is what the caller gets and what the box has always shown.
    /// </para>
    /// </summary>
    private static Action<string, int> MakeTokenForwarder(
        Preview.ILlmPreviewSink preview, NarrationKind kind, string? addressee)
    {
        if (!IsSpokenKind(kind))
            return (token, _) => preview.OnToken(token);

        bool dropQuotes = kind == NarrationKind.Speaking;
        Func<string, string> show = dropQuotes ? s => s.Replace("\"", "") : s => s;

        int frameLength = JsonConstraintGenerator.DialogueReplyFrame(addressee).Length;
        var head = new System.Text.StringBuilder();
        bool framePassed = false;

        return (token, _) =>
        {
            if (framePassed)
            {
                string shown = show(token);
                if (shown.Length > 0) preview.OnToken(shown);
                return;
            }

            head.Append(token);
            if (head.Length < frameLength) return;   // still inside the frame — nothing to show yet

            framePassed = true;
            // Defensive: if no frame is there (grammar changed, or a stray leading space), show what
            // arrived rather than silently eating the first characters of the line.
            string rest = show(StripReplyFrame(head.ToString(), addressee));
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
    /// Prompt for a single line of dialogue in a two-person conversation. <paramref name="neutralText"/>
    /// arrives as the plain spoken line ("Good day to you.") and the answer is the same line in this
    /// persona's words. <paramref name="speakerName"/> names who is speaking,
    /// <paramref name="addressee"/> who is spoken to, and <paramref name="dialogueContext"/> what the
    /// conversation is about — that is the whole of the situation the model is given.
    /// <para>
    /// <b>One frame, on both sides.</b> The line is shown behind the same
    /// <see cref="JsonConstraintGenerator.DialogueReplyFrame"/> the grammar will force the answer to
    /// open with, so prompt and answer are the same shape and the only thing that differs is the part
    /// the model was asked to change. That is what makes "keep the opening, rewrite what is in the
    /// quotes" a concrete instruction rather than a description of a mood.
    /// </para>
    /// <para>
    /// <b>Name the operations.</b> A rewrite task lets a small model satisfy every instruction by
    /// handing the line straight back, and prohibitions ("never give it back word for word") did not
    /// stop it. Listing what a rewrite may actually <i>do</i> — reorder, substitute, invert, interject,
    /// add a figure of speech — gives it something to perform instead of something to avoid. The
    /// meaning clause immediately after is what keeps those operations from turning into invention.
    /// </para>
    /// <para>
    /// <b>Short, and the line stated twice.</b> Everything a 3B model does not need is gone: the
    /// previous turn of the conversation, the worked example, the pronoun rules, the length clause, and
    /// the parenthetical aside (which had its own failure — asked for an optional thought it had none
    /// for, the model wrote the literal word <c>(thought)</c>). What remains is one situation line, the
    /// line to say, the task, the output shape, and the line again at the end, where the closing lines
    /// of a prompt weigh most.
    /// </para>
    /// </summary>
    private static string BuildDialoguePrompt(string neutralText, string? addressee,
                                              string? dialogueContext, string? speakerName, string footer)
    {
        string who     = string.IsNullOrWhiteSpace(addressee) ? "the person in front of you" : addressee!.Trim();
        string speaker = string.IsNullOrWhiteSpace(speakerName) ? "the speaker" : speakerName!.Trim();
        string context = string.IsNullOrWhiteSpace(dialogueContext)
            ? ""
            : $" You are speaking of {dialogueContext.Trim().TrimEnd('.')}.";

        return BuildSpokenPrompt(neutralText, addressee,
                                 $"You are {speaker}. You are speaking directly to {who}.{context}", footer);
    }

    /// <summary>
    /// Prompt for an address to a companion during narration — the party member calling someone over to
    /// look at what they have just noticed. Same shape as <see cref="BuildDialoguePrompt"/>, because it
    /// is the same act: words one person says to another, framed and quoted so the model has a place to
    /// report the speaking outside the quotes and leaves the quotes to hold speech alone.
    /// <para>
    /// <paramref name="neutralText"/> is the whole address (see <c>NeutralNarration.SpokenReport</c>) —
    /// call attention, describe, ask — rewritten in one request. Sentence by sentence, a small model
    /// re-established the situation on every line and paid the persona and setting preamble three times
    /// for one utterance.
    /// </para>
    /// </summary>
    private static string BuildSpeakingPrompt(string neutralText, string? addressee, string footer)
    {
        string who = string.IsNullOrWhiteSpace(addressee) ? "your companion" : addressee!.Trim();

        return BuildSpokenPrompt(neutralText, addressee,
            $"You are speaking directly to {who}, calling {who} over to tell them what you have just noticed.",
            footer);
    }

    /// <summary>
    /// The body shared by every spoken kind: the situation, the line to say behind the
    /// <see cref="JsonConstraintGenerator.DialogueReplyFrame"/>, the rewrite task, the output shape, and
    /// the line again at the end. <paramref name="situation"/> is the one clause that differs between a
    /// dialogue turn and an address to a companion.
    /// </summary>
    private static string BuildSpokenPrompt(string neutralText, string? addressee, string situation, string footer)
    {
        string frame = JsonConstraintGenerator.DialogueReplyFrame(addressee);
        string framed = $"{frame}\"{neutralText.Trim().Trim('"')}\"";

        return $@"{situation}

This is what you must say:
{framed}

Say it again in your own words. Keep the opening ""{frame.Trim()}"" exactly as it is and rewrite only what is inside the quotes.

Change the word order, use other words, turn the sentence around, add an interjection or a figure of speech — whatever makes the line sound like you. The meaning must not change: add nothing, drop nothing, and a question stays a question. Do not give the line back unchanged.

{footer}

Output exactly:
{frame}""<your words>""

Do not add narration, actions, or explanations.

The line to say in your own words:
{framed}";
    }

    private static string InstructionFor(NarrationKind kind) => kind switch
    {
        NarrationKind.Observation =>
            "Re-express this perception in your own voice, keeping the same meaning.",
        NarrationKind.Reasoning =>
            "Re-express this as your own inner thought — your intent, what draws you, and how you mean to proceed — while keeping the same meaning.",
        NarrationKind.Action =>
            "Re-express this intended action in your own voice, concretely and naturally. The action you intend and its target are literal facts that must be preserved exactly: state plainly what you will do, never drop, blur, or replace the action itself — restyle only how it is told, not what is done.",
        NarrationKind.Outcome =>
            "Re-express this result in your own voice — what happens and how it feels to you — while keeping the same meaning and whether it succeeded or failed.",
        // Speaking and DialogueReplica never reach here: a spoken kind's whole prompt is built by
        // BuildSpokenPrompt, which states the task itself rather than appending an instruction line.
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
            // A spoken kind. The shape, the pronouns and the interlocutor's name are stated by
            // BuildSpokenPrompt, which owns the reply shape; the footer carries only voice, style and
            // setting.
            NarrationKind.DialogueReplica =>
                Config.Narrative.DialogueAnswerInstructionFor(personaReminder2, styleInstruction),
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
