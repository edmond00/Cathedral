using System;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Writes the player's reply for a chosen dialogue node by re-expressing the node's neutral intent
/// in the speaking Modus Mentis's voice (or verbatim in playground mode).
/// </summary>
public class MmReplicaExecutor
{
    private readonly PersonaRewriter _rewriter;

    public MmReplicaExecutor(LlamaServerManager llmManager) => _rewriter = new PersonaRewriter(llmManager);

    public async Task<string> ExecuteAsync(
        ModusMentis mm,
        int slotId,
        DialogueTreeNode targetNode,
        NpcEntity npc,
        string partyMemberId,
        string treeDescription,
        CancellationToken ct = default)
    {
        string neutral = NeutralNarration.DialoguePlayerReplica(targetNode.Description);
        string text = await _rewriter.RewriteAsync(slotId, neutral, NarrationKind.Speaking,
            mm.PersonaReminder2, addressee: npc.DisplayName, keepHistory: true, ct: ct);
        return text.Trim().Trim('"');
    }
}
