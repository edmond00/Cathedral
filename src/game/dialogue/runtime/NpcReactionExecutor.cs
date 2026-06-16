using System;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Generates the NPC's reaction to the player's replica after the skill check by re-expressing the
/// neutral outcome (goal achieved / unmoved) in the NPC's voice.
/// </summary>
public class NpcReactionExecutor
{
    private readonly PersonaRewriter _rewriter;

    public NpcReactionExecutor(LlamaServerManager llmManager) => _rewriter = new PersonaRewriter(llmManager);

    public async Task<string> ExecuteAsync(
        NpcEntity npc,
        int npcSlotId,
        string playerReplica,
        bool succeeded,
        DialogueTreeNode targetNode,
        CancellationToken ct = default)
    {
        string neutral = NeutralNarration.NpcReaction(succeeded, targetNode.Description);
        string text = await _rewriter.RewriteAsync(npcSlotId, neutral, NarrationKind.Speaking,
            personaReminder2: null, addressee: "the traveler", keepHistory: true, ct: ct);
        text = text.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(text) ? Fallback(npc, succeeded) : text;
    }

    private static string Fallback(NpcEntity npc, bool succeeded) => succeeded
        ? $"{npc.DisplayName} nods slowly."
        : $"{npc.DisplayName}'s expression doesn't change.";
}
