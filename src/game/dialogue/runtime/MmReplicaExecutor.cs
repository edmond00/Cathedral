using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Asks a modus mentis LLM to write the player's reply for a chosen node.
/// Schema: { "replica": string }
/// </summary>
public class MmReplicaExecutor
{
    private readonly LlamaServerManager _llmManager;

    public MmReplicaExecutor(LlamaServerManager llmManager) => _llmManager = llmManager;

    public async Task<string> ExecuteAsync(
        ModusMentis mm,
        int slotId,
        DialogueTreeNode targetNode,
        NpcEntity npc,
        string partyMemberId,
        string treeDescription,
        CancellationToken ct = default)
    {
        var affinity     = npc.AffinityTable.GetLevel(partyMemberId);
        var affinityDesc = affinity.ToDisplayName(npc.DisplayName);

        string prompt = $"""
            Dialogue subject: {treeDescription}.
            You are speaking to {npc.DisplayName} ({affinityDesc}).
            Your perspective: {mm.PersonaTone ?? mm.ShortDescription}.
            Your goal at this step: {targetNode.Description}.

            Write a single short sentence (1-2 sentences max) of direct speech that fits your perspective.
            Do NOT include quotation marks. Do NOT narrate — only write the words spoken.
            """;

        var schema = new CompositeField("PlayerReplica",
            new StringField("replica", MinLength: 10, MaxLength: 250,
                Hint: "The words the protagonist says to the NPC"));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestAsync(slotId, prompt, gbnf, ct);
        if (string.IsNullOrWhiteSpace(json)) return $"I wanted to {targetNode.Description}.";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var replica = doc.RootElement.GetProperty("replica").GetString() ?? "";
            return string.IsNullOrWhiteSpace(replica) ? $"I wanted to {targetNode.Description}." : replica;
        }
        catch { return $"I wanted to {targetNode.Description}."; }
    }

    private async Task<string?> RequestAsync(int slotId, string prompt, string grammar, CancellationToken ct)
    {
        var tcs    = new TaskCompletionSource<string>();
        var buffer = string.Empty;

        void OnToken(object? s, TokenStreamedEventArgs e)    { if (e.SlotId == slotId) buffer += e.Token; }
        void OnDone (object? s, RequestCompletedEventArgs e) {
            if (e.SlotId != slotId) return;
            _llmManager.TokenStreamed     -= OnToken;
            _llmManager.RequestCompleted  -= OnDone;
            tcs.TrySetResult(e.WasCancelled ? string.Empty : buffer);
        }

        _llmManager.TokenStreamed    += OnToken;
        _llmManager.RequestCompleted += OnDone;
        try
        {
            await _llmManager.ContinueRequestAsync(slotId, prompt, null, null, grammar);
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MmReplicaExecutor: {ex.Message}");
            _llmManager.TokenStreamed    -= OnToken;
            _llmManager.RequestCompleted -= OnDone;
            return null;
        }
    }
}
