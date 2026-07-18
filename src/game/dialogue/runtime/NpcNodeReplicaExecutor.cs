using System;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Generates the NPC's opening speech for the current dialogue tree node by re-expressing the
/// node's neutral intent in the NPC's voice (its WayToSpeakDescription is the slot's system prompt).
/// </summary>
public class NpcNodeReplicaExecutor
{
    private readonly PersonaRewriter _rewriter;

    public NpcNodeReplicaExecutor(LlamaServerManager llmManager) => _rewriter = new PersonaRewriter(llmManager);

    public async Task<string> ExecuteAsync(
        NpcEntity npc,
        int npcSlotId,
        DialogueTreeNode node,
        string treeDescription,
        CancellationToken ct = default)
    {
        string neutral = NeutralNarration.NpcOpening(node.Replica);
        string text = await _rewriter.RewriteAsync(npcSlotId, neutral, NarrationKind.DialogueReplica,
            personaReminder2: null, addressee: "the traveler", keepHistory: true,
            dialogueContext: treeDescription, ct: ct);
        text = text.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(text) ? $"{npc.DisplayName} looks at you in silence." : text;
    }
}
