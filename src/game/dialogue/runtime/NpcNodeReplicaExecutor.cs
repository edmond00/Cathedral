using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Npc;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Generates the NPC's opening speech for the current dialogue tree node.
/// Uses the NPC's dedicated LLM slot (WayToSpeakDescription as system prompt).
/// Schema: { "text": string }
/// </summary>
public class NpcNodeReplicaExecutor
{
    private readonly LlamaServerManager _llmManager;

    public NpcNodeReplicaExecutor(LlamaServerManager llmManager) => _llmManager = llmManager;

    public async Task<string> ExecuteAsync(
        NpcEntity npc,
        int npcSlotId,
        DialogueTreeNode node,
        string treeDescription,
        CancellationToken ct = default)
    {
        string prompt = $"""
            Dialogue subject: {treeDescription}.
            Current step: {node.Description}.
            Speak in character. Address the traveler directly — 1 to 3 sentences.
            Do not explain yourself. Just say what your character would say at this moment.
            """;

        var schema = new CompositeField("NpcReplica",
            new StringField("text", MinLength: 15, MaxLength: 400,
                Hint: "What the NPC says at this point in the conversation"));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestAsync(npcSlotId, prompt, gbnf, ct);
        if (string.IsNullOrWhiteSpace(json)) return Fallback(npc);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("text").GetString() ?? "";
            return string.IsNullOrWhiteSpace(text) ? Fallback(npc) : text;
        }
        catch (JsonException) { return Fallback(npc); }
    }

    private static string Fallback(NpcEntity npc) => $"{npc.DisplayName} looks at you in silence.";

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
            Console.Error.WriteLine($"NpcNodeReplicaExecutor: {ex.Message}");
            _llmManager.TokenStreamed    -= OnToken;
            _llmManager.RequestCompleted -= OnDone;
            return null;
        }
    }
}
