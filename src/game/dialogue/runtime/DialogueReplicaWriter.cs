using System;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
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
    public async Task<string> WriteAsync(
        int              slotId,
        string           neutralTemplate,
        DialogueContext  ctx,
        string           addresseeRole,
        string           subject,
        string?          personaReminder2 = null,
        string?          styleInstruction = null,
        bool             keepHistory      = true,
        CancellationToken ct              = default)
    {
        string expanded  = DialogueTemplate.Expand(neutralTemplate, ctx);
        string? addressee = ctx.Names.Placeholder(addresseeRole);

        string text = await _rewriter.RewriteAsync(
            slotId, expanded, NarrationKind.DialogueReplica,
            personaReminder2, addressee: addressee, keepHistory: keepHistory,
            styleInstruction: styleInstruction, dialogueContext: subject, ct: ct);

        return ctx.Names.ToReal(text.Trim().Trim('"'));
    }
}
