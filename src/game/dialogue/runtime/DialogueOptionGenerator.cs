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
///   <item>each sampled MM has the choice judge score every one of the node's options (its
///         branches) by their <b>intent</b> (not the neutral text) via
///         <see cref="PersonaChoiceSelector.ScoreAsync{T}"/>;</item>
///   <item>the MMs are spread over the branches (<see cref="AssignBranches"/>): the best-scoring
///         pair of MM and still unvoiced branch first, until every branch is voiced, then each
///         remaining MM takes its own best branch — so with at least as many replies as branches every
///         branch is offered, and with fewer each reply is a different branch;</item>
///   <item>each MM then rewrites its branch's neutral replica in its own voice (its slot freshly
///         reset) to produce one shown option.</item>
/// </list>
/// Two MMs pick the same option only once every branch is offered, and word it differently. Dialogue has no refusal — every sampled
/// MM contributes a reply. The chosen option's MM later contributes its level to the dice pool.
/// </summary>
public class DialogueOptionGenerator
{
    private readonly PersonaChoiceSelector  _selector;
    private readonly PersonaRewriter        _rewriter;
    private readonly ModusMentisSlotManager _slots;
    private readonly LlamaServerManager     _llm;
    private readonly Random                 _rng = GameRng.Stream("dialogue-options");

    public DialogueOptionGenerator(LlamaServerManager llm, ModusMentisSlotManager slots)
    {
        _selector = new PersonaChoiceSelector(llm);
        _rewriter = new PersonaRewriter(llm);
        _slots    = slots;
        _llm      = llm;
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

        var speaking = pc.GetUsableSpeakingModiMentis();
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

        string situation = BuildContextText(ctx, subject, previousNpcReplica);

        // 1. Every sampled MM scores every branch of the node (its reply options).
        var scored = new List<(ModusMentis Mm, double[] Scores)>();
        foreach (var mm in sampled)
        {
            try
            {
                var scores = await _selector.ScoreAsync(ChoiceKind.Reply, mm, node.Options, Action, situation, ct: ct);
                scored.Add((mm, scores));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueOptionGenerator: reply scoring failed for {mm.DisplayName}: {ex.Message}");
            }
        }

        // 2. Spread the MMs over the branches, so every branch is offered when there are enough replies.
        var assignment = AssignBranches(scored.Select(s => s.Scores).ToList(), node.Options.Count);
        foreach (var (who, branch) in assignment)
            Console.WriteLine($"DialogueOptionGenerator: {scored[who].Mm.DisplayName} voices branch {branch} " +
                              $"\"{Action(node.Options[branch])}\" (score {scored[who].Scores[branch]:0.00})");

        // 3. Each MM voices its branch, in the order the assignment was made.
        foreach (var (who, branch) in assignment)
        {
            var mm  = scored[who].Mm;
            var opt = node.Options[branch];
            try
            {
                int slot = await _slots.GetOrCreateSlotForModusMentisAsync(mm);
                // The rewrite below keeps history, so it starts from the system prompt alone — no
                // earlier turn's reply colours this one.
                _llm.ResetInstance(slot);

                // The chosen reply's rewrite streams as a "- " line, switching the box title to its MM.
                var sink = part?.NextSegment(PreviewTitles.For(mm));

                // Rewrite the chosen option's replica in the MM's voice.
                string expanded  = DialogueTemplate.Expand(opt.Replica, ctx);
                string? addressee = ctx.Names.Placeholder("npc");
                string text = await _rewriter.RewriteAsync(
                    slot, expanded, NarrationKind.DialogueReplica,
                    mm.PersonaReminder2, addressee: addressee, keepHistory: true,
                    styleInstruction: mm.StyleInstruction, dialogueContext: subject,
                    speakerName: ctx.Names.Placeholder("you"), preview: sink, ct: ct, flavour: Cathedral.Game.Narrative.Sanitizer.PersonaFlavour.For(mm));
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

    /// <summary>
    /// Pairs each speaker with the branch it will voice, from <paramref name="scores"/>[speaker][branch],
    /// so that the replies cover as many distinct branches as they can. While a branch is still
    /// unvoiced, the best-scoring (speaker, branch) pair among the unpaired speakers and the unvoiced
    /// branches is taken; once every branch is voiced, each remaining speaker takes its own best branch.
    /// So with at least as many replies as branches every branch is offered, and with fewer every reply
    /// is a different branch. A branch no speaker could score (<see cref="double.NegativeInfinity"/>:
    /// sampled out, or unreadable) is not sought. Returned in the order the pairs were made.
    /// </summary>
    public static List<(int Speaker, int Branch)> AssignBranches(IReadOnlyList<double[]> scores, int branchCount)
    {
        var result     = new List<(int, int)>();
        var unpaired   = Enumerable.Range(0, scores.Count).ToList();
        var unvoiced   = Enumerable.Range(0, branchCount)
            .Where(b => scores.Any(s => b < s.Length && !double.IsNegativeInfinity(s[b])))
            .ToHashSet();

        while (unpaired.Count > 0)
        {
            int bestSpeaker = -1, bestBranch = -1;
            double best = double.NegativeInfinity;
            foreach (int sp in unpaired)
                for (int b = 0; b < branchCount && b < scores[sp].Length; b++)
                {
                    if (unvoiced.Count > 0 && !unvoiced.Contains(b)) continue;
                    double v = scores[sp][b];
                    if (bestSpeaker < 0 || v > best) { best = v; bestSpeaker = sp; bestBranch = b; }
                }
            if (bestSpeaker < 0 || double.IsNegativeInfinity(best))
            {
                // Nothing this speaker set can score among what is left: give each its own best, if any.
                if (unvoiced.Count > 0) { unvoiced.Clear(); continue; }
                break;
            }
            result.Add((bestSpeaker, bestBranch));
            unpaired.Remove(bestSpeaker);
            unvoiced.Remove(bestBranch);
        }
        return result;
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
