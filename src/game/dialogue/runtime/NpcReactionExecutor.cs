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
/// Generates the NPC's reaction to the player's chosen replica after the skill check.
/// Schema: { "response": string }
/// </summary>
public class NpcReactionExecutor
{
    private readonly LlamaServerManager _llmManager;

    public NpcReactionExecutor(LlamaServerManager llmManager) => _llmManager = llmManager;

    public async Task<string> ExecuteAsync(
        NpcEntity npc,
        int npcSlotId,
        string playerReplica,
        bool succeeded,
        DialogueTreeNode targetNode,
        CancellationToken ct = default)
    {
        string outcomeContext = succeeded
            ? $"The approach worked well. Goal achieved: {targetNode.Description}."
            : "The approach fell flat. The NPC is unmoved or slightly put off.";

        string prompt = $"""
            The traveler just said to you: "{playerReplica}"
            {outcomeContext}
            Respond in character — 1 to 3 sentences of direct speech.
            Do NOT include quotation marks. Do NOT narrate — only write what you say.
            """;

        var schema = new CompositeField("NpcReaction",
            new StringField("response", MinLength: 15, MaxLength: 400,
                Hint: "What the NPC says in response"));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestAsync(npcSlotId, prompt, gbnf, ct);
        if (string.IsNullOrWhiteSpace(json)) return Fallback(npc, succeeded);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("response").GetString() ?? "";
            return string.IsNullOrWhiteSpace(text) ? Fallback(npc, succeeded) : text;
        }
        catch { return Fallback(npc, succeeded); }
    }

    private static string Fallback(NpcEntity npc, bool succeeded) => succeeded
        ? $"{npc.DisplayName} nods slowly."
        : $"{npc.DisplayName}'s expression doesn't change.";

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
            Console.Error.WriteLine($"NpcReactionExecutor: {ex.Message}");
            _llmManager.TokenStreamed    -= OnToken;
            _llmManager.RequestCompleted -= OnDone;
            return null;
        }
    }
}
