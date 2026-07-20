using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.LLM;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Generates the player's reply options at an <see cref="NpcLineNode"/>, reusing the narration-phase
/// graded-choice machinery. Per call:
/// <list type="number">
///   <item>sample <c>N</c> speaking Modi Mentis from the party member's known skills, where
///         <c>N = min(speech-fluency, held speaking MMs)</c> ([SpeechFluencyStat]);</item>
///   <item>each sampled MM grades the node's options by their <b>intent</b> (not the neutral text)
///         via <see cref="PersonaChoiceSelector"/> and picks its best-fitting one;</item>
///   <item>that MM then rewrites the chosen option's neutral replica in its own voice (same slot,
///         history kept) to produce one shown option.</item>
/// </list>
/// Two MMs may pick the same option and word it differently; an MM that grades everything low stays
/// silent (yields no option). The chosen option's MM later contributes its level to the dice pool.
/// </summary>
public class DialogueOptionGenerator
{
    private readonly PersonaChoiceSelector  _selector;
    private readonly PersonaRewriter        _rewriter;
    private readonly ModusMentisSlotManager _slots;
    private readonly Random                 _rng = new();

    public DialogueOptionGenerator(LlamaServerManager llm, ModusMentisSlotManager slots)
    {
        _selector = new PersonaChoiceSelector(llm);
        _rewriter = new PersonaRewriter(llm);
        _slots    = slots;
    }

    public async Task<List<PlayerReplicaOption>> GenerateAsync(
        NpcLineNode      node,
        PartyMember      pc,
        DialogueContext  ctx,
        string           subject,
        CancellationToken ct = default)
    {
        var results = new List<PlayerReplicaOption>();
        if (node.Options.Count == 0) return results;

        var speaking = pc.GetSpeakingModiMentis();
        if (speaking.Count == 0) return results;

        int n = Math.Min(SpeechFluency(pc), speaking.Count);
        var sampled = speaking.OrderBy(_ => _rng.Next()).Take(n).ToList();

        // Expanded intent → option (deduped), preserving order for the grade list.
        var intentToOption = new Dictionary<string, PlayerOption>(StringComparer.Ordinal);
        var intents        = new List<string>();
        foreach (var o in node.Options)
        {
            string intent = DialogueTemplate.Expand(o.Intent, ctx);
            if (intentToOption.ContainsKey(intent)) continue;
            intentToOption[intent] = o;
            intents.Add(intent);
        }

        string contextText = BuildContextText(ctx, subject);

        foreach (var mm in sampled)
        {
            try
            {
                int slot = await _slots.GetOrCreateSlotForModusMentisAsync(mm);

                var parts = new GradeEvalPromptParts(contextText, "You could say:", "fits what you would say");
                var picks = await _selector.SelectAsync(slot, mm, intents, parts, pick: 1, keepHistory: true, ct: ct);
                if (picks.Count == 0) continue;                       // graded everything low → silent
                if (!intentToOption.TryGetValue(picks[0], out var opt)) continue;

                // Rewrite the chosen option's replica in the MM's voice (history kept from grading).
                string expanded  = DialogueTemplate.Expand(opt.Replica, ctx);
                string? addressee = ctx.Names.Placeholder("npc");
                string text = await _rewriter.RewriteAsync(
                    slot, expanded, NarrationKind.DialogueReplica,
                    mm.PersonaReminder2, addressee: addressee, keepHistory: true,
                    styleInstruction: mm.StyleInstruction, dialogueContext: subject, ct: ct);
                text = ctx.Names.ToReal(text.Trim().Trim('"'));

                results.Add(new PlayerReplicaOption(mm, opt, text));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueOptionGenerator: option gen failed for {mm.DisplayName}: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>Max player replies this turn — the tongue-derived speech-fluency stat (1–5).</summary>
    public static int SpeechFluency(PartyMember m)
        => m.DerivedStats.FirstOrDefault(s => s.Name == "speech fluency")?.GetValue(m) ?? 1;

    private static string BuildContextText(DialogueContext ctx, string subject)
    {
        string who  = ctx.Names.Placeholder("npc") ?? "someone";
        string desc = DialogueTemplate.Expand("{npc:description}", ctx);
        return $"You are speaking with {who}, {desc}. The conversation is about {subject}.\n\n";
    }
}
