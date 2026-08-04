using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.LLM;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Generates the player's reply options at an <see cref="NpcLineNode"/>, reusing the narration-phase
/// graded-choice machinery. Per call:
/// <list type="number">
///   <item>sample <c>N</c> speaking Modi Mentis from the party member's known skills, where
///         <c>N = min(speech-fluency, held speaking MMs)</c> ([SpeechFluencyStat]);</item>
///   <item>each sampled MM chooses among the node's options by their <b>intent</b> (not the neutral
///         text) via <see cref="PersonaChoiceSelector"/> — it reasons in its own voice, then the
///         neutral critic maps that to one option;</item>
///   <item>that MM then rewrites the chosen option's neutral replica in its own voice (same slot,
///         freshly reset by the selection pass) to produce one shown option.</item>
/// </list>
/// Two MMs may pick the same option and word it differently. Dialogue has no refusal — every sampled
/// MM contributes a reply. The chosen option's MM later contributes its level to the dice pool.
/// </summary>
public class DialogueOptionGenerator
{
    private readonly PersonaChoiceSelector  _selector;
    private readonly PersonaRewriter        _rewriter;
    private readonly ModusMentisSlotManager _slots;
    private readonly Random                 _rng = GameRng.Stream("dialogue-options");

    public DialogueOptionGenerator(LlamaServerManager llm, ModusMentisSlotManager slots)
    {
        _selector = new PersonaChoiceSelector(llm);
        _rewriter = new PersonaRewriter(llm);
        _slots    = slots;
    }

    /// <param name="previousNpcReplica">
    /// The NPC's line the player is replying to, as its authored <b>heard</b> report (template-expanded,
    /// placeholder names) — "{npc} tells me that…" — rather than the persona's spoken version of it. See
    /// <c>DialogueTreeController._lastNpcNeutralLine</c>. This reaches the grading prompt only (which
    /// option to say), never the rewrite prompt (how to say it): choosing a reply needs to know what was
    /// just said, whereas the rewrite is deliberately given nothing but its own description. It is always
    /// the line just spoken at <paramref name="node"/>, so it is never null in practice, but null is
    /// accepted for callers that have none to give.
    /// </param>
    public async Task<List<PlayerReplicaOption>> GenerateAsync(
        NpcLineNode      node,
        PartyMember      pc,
        DialogueContext  ctx,
        string           subject,
        string?          previousNpcReplica = null,
        LlmPreviewSession? preview = null,
        Action<List<PlayerReplicaOption>>? commit = null,
        CancellationToken ct = default)
    {
        var results = new List<PlayerReplicaOption>();
        if (node.Options.Count == 0) { preview?.Reset(); return results; }

        var speaking = pc.GetSpeakingModiMentis();
        if (speaking.Count == 0) { preview?.Reset(); return results; }

        int n = Math.Min(SpeechFluency(pc), speaking.Count);
        var sampled = speaking.OrderBy(_ => _rng.Next()).Take(n).ToList();
        if (sampled.Count == 0) { preview?.Reset(); return results; }

        // All replies accumulate in one box as "- "-prefixed lines; the title switches to each reply's
        // modus mentis as it streams, and CONTINUE unlocks only once every reply is done. The transform
        // restores real names (the rewrite streams the simpler placeholder names).
        var part = preview?.BeginAccumulatingPart(PreviewTitles.For(sampled[0]), " - ", ctx.Names.ToReal);

        // Each option is offered as the speech act it would be — "ask who they are" — from its intent,
        // never its neutral replica. Intents are authored as imperative speech acts ("greet them
        // warmly"), not verbatim lines, so they are told, not quoted.
        string Action(PlayerOption o) => DialogueTemplate.Expand(o.Intent, ctx);

        var prompt = new PersonaChoicePrompt(
            BuildContextText(ctx, subject, previousNpcReplica), "What do you want to say?", "what they want to say");

        foreach (var mm in sampled)
        {
            try
            {
                int slot = await _slots.GetOrCreateSlotForModusMentisAsync(mm);

                // The MM reasons over the replies and the neutral critic maps that to one. Dialogue has
                // no refusal: no decline option is offered, so the MM always contributes a reply.
                // The MM's free reasoning streams first (dimmer, parenthesized), switching the box title
                // to this reply's MM; the critic then maps it to an option.
                var opt = (await _selector.SelectAsync(
                    slot, mm, node.Options, Action, prompt,
                    preview: part?.NextSegment(PreviewTitles.For(mm), isFree: true), ct: ct)).Item;
                if (opt == null) continue;                            // only if the option list was empty

                // The chosen reply's rewrite streams next as the normal "- " line.
                var sink = part?.NextSegment();

                // Rewrite the chosen option's replica in the MM's voice (fresh slot — the grading
                // requests reset themselves so no single option's evaluation colours the rewrite).
                string expanded  = DialogueTemplate.Expand(opt.Replica, ctx);
                string? addressee = ctx.Names.Placeholder("npc");
                string text = await _rewriter.RewriteAsync(
                    slot, expanded, NarrationKind.DialogueReplica,
                    mm.PersonaReminder2, addressee: addressee, keepHistory: true,
                    styleInstruction: mm.StyleInstruction, dialogueContext: subject,
                    speakerName: ctx.Names.Placeholder("you"), preview: sink, ct: ct);
                // Same structural parse as the NPC side: unwrap the grammar's delimiter quotes.
                text = ctx.Names.ToReal(DialogueReplicaWriter.NormalizeReply(text));

                results.Add(new PlayerReplicaOption(mm, opt, text));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueOptionGenerator: option gen failed for {mm.DisplayName}: {ex.Message}");
            }
        }

        // Deferred commit: the reply list is revealed (and becomes selectable) on CONTINUE.
        if (results.Count == 0)
        {
            preview?.Reset();
        }
        else if (preview != null && part != null)
        {
            if (commit != null) part.AttachCommit(() => commit(results));
            part.MarkComplete();
            preview.EndProduction();
        }
        else
        {
            commit?.Invoke(results);
            preview?.Reset();
        }

        return results;
    }

    /// <summary>Max player replies this turn — the tongue-derived speech-fluency stat (0–5).</summary>
    public static int SpeechFluency(PartyMember m)
        => m.DerivedStats.FirstOrDefault(s => s.Name == "speech fluency")?.GetValue(m) ?? 1;

    /// <summary>
    /// Situational preamble for the grading prompts — who is being spoken to, what about, and the
    /// line being replied to. Constant across a node's options, so it stays in the cached prefix;
    /// it mirrors what the rewrite prompt is told, so an option is graded against the same exchange
    /// it will later be voiced into.
    /// </summary>
    private static string BuildContextText(DialogueContext ctx, string subject, string? previousNpcReplica)
    {
        string who  = ctx.Names.Placeholder("npc") ?? "someone";
        string desc = DialogueTemplate.Expand("{npc:description}", ctx);
        // Stated, not quoted: the value is already a report from the player's side ("Thomas has asked
        // me…"), and quoting it would put a finished spoken line back into a prompt whose whole point
        // is that no such line is available to copy.
        string said = string.IsNullOrWhiteSpace(previousNpcReplica)
            ? " No one has spoken yet."
            : $" Just before this: {previousNpcReplica.Trim().Trim('"').TrimEnd('.')}.";
        return $"You are speaking with {who}, {desc}. The conversation is about {subject}.{said}\n\n";
    }
}
