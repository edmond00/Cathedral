using System;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.LLM;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Rewrites a single neutral dialogue line into persona voice. Handles the full pipeline for one
/// spoken line: expand <c>{scope:field}</c> template tokens against the conversation context, hand the
/// expanded (placeholder-named) line to the speaker's LLM slot as a <see cref="NarrationKind.DialogueReplica"/>,
/// then restore real names in the result. Used for NPC lines, the two resolution lines, and player
/// option lines.
/// </summary>
public class DialogueReplicaWriter
{
    private readonly PersonaRewriter _rewriter;

    public DialogueReplicaWriter(LlamaServerManager llm) => _rewriter = new PersonaRewriter(llm);

    /// <param name="slotId">LLM slot whose system prompt carries the speaker's persona.</param>
    /// <param name="neutralTemplate">Neutral line (may contain template tokens).</param>
    /// <param name="ctx">Live conversation context (fields + name mapping).</param>
    /// <param name="addresseeRole">Role the line is spoken to ("you" for NPC lines, "npc" for player lines).</param>
    /// <param name="subject">The dialogue subject, for the rewrite prompt context.</param>
    /// <param name="previousReplica">
    /// The addressee's previous spoken line (already persona-rewritten, real names), or null if this
    /// is the opening line of the conversation. Forwarded verbatim to
    /// <see cref="PersonaRewriter.RewriteAsync"/> for prompt grounding.
    /// </param>
    /// <param name="keepHistory">
    /// Whether the speaker's slot keeps this turn in its conversation history. Defaults to
    /// <c>false</c>: every replica re-sends the whole dialogue prompt (framing block + answer
    /// instruction footer, ~700 tokens with the reply), so keeping history added a full copy of that
    /// boilerplate per line and the last request of a conversation — the resolution line — overflowed
    /// the slot's 2048-token window and fell back to "{npc} nods.". Continuity does not depend on the
    /// history: <paramref name="previousReplica"/> already carries the exchange into the prompt, and
    /// the persona lives in the slot's system prompt, which a reset preserves.
    /// </param>
    public async Task<string> WriteAsync(
        int              slotId,
        string           neutralTemplate,
        DialogueContext  ctx,
        string           addresseeRole,
        string           subject,
        string?          personaReminder2 = null,
        string?          styleInstruction = null,
        bool             keepHistory      = false,
        string?          previousReplica  = null,
        ILlmPreviewSink? preview          = null,
        CancellationToken ct              = default)
    {
        string expanded  = DialogueTemplate.Expand(neutralTemplate, ctx);
        string? addressee = ctx.Names.Placeholder(addresseeRole);

        // The speaker is the other core role: NPC lines address "you", player lines address "npc".
        string speakerRole = string.Equals(addresseeRole, "you", StringComparison.OrdinalIgnoreCase) ? "npc" : "you";
        string? speaker    = ctx.Names.Placeholder(speakerRole);

        string text = await _rewriter.RewriteAsync(
            slotId, expanded, NarrationKind.DialogueReplica,
            personaReminder2, addressee: addressee, keepHistory: keepHistory,
            styleInstruction: styleInstruction, dialogueContext: subject,
            previousReplica: previousReplica, speakerName: speaker, preview: preview, ct: ct);

        return ctx.Names.ToReal(NormalizeReply(text));
    }

    /// <summary>
    /// Normalises a dialogue reply from the structured grammar shape <c>"spoken" (aside)?</c> into the
    /// inline form the rest of the dialogue pipeline expects: the spoken words with an optional trailing
    /// parenthetical aside, both without the delimiter quotes. Falls back to the trimmed text as-is when
    /// it is not quote-delimited (e.g. playground placeholder text, or a reply cut off before its closing
    /// quote by the token limit).
    /// <para>
    /// The generated line's <c>I say : </c> frame is already gone by here — <see cref="PersonaRewriter"/>
    /// strips it, since it also has to keep the frame out of the live preview. Shared with
    /// <see cref="DialogueOptionGenerator"/> so both sides of a conversation parse the shape one way.
    /// </para>
    /// </summary>
    public static string NormalizeReply(string raw)
    {
        string s = (raw ?? string.Empty).Trim();
        if (s.Length < 2 || s[0] != '"') return s.Trim('"');

        int close = s.IndexOf('"', 1);
        if (close < 0) return s.Trim('"');   // no closing quote (truncated) — degrade gracefully

        string spoken = s.Substring(1, close - 1).Trim();
        string rest   = s.Substring(close + 1).Trim();

        // A trailing parenthetical aside (the unspoken inner thought) is kept inline after the spoken words.
        return rest.Length >= 2 && rest[0] == '(' && rest[^1] == ')'
            ? $"{spoken} {rest}".Trim()
            : spoken;
    }
}
