using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Dialogue.Executors;

/// <summary>
/// Generates the NPC's response to the player's chosen replica.
/// The NPC reacts differently based on whether the skill check succeeded or failed.
/// Schema: { "response": string }
/// </summary>
public class NpcResponseExecutor
{
    private readonly LlamaServerManager _llmManager;

    public NpcResponseExecutor(LlamaServerManager llmManager)
    {
        _llmManager = llmManager;
    }

    public async Task<string> GenerateResponseAsync(
        NpcInstance npc,
        string playerReplica,
        bool skillCheckSucceeded,
        ConversationOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        string prompt = BuildPrompt(npc, playerReplica, skillCheckSucceeded, outcome);
        var schema = new CompositeField("NpcResponse",
            new StringField("response", MinLength: 20, MaxLength: 400,
                Hint: "What the NPC says in direct response to what the protagonist just said"));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestFromLLMAsync(npc.LlmSlotId, prompt, gbnf);

        if (string.IsNullOrWhiteSpace(json))
            return FallbackResponse(skillCheckSucceeded);

        try
        {
            using var doc = JsonDocument.Parse(json);
            string response = doc.RootElement.GetProperty("response").GetString() ?? "";
            return string.IsNullOrWhiteSpace(response) ? FallbackResponse(skillCheckSucceeded) : response;
        }
        catch (JsonException)
        {
            return FallbackResponse(skillCheckSucceeded);
        }
    }

    private static string BuildPrompt(
        NpcInstance npc,
        string playerReplica,
        bool succeeded,
        ConversationOutcome outcome)
    {
        string outcomeContext = succeeded
            ? $"The approach worked. Outcome: {outcome.OutcomeHint}."
            : $"The approach did not land. The NPC remains guarded.";

        return $"""
            The traveler just said to you: "{playerReplica}"
            {outcomeContext}
            Current affinity with this traveler: {npc.Affinity:F0}/100.
            Respond in character — 1 to 3 sentences of direct speech.
            Do NOT include quotation marks around your response. Do NOT narrate — only write what you say.
            """;
    }

    private static string FallbackResponse(bool succeeded)
        => succeeded
            ? "The innkeeper nods slowly. \"Aye. Could be.\""
            : "The innkeeper's expression doesn't change. \"Don't know what to tell you.\"";

    private async Task<string?> RequestFromLLMAsync(int slotId, string prompt, string grammar)
    {
        var tcs = new TaskCompletionSource<string>();
        var buffer = string.Empty;

        void OnToken(object? sender, TokenStreamedEventArgs e)
        {
            if (e.SlotId == slotId) buffer += e.Token;
        }

        void OnCompleted(object? sender, RequestCompletedEventArgs e)
        {
            if (e.SlotId != slotId) return;
            _llmManager.TokenStreamed -= OnToken;
            _llmManager.RequestCompleted -= OnCompleted;
            tcs.TrySetResult(e.WasCancelled ? string.Empty : buffer);
        }

        _llmManager.TokenStreamed += OnToken;
        _llmManager.RequestCompleted += OnCompleted;

        try
        {
            await _llmManager.ContinueRequestAsync(slotId, prompt, null, null, grammar);
            var result = await tcs.Task;
            await Task.Delay(100);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NpcResponseExecutor: LLM request failed: {ex.Message}");
            _llmManager.TokenStreamed -= OnToken;
            _llmManager.RequestCompleted -= OnCompleted;
            return null;
        }
    }
}
